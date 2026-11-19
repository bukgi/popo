using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDItemCategory
    {
        Heal,
        Exp,
        Recruit,
        Evolution,
        Revive,
        Treasure
    }

    public class PMDItemInfo
    {
        public string file;
        public string displayName;
        public string description;
        public PMDItemCategory category;
        public float value;      // heal amount / exp amount / heal percent
        public int buyPrice;
        public int SellPrice => Mathf.Max(5, buyPrice / 2);
    }

    public enum PMDQuestType
    {
        DefeatAny,
        DefeatElement,
        CollectItems,
        DefeatBoss,
        Recruit
    }

    [Serializable]
    public class PMDQuest
    {
        public string title;
        public PMDQuestType type;
        public int elementTarget;
        public int zoneIndex;
        public int required;
        public int progress;
        public int rewardCoins;
        public int rewardExp;
        public string rewardItem;
        public bool completed;
    }

    [Serializable]
    public class PMDSaveData
    {
        public int level = 1;
        public int exp;
        public int coins;
        public float playerHp = -1f;
        public List<string> itemKeys = new List<string>();
        public List<int> itemCounts = new List<int>();
        public string leaderSpecies = "Pikachu";
        public List<string> partySpecies = new List<string>();
        public int zoneIndex;
        public int seed;
        public List<PMDQuest> quests = new List<PMDQuest>();
        public List<int> defeatedBossZones = new List<int>();
        public int totalKills;
        public float musicVolume = 0.55f;
        public float sfxVolume = 0.8f;
        public float uiScale = 1f;
        public bool showMinimap = true;
    }

    /// <summary>
    /// Trạng thái toàn bộ game: Cấp độ/EXP, Túi đồ, Tiền Poké, Nhiệm vụ, Lưu/Tải game, Cài đặt, Tạm dừng & Thua cuộc.
    /// </summary>
    public class PMDGameState : MonoBehaviour
    {
        public static PMDGameState Instance { get; private set; }

        public const int MaxPartySize = 4;
        public const int MaxActiveQuests = 3;

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int Coins { get; private set; }
        public int TotalKills { get; private set; }
        public readonly Dictionary<string, int> Inventory = new Dictionary<string, int>();
        public readonly List<PMDQuest> ActiveQuests = new List<PMDQuest>();
        public readonly List<PMDQuest> QuestOffers = new List<PMDQuest>();
        public readonly HashSet<int> DefeatedBossZones = new HashSet<int>();

        // Settings
        public float MusicVolume = 0.55f;
        public float SfxVolume = 0.8f;
        public float UiScale = 1f;
        public bool ShowMinimap = true;

        // Modal / flow state
        public bool IsMainMenuOpen = false;
        public bool IsPaused;
        public bool IsGameOver;
        public bool IsInventoryOpen;
        public bool IsQuestBoardOpen;
        public bool IsShopOpen;
        public bool IsSettingsOpen;
        public bool IsHelpOpen;

        // Dialogue
        public string DialogueSpeaker;
        public string[] DialogueLines;
        public int DialogueIndex;
        public Action OnDialogueFinished;
        public bool IsDialogueOpen => DialogueLines != null && DialogueIndex < DialogueLines.Length;

        // Toasts
        public struct ToastEntry
        {
            public string text;
            public Color color;
            public float time;
        }
        public readonly List<ToastEntry> Toasts = new List<ToastEntry>();

        public static bool IsInputBlocked =>
            Instance != null && (Instance.IsMainMenuOpen || Instance.IsPaused || Instance.IsGameOver ||
                                 Instance.IsInventoryOpen || Instance.IsQuestBoardOpen || Instance.IsShopOpen ||
                                 Instance.IsDialogueOpen || Instance.IsSettingsOpen || Instance.IsHelpOpen);

        public static string SavePath => Path.Combine(Application.persistentDataPath, "pmd_openworld_save.json");
        public static bool HasSave => File.Exists(SavePath);

        // =========================================================================================
        // ITEM CATALOG
        // =========================================================================================
        public static readonly Dictionary<string, PMDItemInfo> ItemCatalog = BuildCatalog();

        private static Dictionary<string, PMDItemInfo> BuildCatalog()
        {
            var d = new Dictionary<string, PMDItemInfo>(StringComparer.OrdinalIgnoreCase);
            void Add(string file, string name, string desc, PMDItemCategory cat, float value, int price)
            {
                d[file] = new PMDItemInfo { file = file, displayName = name, description = desc, category = cat, value = value, buyPrice = price };
            }

            Add("Apple_Red.None", "Táo Đỏ", "Hồi 40 HP.", PMDItemCategory.Heal, 40f, 30);
            Add("Banana_Yellow.None", "Chuối Vàng", "Hồi 60 HP.", PMDItemCategory.Heal, 60f, 45);
            Add("Berry_Oran.None", "Quả Oran", "Hồi 80 HP.", PMDItemCategory.Heal, 80f, 60);
            Add("Berry_Sitrus.None", "Quả Sitrus", "Hồi 40% HP tối đa.", PMDItemCategory.Heal, -0.4f, 90);
            Add("Berry_Lum.None", "Quả Lum", "Hồi 30 HP và làm mới toàn bộ hồi chiêu.", PMDItemCategory.Heal, 30f, 80);
            Add("Apple_Gold.None", "Táo Vàng", "Hồi đầy HP cho cả đội.", PMDItemCategory.Heal, -1f, 180);
            Add("Gummi_Blue.None", "Kẹo Gummi Xanh", "Nhận 50 EXP.", PMDItemCategory.Exp, 50f, 110);
            Add("Gummi_Gold.None", "Kẹo Gummi Vàng", "Nhận 120 EXP.", PMDItemCategory.Exp, 120f, 240);
            Add("Apricorn_Red.None", "Apricorn Đỏ", "Ném cầu tăng +22% tỉ lệ thu phục Pokémon đang ngất ngư.", PMDItemCategory.Recruit, 0f, 120);
            Add("Apricorn_Blue.None", "Apricorn Xanh", "Ném cầu tăng +22% tỉ lệ thu phục Pokémon đang ngất ngư.", PMDItemCategory.Recruit, 0f, 120);
            Add("Apricorn_Yellow.None", "Apricorn Vàng", "Ném cầu tăng +22% tỉ lệ thu phục Pokémon đang ngất ngư.", PMDItemCategory.Recruit, 0f, 120);
            Add("Stone_Red.None", "Đá Lửa", "Dùng tại Bàn Thờ Tiến Hóa: Charmander → Charizard.", PMDItemCategory.Evolution, 0f, 300);
            Add("Stone_LightBlue.None", "Đá Băng", "Dùng tại Bàn Thờ Tiến Hóa: Eevee → Glaceon.", PMDItemCategory.Evolution, 0f, 300);
            Add("Seed_White.None", "Hạt Hồi Sinh", "Tự động hồi sinh với 50% HP khi gục ngã.", PMDItemCategory.Revive, 0.5f, 250);
            Add("Stone_Blue.None", "Đá Xanh", "Kho báu - bán lấy Poké.", PMDItemCategory.Treasure, 0f, 120);
            Add("Stone_Gold.None", "Đá Vàng", "Kho báu quý - bán lấy Poké.", PMDItemCategory.Treasure, 0f, 300);
            Add("Pearl.None", "Ngọc Trai", "Kho báu - bán lấy Poké.", PMDItemCategory.Treasure, 0f, 200);
            Add("Crystal_Blue.None", "Pha Lê Xanh", "Kho báu - bán lấy Poké.", PMDItemCategory.Treasure, 0f, 260);
            return d;
        }

        public static readonly string[] ShopStock =
        {
            "Apple_Red.None", "Banana_Yellow.None", "Berry_Oran.None", "Berry_Sitrus.None", "Berry_Lum.None",
            "Apple_Gold.None", "Gummi_Blue.None", "Apricorn_Red.None", "Seed_White.None", "Stone_Red.None", "Stone_LightBlue.None"
        };

        public static readonly string[] FieldLootTable =
        {
            "Apple_Red.None", "Apple_Red.None", "Banana_Yellow.None", "Berry_Oran.None", "Berry_Oran.None",
            "Berry_Sitrus.None", "Berry_Lum.None", "Apricorn_Red.None", "Apricorn_Blue.None", "Gummi_Blue.None",
            "Stone_Blue.None", "Pearl.None", "Money_Yellow.None", "Money_Yellow.None", "Seed_White.None"
        };

        public static readonly string[] RareLootTable =
        {
            "Apple_Gold.None", "Gummi_Gold.None", "Stone_Gold.None", "Crystal_Blue.None", "Seed_White.None",
            "Stone_Red.None", "Stone_LightBlue.None", "Apricorn_Yellow.None"
        };

        public static PMDItemInfo GetItemInfo(string file)
        {
            if (!string.IsNullOrEmpty(file) && ItemCatalog.TryGetValue(file, out var info))
                return info;
            return new PMDItemInfo
            {
                file = file,
                displayName = string.IsNullOrEmpty(file) ? "?" : file.Replace(".None", "").Replace("_", " "),
                description = "Vật phẩm lạ.",
                category = PMDItemCategory.Treasure,
                buyPrice = 20
            };
        }

        // =========================================================================================
        // LIFECYCLE
        // =========================================================================================
        private void Awake()
        {
            Instance = this;
            IsMainMenuOpen = false;
            Time.timeScale = 1f;
            if (Inventory.Count == 0)
            {
                ResetForNewGame();
            }
        }

        private void Update()
        {
            if (!IsPaused && Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }

            var kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (IsDialogueOpen || IsInventoryOpen || IsQuestBoardOpen || IsShopOpen || IsSettingsOpen || IsHelpOpen || IsMainMenuOpen)
                {
                    DialogueLines = null;
                    IsInventoryOpen = false;
                    IsQuestBoardOpen = false;
                    IsShopOpen = false;
                    IsSettingsOpen = false;
                    IsHelpOpen = false;
                    IsMainMenuOpen = false;
                    IsPaused = false;
                    RefreshTimeScale();
                }
                else
                {
                    IsPaused = !IsPaused;
                    RefreshTimeScale();
                }
                return;
            }

            if (IsDialogueOpen)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)
                {
                    AdvanceDialogue();
                }
                return;
            }

            if (kb.iKey.wasPressedThisFrame)
            {
                IsInventoryOpen = !IsInventoryOpen;
                IsQuestBoardOpen = false;
                IsShopOpen = false;
            }
            else if (kb.bKey.wasPressedThisFrame)
            {
                IsQuestBoardOpen = !IsQuestBoardOpen;
                IsInventoryOpen = false;
                IsShopOpen = false;
                if (IsQuestBoardOpen && QuestOffers.Count == 0)
                {
                    GenerateQuestOffers(0);
                }
            }
            else if (kb.mKey.wasPressedThisFrame)
            {
                ShowMinimap = !ShowMinimap;
            }
            else if (kb.f5Key.wasPressedThisFrame)
            {
                SaveGame();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        public void RefreshTimeScale()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        public void ResetForNewGame()
        {
            Level = 1;
            Exp = 0;
            Coins = 150;
            TotalKills = 0;
            Inventory.Clear();
            ActiveQuests.Clear();
            QuestOffers.Clear();
            DefeatedBossZones.Clear();
            AddItem("Apple_Red.None", 3, false);
            AddItem("Berry_Oran.None", 2, false);
            AddItem("Apricorn_Red.None", 1, false);
            AddItem("Seed_White.None", 1, false);
        }

        // =========================================================================================
        // STATS & PROGRESSION
        // =========================================================================================
        public static int CurrentLevel => Instance != null ? Instance.Level : 1;
        public static float PlayerMaxHp => 150f + (CurrentLevel - 1) * 18f;
        public static float PlayerDamageMultiplier => 1f + (CurrentLevel - 1) * 0.09f;
        public static float PlayerDefenseMultiplier => 1f / (1f + (CurrentLevel - 1) * 0.05f);
        public int ExpToNextLevel => 30 + (Level - 1) * 28;

        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            Exp += amount;
            bool leveled = false;
            while (Exp >= ExpToNextLevel && Level < 60)
            {
                Exp -= ExpToNextLevel;
                Level++;
                leveled = true;
            }

            if (leveled)
            {
                var player = PMDPlayerController.Instance;
                if (player != null)
                {
                    player.OnLevelUp();
                }
                PushToast($"★ LÊN CẤP {Level}! HP tối đa {PlayerMaxHp:0}, Sát thương x{PlayerDamageMultiplier:0.00}", new Color(1f, 0.9f, 0.3f));
                PMDAudio.Play(PMDSfx.LevelUp);
            }
        }

        public void AddCoins(int amount)
        {
            Coins = Mathf.Max(0, Coins + amount);
        }

        public void RegisterRecruit()
        {
            ProgressQuests(PMDQuestType.Recruit, -1, 1);
        }

        public void RegisterKill(string species, bool isBoss, int zoneIndex, int monLevel)
        {
            TotalKills++;
            int expGain = 8 + monLevel * 4;
            int coinGain = 4 + monLevel * 2 + UnityEngine.Random.Range(0, 6);
            if (isBoss)
            {
                expGain *= 6;
                coinGain *= 8;
                DefeatedBossZones.Add(zoneIndex);
                ProgressQuests(PMDQuestType.DefeatBoss, zoneIndex, 1);
            }

            AddCoins(coinGain);
            AddExp(expGain);

            ProgressQuests(PMDQuestType.DefeatAny, -1, 1);
            var types = PMDTypeChart.GetSpeciesTypes(species);
            for (int i = 0; i < types.Length; i++)
                ProgressQuests(PMDQuestType.DefeatElement, (int)types[i], 1);
        }

        // =========================================================================================
        // INVENTORY
        // =========================================================================================
        public int CountItem(string file)
        {
            return Inventory.TryGetValue(file, out int c) ? c : 0;
        }

        public void AddItem(string file, int count = 1, bool trackQuest = true)
        {
            if (string.IsNullOrEmpty(file) || count <= 0)
                return;

            if (file.StartsWith("Money", StringComparison.OrdinalIgnoreCase))
            {
                int amount = UnityEngine.Random.Range(20, 60) * count;
                AddCoins(amount);
                PushToast($"+{amount} Poké", new Color(1f, 0.85f, 0.3f));
            }
            else
            {
                Inventory[file] = CountItem(file) + count;
            }

            if (trackQuest)
                ProgressQuests(PMDQuestType.CollectItems, -1, count);
        }

        public bool RemoveItem(string file, int count = 1)
        {
            int have = CountItem(file);
            if (have < count)
                return false;
            if (have - count <= 0)
                Inventory.Remove(file);
            else
                Inventory[file] = have - count;
            return true;
        }

        /// <summary>
        /// Uses an item from the bag. Returns true if consumed.
        /// </summary>
        public bool UseItem(string file)
        {
            var player = PMDPlayerController.Instance;
            if (player == null || CountItem(file) <= 0)
                return false;

            var info = GetItemInfo(file);
            switch (info.category)
            {
                case PMDItemCategory.Heal:
                {
                    if (info.value < -0.5f)
                    {
                        player.Heal(player.MaxHp);
                        foreach (var mon in player.RecruitedParty)
                            if (mon != null) mon.HealFull();
                    }
                    else if (info.value < 0f)
                    {
                        player.Heal(player.MaxHp * -info.value);
                    }
                    else
                    {
                        player.Heal(info.value);
                    }

                    if (file.StartsWith("Berry_Lum", StringComparison.OrdinalIgnoreCase))
                        player.ResetAllCooldowns();

                    PMDAudio.Play(PMDSfx.Heal);
                    break;
                }
                case PMDItemCategory.Exp:
                    AddExp(Mathf.RoundToInt(info.value));
                    PMDVfxPlayer.SpawnFloatingText(player.transform.position, $"+{info.value:0} EXP", new Color(0.6f, 0.85f, 1f));
                    break;
                case PMDItemCategory.Recruit:
                    if (!player.TryRecruitWithApricorn())
                    {
                        PMDAudio.Play(PMDSfx.Error);
                        return false;
                    }
                    IsInventoryOpen = false;
                    break;
                case PMDItemCategory.Evolution:
                    PushToast("Hãy mang đá này đến Bàn Thờ Tiến Hóa (Vùng 5 hoặc Vùng 8) và bấm [F]!", new Color(1f, 0.75f, 0.4f));
                    PMDAudio.Play(PMDSfx.Error);
                    return false;
                case PMDItemCategory.Revive:
                    PushToast("Hạt Hồi Sinh sẽ tự kích hoạt khi bạn gục ngã.", new Color(0.7f, 0.95f, 0.7f));
                    return false;
                default:
                    PushToast($"{info.displayName} chỉ có thể bán ở cửa hàng.", new Color(0.9f, 0.9f, 0.9f));
                    return false;
            }

            RemoveItem(file);
            return true;
        }

        public string FindBestHealingItem(float missingHp)
        {
            string best = null;
            float bestScore = float.MaxValue;
            foreach (var kv in Inventory)
            {
                var info = GetItemInfo(kv.Key);
                if (info.category != PMDItemCategory.Heal || kv.Value <= 0)
                    continue;

                float heal = info.value < -0.5f ? 9999f : (info.value < 0f ? PlayerMaxHp * -info.value : info.value);
                float waste = Mathf.Abs(heal - missingHp) + info.buyPrice * 0.05f;
                if (waste < bestScore)
                {
                    bestScore = waste;
                    best = kv.Key;
                }
            }
            return best;
        }

        public bool BuyItem(string file)
        {
            var info = GetItemInfo(file);
            if (Coins < info.buyPrice)
            {
                PushToast("Không đủ Poké!", new Color(1f, 0.45f, 0.4f));
                PMDAudio.Play(PMDSfx.Error);
                return false;
            }
            Coins -= info.buyPrice;
            Inventory[file] = CountItem(file) + 1;
            PMDAudio.Play(PMDSfx.Coin);
            return true;
        }

        public bool SellItem(string file)
        {
            if (!RemoveItem(file))
                return false;
            AddCoins(GetItemInfo(file).SellPrice);
            PMDAudio.Play(PMDSfx.Coin);
            return true;
        }

        // =========================================================================================
        // QUESTS
        // =========================================================================================
        public void GenerateQuestOffers(int zoneIndex, PMDMapZoneInfo zone = null)
        {
            QuestOffers.Clear();
            var rng = new System.Random(Environment.TickCount);
            int lvl = Mathf.Max(1, Level);

            QuestOffers.Add(new PMDQuest
            {
                title = $"Dọn dẹp khu vực: Hạ gục {5 + lvl / 2} Pokémon hoang dã",
                type = PMDQuestType.DefeatAny,
                required = 5 + lvl / 2,
                rewardCoins = 120 + lvl * 15,
                rewardExp = 40 + lvl * 10,
                rewardItem = "Berry_Oran.None"
            });

            PMDElement el = PMDElement.Fire;
            if (zone != null && zone.preferredSpecies != null && zone.preferredSpecies.Length > 0)
            {
                string sp = zone.preferredSpecies[rng.Next(zone.preferredSpecies.Length)];
                int us = sp.IndexOf('_');
                el = PMDTypeChart.GetSpeciesTypes(us >= 0 ? sp.Substring(us + 1) : sp)[0];
            }
            QuestOffers.Add(new PMDQuest
            {
                title = $"Săn hệ {PMDTypeChart.ElementName(el)}: Hạ gục 3 Pokémon hệ {PMDTypeChart.ElementName(el)}",
                type = PMDQuestType.DefeatElement,
                elementTarget = (int)el,
                required = 3,
                rewardCoins = 160 + lvl * 15,
                rewardExp = 60 + lvl * 12,
                rewardItem = "Gummi_Blue.None"
            });

            int r = rng.Next(3);
            if (r == 0)
            {
                QuestOffers.Add(new PMDQuest
                {
                    title = $"Truy nã: Đánh bại Trùm của {(zone != null ? zone.displayName : "vùng này")}",
                    type = PMDQuestType.DefeatBoss,
                    zoneIndex = zoneIndex,
                    required = 1,
                    rewardCoins = 400 + lvl * 30,
                    rewardExp = 150 + lvl * 20,
                    rewardItem = "Apple_Gold.None"
                });
            }
            else if (r == 1)
            {
                QuestOffers.Add(new PMDQuest
                {
                    title = "Thu thập: Nhặt 5 vật phẩm trên bản đồ",
                    type = PMDQuestType.CollectItems,
                    required = 5,
                    rewardCoins = 140 + lvl * 12,
                    rewardExp = 45 + lvl * 8,
                    rewardItem = "Apricorn_Blue.None"
                });
            }
            else
            {
                QuestOffers.Add(new PMDQuest
                {
                    title = "Kết bạn: Thu phục 1 đồng đội mới",
                    type = PMDQuestType.Recruit,
                    required = 1,
                    rewardCoins = 180 + lvl * 12,
                    rewardExp = 70 + lvl * 10,
                    rewardItem = "Seed_White.None"
                });
            }
        }

        public bool AcceptQuest(PMDQuest quest)
        {
            if (ActiveQuests.Count >= MaxActiveQuests)
            {
                PushToast($"Tối đa {MaxActiveQuests} nhiệm vụ cùng lúc!", new Color(1f, 0.5f, 0.4f));
                PMDAudio.Play(PMDSfx.Error);
                return false;
            }
            ActiveQuests.Add(quest);
            QuestOffers.Remove(quest);
            PushToast($"Nhận nhiệm vụ: {quest.title}", new Color(0.6f, 0.9f, 1f));
            PMDAudio.Play(PMDSfx.MenuOpen);
            return true;
        }

        private void ProgressQuests(PMDQuestType type, int target, int amount)
        {
            for (int i = ActiveQuests.Count - 1; i >= 0; i--)
            {
                var q = ActiveQuests[i];
                if (q.completed || q.type != type)
                    continue;
                if (type == PMDQuestType.DefeatElement && q.elementTarget != target)
                    continue;
                if (type == PMDQuestType.DefeatBoss && q.zoneIndex != target)
                    continue;

                q.progress = Mathf.Min(q.required, q.progress + amount);
                if (q.progress >= q.required)
                {
                    q.completed = true;
                    ActiveQuests.RemoveAt(i);
                    AddCoins(q.rewardCoins);
                    if (!string.IsNullOrEmpty(q.rewardItem))
                        Inventory[q.rewardItem] = CountItem(q.rewardItem) + 1;
                    PushToast($"✔ HOÀN THÀNH: {q.title}  (+{q.rewardCoins} Poké, +{q.rewardExp} EXP, +1 {GetItemInfo(q.rewardItem).displayName})",
                        new Color(0.45f, 1f, 0.55f));
                    PMDAudio.Play(PMDSfx.QuestComplete);
                    AddExp(q.rewardExp);
                }
            }
        }

        // =========================================================================================
        // DIALOGUE & TOASTS
        // =========================================================================================
        public void StartDialogue(string speaker, string[] lines, Action onFinished = null)
        {
            DialogueSpeaker = speaker;
            DialogueLines = lines;
            DialogueIndex = 0;
            OnDialogueFinished = onFinished;
            PMDAudio.Play(PMDSfx.MenuOpen);
        }

        public void AdvanceDialogue()
        {
            if (!IsDialogueOpen)
                return;
            DialogueIndex++;
            PMDAudio.Play(PMDSfx.MenuClick);
            if (!IsDialogueOpen)
            {
                var cb = OnDialogueFinished;
                OnDialogueFinished = null;
                DialogueLines = null;
                cb?.Invoke();
            }
        }

        public void PushToast(string text, Color color)
        {
            Toasts.Add(new ToastEntry { text = text, color = color, time = Time.unscaledTime });
            if (Toasts.Count > 6)
                Toasts.RemoveAt(0);
        }

        public static void Toast(string text, Color color)
        {
            if (Instance != null)
                Instance.PushToast(text, color);
        }

        // =========================================================================================
        // GAME OVER
        // =========================================================================================
        public void TriggerGameOver()
        {
            if (IsGameOver)
                return;

            IsGameOver = true;
            int lostCoins = Coins / 2;
            Coins -= lostCoins;

            // PMD rule: lose about half of the items in the bag
            var keys = new List<string>(Inventory.Keys);
            int lostItems = 0;
            foreach (var k in keys)
            {
                int c = Inventory[k];
                int lose = c / 2 + (c % 2 == 1 && UnityEngine.Random.value < 0.5f ? 1 : 0);
                if (lose > 0)
                {
                    RemoveItem(k, lose);
                    lostItems += lose;
                }
            }

            LastGameOverSummary = $"Mất {lostCoins} Poké và {lostItems} vật phẩm.";
            PMDAudio.Play(PMDSfx.GameOver);
            RefreshTimeScale();
        }

        public string LastGameOverSummary = string.Empty;

        // =========================================================================================
        // SAVE / LOAD
        // =========================================================================================
        public void SaveGame()
        {
            var data = new PMDSaveData
            {
                level = Level,
                exp = Exp,
                coins = Coins,
                totalKills = TotalKills,
                musicVolume = MusicVolume,
                sfxVolume = SfxVolume,
                uiScale = UiScale,
                showMinimap = ShowMinimap
            };

            foreach (var kv in Inventory)
            {
                data.itemKeys.Add(kv.Key);
                data.itemCounts.Add(kv.Value);
            }
            data.quests.AddRange(ActiveQuests);
            data.defeatedBossZones.AddRange(DefeatedBossZones);

            var player = PMDPlayerController.Instance;
            if (player != null)
            {
                data.playerHp = player.CurrentHp;
                if (player.Animator != null && player.Animator.CharacterData != null)
                    data.leaderSpecies = player.Animator.CharacterData.pokemonName;
                foreach (var mon in player.RecruitedParty)
                    if (mon != null)
                        data.partySpecies.Add(mon.PokemonName);
            }

            var mapGen = FindFirstObjectByType<PMDOpenWorldMapGenerator>();
            if (mapGen != null)
            {
                data.zoneIndex = mapGen.LastOutdoorZoneIndex;
                data.seed = mapGen.LastOutdoorSeed;
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                PushToast("💾 Đã lưu game!", new Color(0.55f, 0.95f, 1f));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PMDGameState] Save failed: {e.Message}");
                PushToast("Lưu game thất bại!", new Color(1f, 0.4f, 0.4f));
            }
        }

        public PMDSaveData LoadSaveData()
        {
            if (!HasSave)
                return null;
            try
            {
                return JsonUtility.FromJson<PMDSaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PMDGameState] Load failed: {e.Message}");
                return null;
            }
        }

        public void ApplySaveData(PMDSaveData data)
        {
            Level = Mathf.Max(1, data.level);
            Exp = Mathf.Max(0, data.exp);
            Coins = Mathf.Max(0, data.coins);
            TotalKills = data.totalKills;
            Inventory.Clear();
            for (int i = 0; i < data.itemKeys.Count && i < data.itemCounts.Count; i++)
                Inventory[data.itemKeys[i]] = data.itemCounts[i];
            ActiveQuests.Clear();
            if (data.quests != null)
                ActiveQuests.AddRange(data.quests);
            DefeatedBossZones.Clear();
            if (data.defeatedBossZones != null)
                foreach (var z in data.defeatedBossZones)
                    DefeatedBossZones.Add(z);
            ApplySettings(data);
        }

        public void LoadSettingsOnly()
        {
            var data = LoadSaveData();
            if (data != null)
                ApplySettings(data);
        }

        private void ApplySettings(PMDSaveData data)
        {
            MusicVolume = Mathf.Clamp01(data.musicVolume);
            SfxVolume = Mathf.Clamp01(data.sfxVolume);
            UiScale = Mathf.Clamp(data.uiScale <= 0f ? 1f : data.uiScale, 0.6f, 1.6f);
            ShowMinimap = data.showMinimap;
        }
    }
}

