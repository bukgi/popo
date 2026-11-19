using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Dynamically loads PMDCollab Pokémon animations, portraits, maps, and VFX sprite sheets
    /// directly from Assets/Resources/PMDCollab.
    /// </summary>
    public static class PMDRuntimeCharacterLoader
    {
        private const float DefaultPixelsPerUnit = 24f;
        public const int CurrentSpriteSheetVersion = 9;

        private static readonly string[] PortraitEmotionOrder =
        {
            "Normal", "Happy", "Pain", "Angry", "Worried",
            "Sad", "Crying", "Shouting", "Teary-Eyed", "Determined",
            "Joyous", "Inspired", "Surprised", "Dizzy", "Special0",
            "Special1", "Sigh", "Stunned", "Special2", "Special3"
        };

        private static readonly Dictionary<string, PMDCharacterData> Cache = new Dictionary<string, PMDCharacterData>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<Sprite>> VfxCache = new Dictionary<string, List<Sprite>>(StringComparer.OrdinalIgnoreCase);

        public static List<PMDCharacterData> LoadAllAvailableCharacters()
        {
            Cache.Clear();
            var results = new List<PMDCharacterData>();

            var trainerData = PMDTrainerSpriteBuilder.BuildTrainerCharacterData();
            if (trainerData != null)
            {
                Cache["0000_Trainer"] = trainerData;
                results.Add(trainerData);
            }

            var folderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var folderList = new List<string>();

            // 1. Automatically scan Assets/Resources/PMDCollab/Pokemon for ALL installed Pokémon folders
            string pokemonRoot = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Pokemon");
            if (Directory.Exists(pokemonRoot))
            {
                string[] dirs = Directory.GetDirectories(pokemonRoot);
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);

                // Keep 0025_Pikachu first as starter partner, then all other Pokédex numbers in order
                for (int i = 0; i < dirs.Length; i++)
                {
                    string name = Path.GetFileName(dirs[i]);
                    if (string.Equals(name, "0025_Pikachu", StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(Path.Combine(dirs[i], "AnimData.xml")))
                    {
                        folderSet.Add(name);
                        folderList.Add(name);
                    }
                }

                for (int i = 0; i < dirs.Length; i++)
                {
                    string name = Path.GetFileName(dirs[i]);
                    if (!folderSet.Contains(name) && File.Exists(Path.Combine(dirs[i], "AnimData.xml")))
                    {
                        folderSet.Add(name);
                        folderList.Add(name);
                    }
                }
            }

            foreach (string folder in folderList)
            {
                // Eagerly load Pikachu; lazy-load all other Pokémon on first use so 80+ Pokémon load in < 0.05s!
                bool eager = string.Equals(folder, "0025_Pikachu", StringComparison.OrdinalIgnoreCase);
                var data = eager ? LoadCharacterFromResources(folder) : CreateLazyCharacterStub(folder);
                if (data != null)
                {
                    results.Add(data);
                }
            }

            return results;
        }

        public static PMDCharacterData CreateLazyCharacterStub(string folderName)
        {
            if (Cache.TryGetValue(folderName, out PMDCharacterData cached) && cached != null && cached.runtimeVersion == CurrentSpriteSheetVersion)
                return cached;

            var data = ScriptableObject.CreateInstance<PMDCharacterData>();
            string[] split = folderName.Split('_');
            data.pokemonId = split.Length > 0 ? split[0] : "0000";
            if (int.TryParse(data.pokemonId, out int parsedDex))
                data.dexId = parsedDex;
            data.pokemonName = split.Length > 1 ? split[1] : folderName;
            data.runtimeVersion = CurrentSpriteSheetVersion;
            data.isFullyLoaded = false;
            data.name = $"Runtime_{folderName}_v{CurrentSpriteSheetVersion}";

            Cache[folderName] = data;
            return data;
        }

        public static void EnsureCharacterLoaded(PMDCharacterData data)
        {
            if (data == null || data.isFullyLoaded)
                return;

            data.isFullyLoaded = true;
            if (data.pokemonId == "0000" || string.Equals(data.pokemonName, "Trainer", StringComparison.OrdinalIgnoreCase))
                return;

            string folderName = $"{data.pokemonId}_{data.pokemonName}";
            string baseResPath = $"PMDCollab/Pokemon/{folderName}";
            string diskFolderPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Pokemon", folderName);

            string xmlText = null;
            string xmlDiskPath = Path.Combine(diskFolderPath, "AnimData.xml");
            if (File.Exists(xmlDiskPath))
            {
                xmlText = File.ReadAllText(xmlDiskPath);
            }
            else
            {
                TextAsset xmlAsset = Resources.Load<TextAsset>($"{baseResPath}/AnimData");
                if (xmlAsset != null)
                    xmlText = xmlAsset.text;
            }

            if (string.IsNullOrEmpty(xmlText))
                return;

            data.animations.Clear();
            data.portraits.Clear();
            ParseXmlAndSliceTextures(data, xmlText, baseResPath, diskFolderPath);
            SlicePortraitSheet(data, baseResPath, diskFolderPath);
        }

        /// <summary>
        /// Loads a horizontal VFX strip from Assets/Resources/PMDCollab/VFX/{vfxFileName}.png.
        /// Every PMDCollab Particle sheet consists of square frames where frameSize = tex.height.
        /// </summary>
        public static List<Sprite> LoadVfxFrames(string vfxFileName, float ppu = 24f)
        {
            if (string.IsNullOrEmpty(vfxFileName))
                return null;

            string key = $"{vfxFileName}_{ppu}";
            if (VfxCache.TryGetValue(key, out List<Sprite> cached) && cached != null && cached.Count > 0 && cached[0] != null)
            {
                return cached;
            }

            string diskPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "VFX", vfxFileName + ".png");
            Texture2D tex = LoadPixelTextureFromFile(diskPath, $"PMDCollab/VFX/{vfxFileName}");
            if (tex == null)
                return null;

            int frameHeight = tex.height;
            int frameWidth = frameHeight;
            if (frameWidth <= 0 || tex.width < frameWidth)
            {
                frameWidth = tex.width;
            }

            int frameCount = Mathf.Max(1, tex.width / frameWidth);
            var frames = new List<Sprite>(frameCount);

            for (int i = 0; i < frameCount; i++)
            {
                Rect rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu);
                sp.name = $"{vfxFileName}_{i}";
                frames.Add(sp);
            }

            VfxCache[key] = frames;
            return frames;
        }

        public static PMDCharacterData ReloadCharacterFromDisk(string folderName)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                Cache.Remove(folderName);
            }
            return LoadCharacterFromResources(folderName);
        }

        public static PMDCharacterData LoadCharacterFromResources(string folderName)
        {
            if (!string.IsNullOrEmpty(folderName) &&
                (folderName.StartsWith("0000", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(folderName, "Trainer", StringComparison.OrdinalIgnoreCase)))
            {
                var trainer = PMDTrainerSpriteBuilder.BuildTrainerCharacterData();
                Cache["0000_Trainer"] = trainer;
                return trainer;
            }

            if (Cache.TryGetValue(folderName, out PMDCharacterData cached) && cached != null && cached.runtimeVersion == CurrentSpriteSheetVersion)
            {
                var idle = cached.GetAnimation("Idle");
                if (idle != null && idle.directionRows.Count > 0 && idle.directionRows[0].frames.Count > 0 && idle.directionRows[0].frames[0] != null)
                {
                    return cached;
                }
            }

            string baseResPath = $"PMDCollab/Pokemon/{folderName}";
            string diskFolderPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Pokemon", folderName);

            string xmlText = null;
            string xmlDiskPath = Path.Combine(diskFolderPath, "AnimData.xml");
            if (File.Exists(xmlDiskPath))
            {
                xmlText = File.ReadAllText(xmlDiskPath);
            }
            else
            {
                TextAsset xmlAsset = Resources.Load<TextAsset>($"{baseResPath}/AnimData");
                if (xmlAsset != null)
                    xmlText = xmlAsset.text;
            }

            if (string.IsNullOrEmpty(xmlText))
            {
                Debug.LogWarning($"[PMDCollab] Missing AnimData.xml for {folderName}");
                return null;
            }

            var data = ScriptableObject.CreateInstance<PMDCharacterData>();
            string[] split = folderName.Split('_');
            data.pokemonId = split.Length > 0 ? split[0] : "0000";
            if (int.TryParse(data.pokemonId, out int parsedDex))
                data.dexId = parsedDex;
            data.pokemonName = split.Length > 1 ? split[1] : folderName;
            data.runtimeVersion = CurrentSpriteSheetVersion;
            data.name = $"Runtime_{folderName}_v{CurrentSpriteSheetVersion}";

            ParseXmlAndSliceTextures(data, xmlText, baseResPath, diskFolderPath);
            SlicePortraitSheet(data, baseResPath, diskFolderPath);

            Cache[folderName] = data;
            return data;
        }

        public static Texture2D LoadPixelTextureFromFile(string diskPath, string resourceFallbackPath = null)
        {
            if (!string.IsNullOrEmpty(diskPath) && File.Exists(diskPath))
            {
                byte[] bytes = File.ReadAllBytes(diskPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes, false))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.name = Path.GetFileNameWithoutExtension(diskPath);
                    return tex;
                }
            }

            if (!string.IsNullOrEmpty(resourceFallbackPath))
            {
                Texture2D resTex = Resources.Load<Texture2D>(resourceFallbackPath);
                if (resTex != null)
                {
                    resTex.filterMode = FilterMode.Point;
                    return resTex;
                }
            }

            return null;
        }

        private static void ParseXmlAndSliceTextures(PMDCharacterData data, string xmlContent, string baseResPath, string diskFolderPath)
        {
            XDocument doc = XDocument.Parse(xmlContent);
            XElement root = doc.Element("AnimData");
            if (root == null)
                return;

            data.shadowSize = ParseInt(root.Element("ShadowSize"), 1);
            XElement animsEl = root.Element("Anims");
            if (animsEl == null)
                return;

            foreach (XElement animEl in animsEl.Elements("Anim"))
            {
                var seq = new PMDAnimSequence
                {
                    name = animEl.Element("Name")?.Value?.Trim() ?? string.Empty,
                    index = ParseInt(animEl.Element("Index"), -1),
                    copyOf = animEl.Element("CopyOf")?.Value?.Trim() ?? string.Empty,
                    frameWidth = ParseInt(animEl.Element("FrameWidth"), 0),
                    frameHeight = ParseInt(animEl.Element("FrameHeight"), 0),
                    rushFrame = ParseInt(animEl.Element("RushFrame"), -1),
                    hitFrame = ParseInt(animEl.Element("HitFrame"), -1),
                    returnFrame = ParseInt(animEl.Element("ReturnFrame"), -1)
                };

                XElement durationsEl = animEl.Element("Durations");
                if (durationsEl != null)
                {
                    foreach (XElement d in durationsEl.Elements("Duration"))
                    {
                        seq.durations.Add(ParseInt(d, 6));
                    }
                }

                if (!string.IsNullOrEmpty(seq.name))
                {
                    data.animations.Add(seq);
                }
            }

            var byName = new Dictionary<string, PMDAnimSequence>(StringComparer.OrdinalIgnoreCase);
            foreach (var seq in data.animations)
            {
                byName[seq.name] = seq;
            }

            foreach (var seq in data.animations)
            {
                if (!string.IsNullOrEmpty(seq.copyOf) && byName.TryGetValue(seq.copyOf, out var src))
                {
                    if (seq.frameWidth <= 0) seq.frameWidth = src.frameWidth;
                    if (seq.frameHeight <= 0) seq.frameHeight = src.frameHeight;
                    if (seq.durations.Count == 0) seq.durations.AddRange(src.durations);
                }
            }

            foreach (var seq in data.animations)
            {
                if (seq.frameWidth <= 0 || seq.frameHeight <= 0)
                    continue;

                string animDiskPath = Path.Combine(diskFolderPath, $"{seq.name}-Anim.png");
                Texture2D tex = LoadPixelTextureFromFile(animDiskPath, $"{baseResPath}/{seq.name}-Anim");

                if (tex == null && !string.IsNullOrEmpty(seq.copyOf))
                {
                    string copyDiskPath = Path.Combine(diskFolderPath, $"{seq.copyOf}-Anim.png");
                    tex = LoadPixelTextureFromFile(copyDiskPath, $"{baseResPath}/{seq.copyOf}-Anim");
                }

                if (tex == null)
                    continue;

                int cols = tex.width / seq.frameWidth;
                int rows = tex.height / seq.frameHeight;

                seq.directionRows.Clear();
                for (int r = 0; r < rows; r++)
                {
                    var dirFrames = new PMDDirectionFrames
                    {
                        direction = (PMDDirection)Mathf.Clamp(r, 0, 7)
                    };

                    int yBottom = tex.height - ((r + 1) * seq.frameHeight);
                    for (int c = 0; c < cols; c++)
                    {
                        int xLeft = c * seq.frameWidth;
                        Rect rect = new Rect(xLeft, yBottom, seq.frameWidth, seq.frameHeight);
                        Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.3f), DefaultPixelsPerUnit);
                        sp.name = $"{data.pokemonName}_{seq.name}_R{r}_C{c}";
                        dirFrames.frames.Add(sp);
                    }

                    seq.directionRows.Add(dirFrames);
                }
            }
        }

        private static void SlicePortraitSheet(PMDCharacterData data, string baseResPath, string diskFolderPath)
        {
            string portraitDiskPath = Path.Combine(diskFolderPath, "Portraits.png");
            Texture2D tex = LoadPixelTextureFromFile(portraitDiskPath, $"{baseResPath}/Portraits");
            if (tex == null)
                return;

            const int size = 40;
            int cols = tex.width / size;
            int rows = tex.height / size;

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                int yBottom = tex.height - ((r + 1) * size);
                for (int c = 0; c < cols; c++)
                {
                    if (idx >= PortraitEmotionOrder.Length)
                        break;

                    int xLeft = c * size;
                    Rect rect = new Rect(xLeft, yBottom, size, size);
                    Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 40f);
                    sp.name = $"{data.pokemonName}_Portrait_{PortraitEmotionOrder[idx]}";

                    data.portraits.Add(new PMDPortraitEntry
                    {
                        emotionName = PortraitEmotionOrder[idx],
                        sprite = sp
                    });
                    idx++;
                }
            }
        }

        private static int ParseInt(XElement el, int fallback)
        {
            if (el == null) return fallback;
            return int.TryParse(el.Value.Trim(), out int val) ? val : fallback;
        }

        // =========================================================================================
        // LIVE IN-GAME POKÉDEX #1 - #1025 AUTO-DOWNLOADER (PMDCollab GraphQL)
        // =========================================================================================
        private static readonly HttpClient RuntimeHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        public static bool IsDownloadingPokemon { get; private set; }

        [Serializable] private class GqlRoot { public GqlData data; }
        [Serializable] private class GqlData { public GqlMonster[] monster; }
        [Serializable] private class GqlMonster { public int id; public string name; public GqlForm[] forms; }
        [Serializable] private class GqlForm { public string path; public string fullName; public GqlPortrait portraits; public GqlSprite sprites; }
        [Serializable] private class GqlPortrait { public string sheetUrl; }
        [Serializable] private class GqlSprite { public string zipUrl; }

        public static async void DownloadAndSummonByDexIdAsync(int dexId)
        {
            if (IsDownloadingPokemon || dexId < 1 || dexId > 1025)
                return;

            IsDownloadingPokemon = true;
            PMDGameState.Toast($"⏳ Đang tải Pokémon Pokédex #{dexId:D4} từ kho PMDCollab...", new Color(0.55f, 0.92f, 1f));

            try
            {
                string query = $"{{\"query\":\"{{ monster(filter: [{dexId}]) {{ id name forms {{ path fullName portraits {{ sheetUrl }} sprites {{ zipUrl }} }} }} }}\"}}";
                using var content = new StringContent(query, Encoding.UTF8, "application/json");
                using var resp = await RuntimeHttp.PostAsync("https://spriteserver.pmdcollab.org/graphql", content);
                resp.EnsureSuccessStatusCode();

                string json = await resp.Content.ReadAsStringAsync();
                var parsed = JsonUtility.FromJson<GqlRoot>(json);
                var mon = (parsed?.data?.monster != null && parsed.data.monster.Length > 0) ? parsed.data.monster[0] : null;
                if (mon == null || mon.forms == null || mon.forms.Length == 0)
                {
                    PMDGameState.Toast($"Không tìm thấy dữ liệu cho Pokédex #{dexId:D4}!", new Color(1f, 0.5f, 0.4f));
                    return;
                }

                GqlForm form = null;
                for (int i = 0; i < mon.forms.Length; i++)
                {
                    if (mon.forms[i].sprites != null && !string.IsNullOrEmpty(mon.forms[i].sprites.zipUrl))
                    {
                        form = mon.forms[i];
                        if (string.IsNullOrEmpty(form.path)) break;
                    }
                }

                if (form == null)
                {
                    PMDGameState.Toast($"Pokédex #{dexId:D4} ({mon.name}) chưa có sprite!", new Color(1f, 0.5f, 0.4f));
                    return;
                }

                string safeName = System.Text.RegularExpressions.Regex.Replace(mon.name, @"[^a-zA-Z0-9_-]", "");
                string folderName = $"{mon.id:D4}_{safeName}";
                string targetDir = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Pokemon", folderName);
                Directory.CreateDirectory(targetDir);

                byte[] zipBytes = await RuntimeHttp.GetByteArrayAsync(form.sprites.zipUrl);
                using (var ms = new MemoryStream(zipBytes))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.Equals(entry.Name, "AnimData.xml", StringComparison.OrdinalIgnoreCase) ||
                            entry.Name.EndsWith("-Anim.png", StringComparison.OrdinalIgnoreCase))
                        {
                            string outFile = Path.Combine(targetDir, entry.Name);
                            using var inStream = entry.Open();
                            using var outStream = File.Create(outFile);
                            await inStream.CopyToAsync(outStream);
                        }
                    }
                }

                if (form.portraits != null && !string.IsNullOrEmpty(form.portraits.sheetUrl))
                {
                    byte[] portBytes = await RuntimeHttp.GetByteArrayAsync(form.portraits.sheetUrl);
                    File.WriteAllBytes(Path.Combine(targetDir, "Portraits.png"), portBytes);
                }

                var loadedData = LoadCharacterFromResources(folderName);
                if (loadedData != null && PMDOpenWorldManager.Instance != null)
                {
                    PMDOpenWorldManager.Instance.RegisterAndSummonCharacter(loadedData);
                }
            }
            catch (Exception ex)
            {
                PMDGameState.Toast($"Lỗi tải Pokédex #{dexId:D4}: {ex.Message}", new Color(1f, 0.45f, 0.4f));
            }
            finally
            {
                IsDownloadingPokemon = false;
            }
        }
    }
}

