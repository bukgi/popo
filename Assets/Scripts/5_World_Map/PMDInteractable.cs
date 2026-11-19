using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDInteractKind
    {
        Chest,
        MissionBoard,
        EvolutionAltar,
        Npc,
        Shop,
        Inn,
        Sign
    }

    /// <summary>
    /// Vật thể tương tác bằng phím [F]: Rương kho báu, Bảng nhiệm vụ, Bàn thờ tiến hóa, NPC hội thoại,
    /// Cửa hàng, Nhà trọ (hồi máu + lưu game) và Biển chỉ đường.
    /// </summary>
    public class PMDInteractable : MonoBehaviour
    {
        public static readonly List<PMDInteractable> Active = new List<PMDInteractable>();

        public PMDInteractKind kind;
        public string label = "Tương tác";
        public string speakerName = "";
        public string[] lines;
        public float interactRadius = 1.7f;
        public bool isUsed;
        public bool isRareChest;

        private void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public string PromptText
        {
            get
            {
                if (kind == PMDInteractKind.Chest && isUsed)
                    return null;
                return $"[F] {label}";
            }
        }

        public static PMDInteractable FindNearest(Vector2 pos)
        {
            PMDInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Active.Count; i++)
            {
                var it = Active[i];
                if (it == null || (it.kind == PMDInteractKind.Chest && it.isUsed))
                    continue;
                float d = Vector2.Distance(pos, it.transform.position);
                if (d <= it.interactRadius && d < bestDist)
                {
                    bestDist = d;
                    best = it;
                }
            }
            return best;
        }

        public void Interact(PMDPlayerController player)
        {
            var gs = PMDGameState.Instance;
            if (gs == null || player == null)
                return;

            var mapGen = FindFirstObjectByType<PMDOpenWorldMapGenerator>();
            var zone = mapGen != null ? mapGen.ActiveZone : null;

            switch (kind)
            {
                case PMDInteractKind.Chest:
                    OpenChest(gs, mapGen);
                    break;

                case PMDInteractKind.MissionBoard:
                    gs.GenerateQuestOffers(mapGen != null ? mapGen.ActiveZoneIndex : 0, zone);
                    gs.IsQuestBoardOpen = true;
                    gs.RefreshTimeScale();
                    PMDAudio.Play(PMDSfx.MenuOpen);
                    break;

                case PMDInteractKind.EvolutionAltar:
                    TryEvolve(gs, player);
                    break;

                case PMDInteractKind.Shop:
                    gs.StartDialogue(speakerName, lines ?? new[] { "Chào mừng! Hãy xem hàng của tiệm nhé!" }, () =>
                    {
                        gs.IsShopOpen = true;
                        gs.RefreshTimeScale();
                    });
                    break;

                case PMDInteractKind.Inn:
                    gs.StartDialogue(speakerName, lines ?? new[] { "Nghỉ ngơi một chút nhé..." }, () =>
                    {
                        player.Heal(player.MaxHp);
                        foreach (var mon in player.RecruitedParty)
                            if (mon != null) mon.HealFull();
                        player.ResetAllCooldowns();
                        PMDAudio.Play(PMDSfx.Heal);
                        gs.SaveGame();
                        gs.PushToast("Cả đội đã hồi phục hoàn toàn!", new Color(0.5f, 1f, 0.6f));
                    });
                    break;

                case PMDInteractKind.Npc:
                case PMDInteractKind.Sign:
                default:
                    gs.StartDialogue(speakerName, lines ?? new[] { "..." });
                    break;
            }
        }

        private void OpenChest(PMDGameState gs, PMDOpenWorldMapGenerator mapGen)
        {
            if (isUsed)
                return;
            isUsed = true;

            int coins = Random.Range(40, 110) * (isRareChest ? 3 : 1) + PMDGameState.CurrentLevel * 8;
            gs.AddCoins(coins);

            var table = isRareChest ? PMDGameState.RareLootTable : PMDGameState.FieldLootTable;
            int itemCount = isRareChest ? 3 : 2;
            string summary = $"+{coins} Poké";
            for (int i = 0; i < itemCount; i++)
            {
                string item = table[Random.Range(0, table.Length)];
                if (item.StartsWith("Money"))
                    item = "Apple_Red.None";
                gs.AddItem(item, 1);
                summary += $", +1 {PMDGameState.GetItemInfo(item).displayName}";
            }

            gs.PushToast($"📦 Mở rương: {summary}", new Color(1f, 0.85f, 0.35f));
            PMDAudio.Play(PMDSfx.ChestOpen);
            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", transform.position, 1.3f, 26f);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && mapGen != null)
            {
                Sprite open = mapGen.CreateObjectSprite("Chest_Open.None");
                if (open != null)
                    sr.sprite = open;
                else
                    sr.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        private void TryEvolve(PMDGameState gs, PMDPlayerController player)
        {
            string leader = player.Animator != null && player.Animator.CharacterData != null
                ? player.Animator.CharacterData.pokemonName
                : "";

            string stone = null;
            string target = null;
            if (leader == "Charmander") { stone = "Stone_Red.None"; target = "Charizard"; }
            else if (leader == "Eevee") { stone = "Stone_LightBlue.None"; target = "Glaceon"; }

            if (target == null)
            {
                gs.StartDialogue("Bàn Thờ Tiến Hóa", new[]
                {
                    "Một luồng ánh sáng huyền bí bao quanh bàn thờ...",
                    $"{leader} chưa thể tiến hóa ở đây.",
                    "(Charmander cần Đá Lửa → Charizard. Eevee cần Đá Băng → Glaceon.)"
                });
                return;
            }

            if (gs.CountItem(stone) <= 0)
            {
                gs.StartDialogue("Bàn Thờ Tiến Hóa", new[]
                {
                    $"{leader} đang phát sáng nhẹ...",
                    $"Cần 1 {PMDGameState.GetItemInfo(stone).displayName} để tiến hóa thành {target}.",
                    "(Mua ở Bưu Điện - Vùng 1, hoặc tìm trong rương quý/Trùm.)"
                });
                return;
            }

            if (PMDGameState.CurrentLevel < 8)
            {
                gs.StartDialogue("Bàn Thờ Tiến Hóa", new[] { $"{leader} cần đạt ít nhất Cấp 8 để tiến hóa!" });
                return;
            }

            var mgr = PMDOpenWorldManager.Instance;
            var data = mgr != null ? mgr.FindCharacterByName(target) : null;
            if (data == null)
            {
                gs.StartDialogue("Bàn Thờ Tiến Hóa", new[] { $"Không tìm thấy dữ liệu sprite của {target}." });
                return;
            }

            gs.RemoveItem(stone);
            gs.StartDialogue("Bàn Thờ Tiến Hóa", new[]
            {
                $"Ồ!? {leader} đang tiến hóa!",
                $"Chúc mừng! {leader} đã tiến hóa thành {target}!"
            }, () =>
            {
                player.EvolveInto(data);
                PMDAudio.Play(PMDSfx.Evolve);
            });
        }
    }
}

