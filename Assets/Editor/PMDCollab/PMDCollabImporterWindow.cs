using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using POKE.PMDCollab.OpenWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace POKE.PMDCollab.Editor
{
    public class PMDCollabImporterWindow : EditorWindow
    {
        private const string GraphQlUrl = "https://spriteserver.pmdcollab.org/graphql";
        private const string UxmlPath = "Assets/Editor/PMDCollab/PMDCollabImporterWindow.uxml";

        private static readonly int[] DefaultOpenWorldRosterDexIds = { 25, 1, 4, 7, 133, 448 };

        private static readonly HashSet<string> CoreAnimationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Idle", "Walk", "Attack", "Strike", "Shoot", "Hurt", "Sleep", "Faint",
            "Charge", "Hop", "Swing", "Double", "Rotate", "Shake", "Dance", "Pose"
        };

        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        private IntegerField _dexIdField;
        private Button _fetchFormsButton;
        private DropdownField _formDropdown;
        private Label _creditInfoLabel;
        private Toggle _coreAnimsOnlyToggle;
        private Toggle _importPortraitsToggle;
        private Toggle _importShadowsToggle;
        private IntegerField _pixelsPerUnitField;
        private TextField _outputFolderField;
        private Button _downloadAndSliceButton;
        private ObjectField _lastImportedDataField;
        private Button _spawnInSceneButton;
        private Button _buildOpenWorldButton;
        private Label _statusLabel;

        private MonsterNode _currentMonster;
        private readonly List<MonsterFormNode> _availableForms = new List<MonsterFormNode>();

        [MenuItem("Tools/PMD Collab/Pokémon Sprite Importer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<PMDCollabImporterWindow>();
            wnd.titleContent = new GUIContent("PMD Sprite Importer");
            wnd.minSize = new Vector2(450, 560);
            wnd.Show();
        }

        [MenuItem("Tools/PMD Collab/Tạo Thế Giới Mở (Auto-Download 6 Pokémon & Map)")]
        public static void QuickBuildOpenWorldFromMenu()
        {
            var wnd = GetWindow<PMDCollabImporterWindow>();
            wnd.titleContent = new GUIContent("PMD Sprite Importer");
            wnd.Show();
            _ = wnd.BuildCompleteOpenWorldAsync();
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                rootVisualElement.Add(new Label($"Không tìm thấy file UXML tại: {UxmlPath}"));
                return;
            }

            visualTree.CloneTree(rootVisualElement);

            _dexIdField = rootVisualElement.Q<IntegerField>("dexIdField");
            _fetchFormsButton = rootVisualElement.Q<Button>("fetchFormsButton");
            _formDropdown = rootVisualElement.Q<DropdownField>("formDropdown");
            _creditInfoLabel = rootVisualElement.Q<Label>("creditInfoLabel");
            _coreAnimsOnlyToggle = rootVisualElement.Q<Toggle>("coreAnimsOnlyToggle");
            _importPortraitsToggle = rootVisualElement.Q<Toggle>("importPortraitsToggle");
            _importShadowsToggle = rootVisualElement.Q<Toggle>("importShadowsToggle");
            _pixelsPerUnitField = rootVisualElement.Q<IntegerField>("pixelsPerUnitField");
            _outputFolderField = rootVisualElement.Q<TextField>("outputFolderField");
            _downloadAndSliceButton = rootVisualElement.Q<Button>("downloadAndSliceButton");
            _lastImportedDataField = rootVisualElement.Q<ObjectField>("lastImportedDataField");
            _spawnInSceneButton = rootVisualElement.Q<Button>("spawnInSceneButton");
            _buildOpenWorldButton = rootVisualElement.Q<Button>("buildOpenWorldButton");
            _statusLabel = rootVisualElement.Q<Label>("statusLabel");

            if (_lastImportedDataField != null)
            {
                _lastImportedDataField.objectType = typeof(PMDCharacterData);
            }

            if (_fetchFormsButton != null)
            {
                _fetchFormsButton.clicked += () => _ = FetchPokemonFormsAsync(_dexIdField.value);
            }

            if (_formDropdown != null)
            {
                _formDropdown.RegisterValueChangedCallback(_ => UpdateSelectedFormCredits());
            }

            if (_downloadAndSliceButton != null)
            {
                _downloadAndSliceButton.clicked += () => _ = DownloadAndImportSelectedAsync();
            }

            if (_spawnInSceneButton != null)
            {
                _spawnInSceneButton.clicked += SpawnSelectedCharacterInScene;
            }

            if (_buildOpenWorldButton != null)
            {
                _buildOpenWorldButton.clicked += () => _ = BuildCompleteOpenWorldAsync();
            }

            BindPresetButton("preset0001", 1);
            BindPresetButton("preset0004", 4);
            BindPresetButton("preset0007", 7);
            BindPresetButton("preset0025", 25);
            BindPresetButton("preset0133", 133);
            BindPresetButton("preset0151", 151);
            BindPresetButton("preset0448", 448);

            _ = FetchPokemonFormsAsync(_dexIdField != null ? _dexIdField.value : 25);
        }

        private void BindPresetButton(string buttonName, int dexId)
        {
            var btn = rootVisualElement.Q<Button>(buttonName);
            if (btn == null)
                return;

            btn.clicked += () =>
            {
                if (_dexIdField != null)
                    _dexIdField.value = dexId;
                _ = FetchPokemonFormsAsync(dexId);
            };
        }

        private async Task FetchPokemonFormsAsync(int dexId)
        {
            if (dexId <= 0)
            {
                SetStatus("Số Pokédex phải lớn hơn 0.");
                return;
            }

            SetStatus($"Đang tra cứu dữ liệu Pokémon #{dexId:D4} từ spriteserver.pmdcollab.org...");
            SetButtonsEnabled(false);

            try
            {
                MonsterNode monster = await QuerySingleMonsterAsync(dexId);
                _availableForms.Clear();
                var choices = new List<string>();

                if (monster != null)
                {
                    _currentMonster = monster;
                    if (_currentMonster.forms != null)
                    {
                        foreach (var form in _currentMonster.forms)
                        {
                            bool hasSprites = !string.IsNullOrEmpty(form.sprites?.zipUrl);
                            bool hasPortraits = !string.IsNullOrEmpty(form.portraits?.sheetUrl);
                            if (!hasSprites && !hasPortraits)
                                continue;

                            _availableForms.Add(form);
                            string pathSuffix = string.IsNullOrEmpty(form.path) ? "Base" : form.path;
                            string tag = hasSprites ? "[Sprites+Portraits]" : "[Chỉ Portraits]";
                            choices.Add($"{form.fullName} ({pathSuffix}) {tag}");
                        }
                    }
                }

                if (_formDropdown != null)
                {
                    _formDropdown.choices = choices;
                    _formDropdown.index = choices.Count > 0 ? 0 : -1;
                }

                if (_availableForms.Count > 0)
                {
                    UpdateSelectedFormCredits();
                    SetStatus($"Đã tìm thấy #{_currentMonster.id:D4} {_currentMonster.name} với {_availableForms.Count} form có sẵn.");
                }
                else
                {
                    SetStatus($"Không tìm thấy form nào cho Pokédex #{dexId:D4}.");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi khi truy vấn GraphQL API: {ex.Message}");
                Debug.LogWarning($"[PMDCollab] GraphQL query error: {ex}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private static async Task<MonsterNode> QuerySingleMonsterAsync(int dexId)
        {
            string queryJson = $"{{\"query\":\"{{ monster(filter: [{dexId}]) {{ id name forms {{ path fullName portraits {{ sheetUrl creditPrimary {{ name }} }} sprites {{ zipUrl animDataXml creditPrimary {{ name }} }} }} }} }}\"}}";
            using var content = new StringContent(queryJson, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await SharedHttpClient.PostAsync(GraphQlUrl, content);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            var parsed = JsonUtility.FromJson<GraphQlRootResponse>(body);
            if (parsed?.data?.monster != null && parsed.data.monster.Length > 0)
            {
                return parsed.data.monster[0];
            }
            return null;
        }

        private void UpdateSelectedFormCredits()
        {
            if (_creditInfoLabel == null)
                return;

            MonsterFormNode form = GetSelectedForm();
            if (form == null || _currentMonster == null)
            {
                _creditInfoLabel.text = "Tác giả (Credits): Chưa chọn form";
                return;
            }

            string spriteAuthor = form.sprites?.creditPrimary?.name ?? "Không có / Mặc định";
            string portraitAuthor = form.portraits?.creditPrimary?.name ?? "Không có / Mặc định";
            _creditInfoLabel.text = $"Pokémon: #{_currentMonster.id:D4} {_currentMonster.name} ({form.fullName}) | Sprite Artist: {spriteAuthor} | Portrait Artist: {portraitAuthor}";
        }

        private MonsterFormNode GetSelectedForm()
        {
            if (_formDropdown == null || _availableForms.Count == 0)
                return null;

            int idx = Mathf.Clamp(_formDropdown.index, 0, _availableForms.Count - 1);
            return _availableForms[idx];
        }

        private async Task<PMDCharacterData> DownloadAndImportSelectedAsync()
        {
            int dexId = _dexIdField != null ? _dexIdField.value : 25;
            if (_currentMonster == null || _currentMonster.id != dexId || _availableForms.Count == 0)
            {
                await FetchPokemonFormsAsync(dexId);
            }

            MonsterFormNode form = GetSelectedForm();
            if (_currentMonster == null || form == null)
            {
                SetStatus("Không có dữ liệu Pokémon để tải. Vui lòng kiểm tra kết nối mạng hoặc số Pokédex.");
                return null;
            }

            SetButtonsEnabled(false);
            try
            {
                PMDCharacterData data = await ImportMonsterFormInternalAsync(_currentMonster, form);
                if (data != null && _lastImportedDataField != null)
                {
                    _lastImportedDataField.value = data;
                    EditorGUIUtility.PingObject(data);
                    Selection.activeObject = data;
                }
                return data;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                SetButtonsEnabled(true);
            }
        }

        private async Task<PMDCharacterData> ImportMonsterFormInternalAsync(MonsterNode monster, MonsterFormNode form)
        {
            bool coreOnly = _coreAnimsOnlyToggle == null || _coreAnimsOnlyToggle.value;
            bool includePortraits = _importPortraitsToggle == null || _importPortraitsToggle.value;
            bool includeShadowsAndOffsets = _importShadowsToggle != null && _importShadowsToggle.value;
            int ppu = _pixelsPerUnitField != null ? Mathf.Max(1, _pixelsPerUnitField.value) : 24;
            string baseOutputFolder = _outputFolderField != null && !string.IsNullOrWhiteSpace(_outputFolderField.value)
                ? _outputFolderField.value.Trim().TrimEnd('/', '\\')
                : "Assets/PMDCollab/Pokemon";

            string safePokemonName = SanitizeFileName(monster.name);
            string safeFormSuffix = string.IsNullOrEmpty(form.path)
                ? "Normal"
                : SanitizeFileName(form.fullName.Replace(" ", "_"));

            string targetFolder = $"{baseOutputFolder}/{monster.id:D4}_{safePokemonName}_{safeFormSuffix}";
            string dataAssetPath = $"{targetFolder}/{safePokemonName}_{safeFormSuffix}_Data.asset";

            // Reuse existing asset if already imported with animations
            var existingData = AssetDatabase.LoadAssetAtPath<PMDCharacterData>(dataAssetPath);
            if (existingData != null && existingData.animations != null && existingData.animations.Count > 0)
            {
                return existingData;
            }

            string spritesFolder = $"{targetFolder}/Sprites";
            Directory.CreateDirectory(spritesFolder);

            string animDataXmlContent = null;
            var extractedAnimFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Download and extract sprites.zip
            if (!string.IsNullOrEmpty(form.sprites?.zipUrl))
            {
                SetStatus($"Đang tải #{monster.id:D4} {monster.name} từ {form.sprites.zipUrl}...");
                EditorUtility.DisplayProgressBar(
                    "PMDCollab Importer",
                    $"Đang tải #{monster.id:D4} {monster.name} ({form.fullName})...",
                    0.2f);

                byte[] zipBytes = await SharedHttpClient.GetByteArrayAsync(form.sprites.zipUrl);

                using var ms = new MemoryStream(zipBytes);
                using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    if (string.Equals(entry.Name, "AnimData.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                        animDataXmlContent = await reader.ReadToEndAsync();
                        File.WriteAllText(Path.Combine(targetFolder, "AnimData.xml"), animDataXmlContent, Encoding.UTF8);
                        continue;
                    }

                    if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool isAnimSheet = entry.Name.EndsWith("-Anim.png", StringComparison.OrdinalIgnoreCase);
                    if (!isAnimSheet && !includeShadowsAndOffsets)
                        continue;

                    if (isAnimSheet)
                    {
                        string actionName = entry.Name.Substring(0, entry.Name.Length - "-Anim.png".Length);
                        if (coreOnly && !CoreAnimationNames.Contains(actionName))
                            continue;

                        string outPath = $"{spritesFolder}/{entry.Name}";
                        ExtractEntryToFile(entry, outPath);
                        extractedAnimFiles[actionName] = outPath;
                    }
                    else
                    {
                        string outPath = $"{spritesFolder}/{entry.Name}";
                        ExtractEntryToFile(entry, outPath);
                    }
                }
            }

            // 2. Download Portrait Sheet
            string portraitAssetPath = null;
            if (includePortraits && !string.IsNullOrEmpty(form.portraits?.sheetUrl))
            {
                EditorUtility.DisplayProgressBar(
                    "PMDCollab Importer",
                    $"Đang tải Portrait #{monster.id:D4} {monster.name}...",
                    0.4f);

                byte[] portraitBytes = await SharedHttpClient.GetByteArrayAsync(form.portraits.sheetUrl);
                portraitAssetPath = $"{targetFolder}/Portraits.png";
                File.WriteAllBytes(portraitAssetPath, portraitBytes);
            }

            // 3. Refresh AssetDatabase
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 4. Parse AnimData.xml and slice sprite sheets
            PMDParsedAnimData parsedAnimData = PMDAnimDataParser.ParseXml(animDataXmlContent);
            var importedSequences = new List<PMDAnimSequence>();

            int totalAnims = parsedAnimData.sequences.Count;
            for (int i = 0; i < totalAnims; i++)
            {
                PMDAnimSequence seq = parsedAnimData.sequences[i];

                if (!string.IsNullOrEmpty(seq.copyOf))
                {
                    if (!coreOnly || (CoreAnimationNames.Contains(seq.name) && CoreAnimationNames.Contains(seq.copyOf)))
                    {
                        importedSequences.Add(seq);
                    }
                    continue;
                }

                if (!extractedAnimFiles.TryGetValue(seq.name, out string animAssetPath))
                    continue;

                float progress = 0.45f + (0.4f * (i + 1) / Mathf.Max(1, totalAnims));
                EditorUtility.DisplayProgressBar(
                    "PMDCollab Importer",
                    $"Đang cắt #{monster.id:D4} {monster.name}: {seq.name}-Anim.png ({seq.frameWidth}x{seq.frameHeight})...",
                    progress);

                seq.directionFrames = PMDSpriteSlicer.SliceAnimationSheet(
                    animAssetPath,
                    seq.name,
                    seq.frameWidth,
                    seq.frameHeight,
                    ppu);

                if (seq.directionFrames.Count > 0)
                {
                    importedSequences.Add(seq);
                }
            }

            // 5. Slice Portraits.png
            var importedPortraits = new List<PMDPortraitEntry>();
            if (!string.IsNullOrEmpty(portraitAssetPath) && File.Exists(portraitAssetPath))
            {
                EditorUtility.DisplayProgressBar("PMDCollab Importer", $"Đang cắt Portrait #{monster.id:D4} {monster.name}...", 0.9f);
                importedPortraits = PMDSpriteSlicer.SlicePortraitSheet(portraitAssetPath, 40);
            }

            // 6. Save PMDCharacterData asset
            var characterData = AssetDatabase.LoadAssetAtPath<PMDCharacterData>(dataAssetPath);
            if (characterData == null)
            {
                characterData = CreateInstance<PMDCharacterData>();
                AssetDatabase.CreateAsset(characterData, dataAssetPath);
            }

            characterData.dexId = monster.id;
            characterData.pokemonName = monster.name;
            characterData.formPath = form.path;
            characterData.formFullName = form.fullName;
            characterData.shadowSize = parsedAnimData.shadowSize;
            characterData.spriteCredit = form.sprites?.creditPrimary?.name ?? string.Empty;
            characterData.portraitCredit = form.portraits?.creditPrimary?.name ?? string.Empty;
            characterData.animations = importedSequences;
            characterData.portraits = importedPortraits;

            EditorUtility.SetDirty(characterData);
            AssetDatabase.SaveAssets();

            SetStatus(
                $"Đã import #{monster.id:D4} {monster.name}: {importedSequences.Count} animations 8 hướng & {importedPortraits.Count} portraits.");

            return characterData;
        }

        public async Task BuildCompleteOpenWorldAsync()
        {
            SetButtonsEnabled(false);
            try
            {
                var rosterDataList = new List<PMDCharacterData>();

                // Include currently selected Pokémon first if different from default list
                int selectedDex = _dexIdField != null ? _dexIdField.value : 25;
                var dexList = new List<int> { selectedDex };
                foreach (int id in DefaultOpenWorldRosterDexIds)
                {
                    if (!dexList.Contains(id))
                        dexList.Add(id);
                }

                for (int i = 0; i < dexList.Count; i++)
                {
                    int dexId = dexList[i];
                    SetStatus($"[{i + 1}/{dexList.Count}] Đang chuẩn bị dữ liệu Pokémon #{dexId:D4} cho Thế Giới Mở...");
                    MonsterNode monster = await QuerySingleMonsterAsync(dexId);
                    if (monster?.forms == null || monster.forms.Length == 0)
                        continue;

                    MonsterFormNode baseForm = monster.forms.FirstOrDefault(f => !string.IsNullOrEmpty(f.sprites?.zipUrl));
                    if (baseForm == null)
                        continue;

                    PMDCharacterData data = await ImportMonsterFormInternalAsync(monster, baseForm);
                    if (data != null)
                    {
                        rosterDataList.Add(data);
                    }
                }

                if (rosterDataList.Count == 0)
                {
                    SetStatus("Không thể tải dữ liệu Pokémon nào. Vui lòng kiểm tra kết nối Internet.");
                    return;
                }

                if (_lastImportedDataField != null)
                {
                    _lastImportedDataField.value = rosterDataList[0];
                }

                AssembleOpenWorldScene(rosterDataList);

                SetStatus(
                    $"🌍 Đã dựng xong Bản Đồ Thế Giới Mở (5 vùng sinh thái) với {rosterDataList.Count} loài Pokémon ({string.Join(", ", rosterDataList.Select(d => d.pokemonName))})! Hãy nhấn PLAY để trải nghiệm ngay!");
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi khi tạo Thế Giới Mở: {ex.Message}");
                Debug.LogError($"[PMDCollab] Open World build error: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                SetButtonsEnabled(true);
            }
        }

        private static void AssembleOpenWorldScene(List<PMDCharacterData> roster)
        {
            PMDOpenWorldAutoBootstrap.EnsureOpenWorldOnPlay();

            var worldMgr = FindFirstObjectByType<PMDOpenWorldManager>();
            if (worldMgr != null && worldMgr.Player != null)
            {
                Selection.activeGameObject = worldMgr.Player.gameObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.LookAt(Vector3.zero);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private void SpawnSelectedCharacterInScene()
        {
            var data = _lastImportedDataField?.value as PMDCharacterData;
            if (data == null)
            {
                SetStatus("Vui lòng tải một Pokémon trước hoặc kéo file PMDCharacterData vào ô 'Character Data vừa tạo'.");
                return;
            }

            var go = new GameObject($"Pokemon_{data.dexId:D4}_{data.pokemonName}");
            Undo.RegisterCreatedObjectUndo(go, "Spawn PMD Pokémon");

            go.AddComponent<SpriteRenderer>();
            var sorter = go.AddComponent<PMDYSorter>();
            sorter.shadowSize = data.shadowSize;

            var animator = go.AddComponent<PMDSpriteAnimator>();
            animator.characterData = data;
            animator.currentAnimation = "Idle";
            animator.currentDirection = PMDDirection.Down;
            animator.RefreshSequence(resetFrame: true);

            Selection.activeGameObject = go;
            if (SceneView.lastActiveSceneView != null)
            {
                go.transform.position = Vector3.zero;
                SceneView.lastActiveSceneView.FrameSelected();
            }

            SetStatus($"Đã tạo '{go.name}' trong Scene! Nhấn Play và dùng phím WASD/Mũi tên để di chuyển 8 hướng, Space/J/K/L/Z để test đòn đánh.");
        }

        private static void ExtractEntryToFile(ZipArchiveEntry entry, string destinationPath)
        {
            using Stream inStream = entry.Open();
            using FileStream outStream = File.Create(destinationPath);
            inStream.CopyTo(outStream);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Pokemon";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(' ', '_');
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _fetchFormsButton?.SetEnabled(enabled);
            _downloadAndSliceButton?.SetEnabled(enabled);
            _spawnInSceneButton?.SetEnabled(enabled);
            _buildOpenWorldButton?.SetEnabled(enabled);
        }

        #region GraphQL JSON Serialization Models

        [Serializable]
        private class GraphQlRootResponse
        {
            public GraphQlDataNode data;
        }

        [Serializable]
        private class GraphQlDataNode
        {
            public MonsterNode[] monster;
        }

        [Serializable]
        private class MonsterNode
        {
            public int id;
            public string name;
            public MonsterFormNode[] forms;
        }

        [Serializable]
        private class MonsterFormNode
        {
            public string path;
            public string fullName;
            public PortraitPayloadNode portraits;
            public SpritePayloadNode sprites;
        }

        [Serializable]
        private class PortraitPayloadNode
        {
            public string sheetUrl;
            public CreditNode creditPrimary;
        }

        [Serializable]
        private class SpritePayloadNode
        {
            public string zipUrl;
            public string animDataXml;
            public CreditNode creditPrimary;
        }

        [Serializable]
        private class CreditNode
        {
            public string name;
        }

        #endregion
    }
}
