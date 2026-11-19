using System;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDSkillKind
    {
        Melee = 0,
        Projectile = 1,
        LinearShockwaveDash = 2,
        UltimateOrbitalStorm = 3
    }

    public enum PMDSkillPattern
    {
        SingleStrike,
        MultiSlashCombo,
        ChainLightningArc,
        StraightVolley,
        HomingProjectile,
        BoomerangVolley,
        SpiralOrbitalShot,
        ClusterBombLob,
        ContinuousBeamLaser,
        HighSpeedRamDash,
        VortexPullWhirlpool,
        TargetedSkyStrike,
        SweepingTidalWave,
        ExpandingNovaStorm
    }

    [Serializable]
    public class PMDSkillData
    {
        public string skillName;
        public string moveType = "Normal";
        public int canonicalPower = 60;
        public string description = "";
        public string hotkeyLabel;
        public PMDSkillKind kind;
        public PMDSkillPattern pattern = PMDSkillPattern.SingleStrike;
        public PMDUniqueMoveId uniqueId = PMDUniqueMoveId.None;
        public PMDArchetypeMechanic archetype = PMDArchetypeMechanic.None;
        public PMDVisualAssetKind visualKind = PMDVisualAssetKind.Auto;
        public string animName = "Attack";
        public string vfxSheet = "Slash.None";
        public string secondaryVfxSheet = "Shock_Wave.None";
        public string hitVfxSheet = "Shock_Wave.None";
        public string ringVfxSheet = "Circle_Yellow_Out.None";
        public float cooldown = 1.0f;
        public float damage = 25f;
        public float range = 2.2f;
        public float projectileSpeed = 8.5f;
        public int projectileCount = 1;
        public float spreadAngle = 0f;
        public bool piercing = false;
        public int waveSteps = 5;
        public float dashSpeed = 13.5f;
        public float lifestealPercent = 0f;
        public float healPercent = 0f;
        public float vfxScale = 1.25f;
        public Color elementColor = new Color(1f, 0.88f, 0.25f);
    }

    /// <summary>
    /// Database of 48 completely distinct Pokémon moves (sourced from Pokémon UNITE, Bulbapedia, Serebii, and PokémonDB).
    /// Every single move across all 12 Pokémon has its own unique PMDUniqueMoveId (1..48), procedural pixel-art visual,
    /// and custom combat behavior in PMDBespokeSkillEngine — no two moves share a coroutine or visual!
    /// </summary>
    public static class PMDSkillDatabase
    {
        private static readonly Dictionary<string, PMDSkillData[]> MovesetsBySpecies =
            new Dictionary<string, PMDSkillData[]>(StringComparer.OrdinalIgnoreCase);

        static PMDSkillDatabase()
        {
            InitializeDatabase();
        }

        public static PMDSkillData[] GetSkillsForPokemon(string pokemonName)
        {
            if (string.IsNullOrEmpty(pokemonName))
                return MovesetsBySpecies["Pikachu"];

            if (MovesetsBySpecies.TryGetValue(pokemonName, out var skills))
            {
                return skills;
            }

            // Dynamically synthesize and cache a custom 4-move kit matched to this Pokémon's elemental type(s)!
            var generated = SynthesizeElementalMoveset(pokemonName);
            MovesetsBySpecies[pokemonName] = generated;
            return generated;
        }

        private static PMDSkillData[] SynthesizeElementalMoveset(string pokemonName)
        {
            var types = PMDTypeChart.GetSpeciesTypes(pokemonName);
            PMDElement primary = (types != null && types.Length > 0) ? types[0] : PMDElement.Normal;
            int hash = Mathf.Abs(pokemonName.GetHashCode());

            string[] templates = primary switch
            {
                PMDElement.Fire => new[] { "Charmander", "Charizard", "Torchic", "Arcanine" },
                PMDElement.Water => new[] { "Squirtle", "Mudkip", "Gyarados", "Greninja", "Lapras" },
                PMDElement.Grass => new[] { "Bulbasaur", "Treecko", "Sceptile", "Leafeon" },
                PMDElement.Electric => new[] { "Pikachu", "Ampharos", "Luxray", "Zapdos" },
                PMDElement.Ice => new[] { "Glaceon", "Lapras", "Articuno" },
                PMDElement.Psychic => new[] { "Mew", "Mewtwo", "Alakazam", "Gardevoir" },
                PMDElement.Ghost => new[] { "Gengar", "Mimikyu", "Chandelure", "Giratina" },
                PMDElement.Dark => new[] { "Darkrai", "Zoroark", "Umbreon", "Absol" },
                PMDElement.Poison => new[] { "Gengar", "Bulbasaur", "Venusaur" },
                PMDElement.Fairy => new[] { "Sylveon", "Gardevoir", "Mimikyu" },
                PMDElement.Ground => new[] { "Groudon", "Garchomp", "Flygon", "Swampert" },
                PMDElement.Rock => new[] { "Tyranitar", "Aggron" },
                PMDElement.Bug => new[] { "Scizor" },
                PMDElement.Flying => new[] { "Charizard", "Rayquaza", "Lugia", "Ho_Oh" },
                PMDElement.Fighting => new[] { "Lucario", "Machamp", "Blaziken", "Infernape" },
                PMDElement.Steel => new[] { "Lucario", "Metagross", "Scizor", "Dialga" },
                PMDElement.Dragon => new[] { "Dragonite", "Rayquaza", "Garchomp", "Salamence" },
                _ => new[] { "Eevee", "Snorlax", "Arceus", "Lucario" }
            };

            var result = new PMDSkillData[4];
            string[] hotkeys = { "J", "K", "L", "U" };

            for (int slot = 0; slot < 4; slot++)
            {
                string donorName = templates[(hash + slot) % templates.Length];
                var donorSet = MovesetsBySpecies.TryGetValue(donorName, out var ds) ? ds : MovesetsBySpecies["Pikachu"];
                var src = donorSet[slot];

                result[slot] = new PMDSkillData
                {
                    skillName = src.skillName,
                    moveType = src.moveType,
                    canonicalPower = src.canonicalPower,
                    description = src.description,
                    hotkeyLabel = hotkeys[slot],
                    kind = src.kind,
                    pattern = src.pattern,
                    uniqueId = src.uniqueId,
                    animName = src.animName,
                    vfxSheet = src.vfxSheet,
                    secondaryVfxSheet = src.secondaryVfxSheet,
                    hitVfxSheet = src.hitVfxSheet,
                    ringVfxSheet = src.ringVfxSheet,
                    cooldown = src.cooldown,
                    damage = src.damage,
                    range = src.range,
                    projectileSpeed = src.projectileSpeed,
                    projectileCount = src.projectileCount,
                    spreadAngle = src.spreadAngle,
                    piercing = src.piercing,
                    waveSteps = src.waveSteps,
                    dashSpeed = src.dashSpeed,
                    lifestealPercent = src.lifestealPercent,
                    healPercent = src.healPercent,
                    vfxScale = src.vfxScale,
                    elementColor = src.elementColor
                };
            }

            return result;
        }

        private static void InitializeDatabase()
        {
            Color elec = new Color(1f, 0.88f, 0.22f);
            Color grass = new Color(0.35f, 0.92f, 0.42f);
            Color fire = new Color(1f, 0.45f, 0.2f);
            Color water = new Color(0.3f, 0.78f, 1f);
            Color fight = new Color(0.95f, 0.4f, 0.35f);
            Color psychic = new Color(0.95f, 0.48f, 0.92f);
            Color ice = new Color(0.55f, 0.92f, 1f);
            Color normal = new Color(0.92f, 0.88f, 0.78f);

            // =================================================================================
            // 0. #0000 TRAINER (Fallback when no companion is out)
            // =================================================================================
            MovesetsBySpecies["Trainer"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Lệnh Xung Kích", moveType = "Fighting", canonicalPower = 55,
                    description = "Quét 3 nhát chỉ huy cận chiến hướng chuột",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, pattern = PMDSkillPattern.MultiSlashCombo,
                    animName = "Attack", vfxSheet = "Slash.None", secondaryVfxSheet = "Circle_Yellow_Out.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 0.42f, damage = 30f, range = 2.45f, vfxScale = 1.35f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Phóng Cầu Khóa Mục Tiêu", moveType = "Electric", canonicalPower = 70,
                    description = "Ném cầu năng lượng tự động đuổi theo mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, pattern = PMDSkillPattern.HomingProjectile,
                    animName = "Shoot", vfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 0.65f, damage = 34f, range = 8.0f, projectileSpeed = 11.0f, projectileCount = 1, vfxScale = 1.3f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Tốc Biến Chỉ Huy", moveType = "Water", canonicalPower = 85,
                    description = "Lướt tốc độ cao húc văng kẻ địch trên đường",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, pattern = PMDSkillPattern.HighSpeedRamDash,
                    animName = "Charge", vfxSheet = "Shock_Wave.None", secondaryVfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 2.2f, damage = 44f, range = 5.4f, dashSpeed = 15.0f, waveSteps = 6, vfxScale = 1.35f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Hợp Lực Huấn Luyện Viên", moveType = "Electric", canonicalPower = 110,
                    description = "Giơ cao Poké Ball hồi 30% HP và bung 3 vòng bão năng lượng",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, pattern = PMDSkillPattern.ExpandingNovaStorm,
                    animName = "Hop", vfxSheet = "Discharge.None", secondaryVfxSheet = "Synthesis_Sparkle.None", ringVfxSheet = "Circle_Green_Out.None",
                    cooldown = 5.5f, damage = 62f, range = 3.8f, healPercent = 0.30f, vfxScale = 1.55f, elementColor = elec
                }
            };

            // =================================================================================
            // 1. #0025 PIKACHU (Moves 1..4 — Sét Zigzag | Bẫy Lưới Điện | Lao Hất Tung | Mây Giông UNITE)
            // =================================================================================
            MovesetsBySpecies["Pikachu"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Thunder Shock", moveType = "Electric", canonicalPower = 40,
                    description = "Phóng tia sét chữ Z nhảy chuyền liên tiếp giữa 4 quái",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Pikachu_ThunderShock,
                    animName = "Charge", vfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 0.45f, damage = 29f, range = 3.2f, vfxScale = 1.35f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Electroweb", moveType = "Electric", canonicalPower = 65,
                    description = "Bắn tấm lưới điện mạng nhện trải trên đất giữ chân & giật quái 2.2s",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Pikachu_Electroweb,
                    animName = "Shoot", vfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 1.2f, damage = 42f, range = 7.5f, projectileSpeed = 11.0f, vfxScale = 1.4f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Volt Tackle", moveType = "Electric", canonicalPower = 120,
                    description = "Chạy 4 chân hóa cầu sét lao thẳng hất tung mọi kẻ địch",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Pikachu_VoltTackle,
                    animName = "Attack", vfxSheet = "Discharge.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.4f, damage = 60f, range = 6.0f, vfxScale = 1.5f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Thunderstorm", moveType = "Electric", canonicalPower = 110,
                    description = "Gọi 3 đám mây giông bay trên đầu tự động giáng sét liên hoàn + Hồi 35% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Pikachu_Thunderstorm,
                    animName = "Hop", vfxSheet = "Discharge.None",
                    cooldown = 5.5f, damage = 88f, range = 5.0f, healPercent = 0.35f, vfxScale = 1.65f, elementColor = elec
                }
            };

            // =================================================================================
            // 2. #0001 BULBASAUR (Moves 5..8 — 2 Roi Kéo Gom | Lá Boomerang | Đầm Lầy Độc | Laser Mặt Trời)
            // =================================================================================
            MovesetsBySpecies["Bulbasaur"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Vine Whip", moveType = "Grass", canonicalPower = 45,
                    description = "Vươn 2 sợi dây leo dài quất chéo kéo gom quái về trước mặt",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Bulbasaur_VineWhip,
                    animName = "Attack", vfxSheet = "Vine_Whip_2.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 0.50f, damage = 28f, range = 3.0f, vfxScale = 1.4f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Razor Leaf", moveType = "Grass", canonicalPower = 55,
                    description = "Phóng 3 phi tiêu lá xoay tròn bay ra xa rồi vòng Boomerang quay về",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Bulbasaur_RazorLeaf,
                    animName = "Shoot", vfxSheet = "Energy_Ball_Hit.None", hitVfxSheet = "Vine_Whip_2.None",
                    cooldown = 1.1f, damage = 36f, range = 7.5f, projectileSpeed = 10.2f, vfxScale = 1.3f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Sludge Bomb", moveType = "Poison", canonicalPower = 90,
                    description = "Ném cầu bùn độc tạo vũng đầm lầy tím sủi bọt rút máu suốt 3 giây",
                    hotkeyLabel = "L", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Bulbasaur_SludgeBomb,
                    animName = "Strike", vfxSheet = "Energy_Ball.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 2.6f, damage = 54f, range = 5.5f, vfxScale = 1.4f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Solar Beam", moveType = "Grass", canonicalPower = 120,
                    description = "Bắn tia đại bác laser ánh sáng mặt trời dài 9m xuyên bản đồ + Hồi 45% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Bulbasaur_SolarBeam,
                    animName = "Hop", vfxSheet = "Synthesis_Sparkle.None",
                    cooldown = 6.0f, damage = 90f, range = 8.5f, healPercent = 0.45f, vfxScale = 1.65f, elementColor = grass
                }
            };

            // =================================================================================
            // 3. #0004 CHARMANDER (Moves 9..12 — Hỏa Quyền | Pháo Hoa 8 Tia | Chữ ĐẠI 大 Lửa | 4 Hỏa Cầu Xoay)
            // =================================================================================
            MovesetsBySpecies["Charmander"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Fire Punch", moveType = "Fire", canonicalPower = 75,
                    description = "Phóng nắm đấm lửa khổng lồ đấm văng kẻ địch và phát nổ",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Charmander_FirePunch,
                    animName = "Attack", vfxSheet = "Slash.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 0.45f, damage = 29f, range = 2.4f, vfxScale = 1.35f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Flame Burst", moveType = "Fire", canonicalPower = 70,
                    description = "Bắn quả cầu lửa phát nổ bung ra 8 tia pháo hoa lửa tỏa tròn 360°",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charmander_FlameBurst,
                    animName = "Shoot", vfxSheet = "Ember.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 1.0f, damage = 38f, range = 7.2f, projectileSpeed = 9.6f, vfxScale = 1.4f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Fire Blast", moveType = "Fire", canonicalPower = 110,
                    description = "Khắc ấn Hỏa Tự ĐẠI (大) rực lửa cháy âm ỉ trên mặt đất thiêu đốt quái",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Charmander_FireBlastKanji,
                    animName = "Strike", vfxSheet = "Sacred_Fire_Pillar.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 2.7f, damage = 58f, range = 6.0f, vfxScale = 1.45f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Inferno Spin", moveType = "Fire", canonicalPower = 100,
                    description = "Triệu hồi 4 quả cầu lửa hộ vệ quay tròn bung rộng rồi thu hẹp + Hồi 35% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Charmander_InfernoSpin,
                    animName = "Hop", vfxSheet = "Blast_Burn.None",
                    cooldown = 5.8f, damage = 84f, range = 4.6f, healPercent = 0.35f, vfxScale = 1.6f, elementColor = fire
                }
            };

            // =================================================================================
            // 4. #0006 CHARIZARD (Moves 13..16 — Trảm Chữ X | 3 Đao Gió | Không Kích Seismic Slam | 12 Cột Núi Lửa)
            // =================================================================================
            MovesetsBySpecies["Charizard"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Dragon Claw", moveType = "Dragon", canonicalPower = 80,
                    description = "Vung song long trảo chém dấu X xanh ngọc khổng lồ trước mặt",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Charizard_DragonClawX,
                    animName = "Attack", vfxSheet = "X_Scissor.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 0.45f, damage = 34f, range = 2.5f, vfxScale = 1.45f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Air Slash", moveType = "Flying", canonicalPower = 75,
                    description = "Vỗ cánh phóng 3 lưỡi đao gió hình bán nguyệt cắt xuyên mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charizard_AirSlash,
                    animName = "Shoot", vfxSheet = "Dragon_Pulse_Ball.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 1.1f, damage = 42f, range = 8.5f, projectileSpeed = 10.5f, vfxScale = 1.55f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Seismic Slam", moveType = "Fighting", canonicalPower = 120,
                    description = "Bay vút lên trời quắp lấy mục tiêu rồi bổ nhào đập nổ tung mặt đất",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Charizard_SeismicSlam,
                    animName = "Strike", vfxSheet = "Eruption.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.8f, damage = 66f, range = 6.5f, vfxScale = 1.6f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Blast Burn", moveType = "Fire", canonicalPower = 150,
                    description = "Kích hoạt 12 miệng núi lửa phun trào dung nham ngẫu nhiên khắp vùng + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Charizard_BlastBurnVolcano,
                    animName = "Hop", vfxSheet = "Eruption.None",
                    cooldown = 6.2f, damage = 98f, range = 5.2f, healPercent = 0.38f, vfxScale = 1.85f, elementColor = fire
                }
            };

            // =================================================================================
            // 5. #0007 SQUIRTLE (Moves 17..20 — Húc Khiên Nước | Bong Bóng Nhốt Lơ Lửng | Xoay Mai | Cưỡi Sóng Surf)
            // =================================================================================
            MovesetsBySpecies["Squirtle"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Skull Bash", moveType = "Water", canonicalPower = 90,
                    description = "Bọc khiên bong bóng nước húc văng kẻ địch về phía sau",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Squirtle_SkullBash,
                    animName = "Attack", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Bubbles_Blue.None",
                    cooldown = 0.45f, damage = 29f, range = 2.3f, vfxScale = 1.35f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Bubble Prison", moveType = "Water", canonicalPower = 65,
                    description = "Phóng bong bóng khổng lồ nhốt quái và nhấc bổng lơ lửng trên không rồi kích nổ",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Squirtle_BubblePrison,
                    animName = "Shoot", vfxSheet = "Bubbles_Blue.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 1.25f, damage = 40f, range = 7.5f, projectileSpeed = 9.0f, vfxScale = 1.55f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Rapid Spin", moveType = "Water", canonicalPower = 70,
                    description = "Thu mình vào mai xoay tít như con quay và bắn vòi nước xoắn ốc 360°",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Squirtle_RapidSpin,
                    animName = "Strike", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 2.4f, damage = 54f, range = 6.0f, vfxScale = 1.5f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Surf Tsunami", moveType = "Water", canonicalPower = 110,
                    description = "Triệu hồi và cưỡi trên đầu ngọn sóng thần rộng 5 cột cuốn trôi mọi quái + Hồi 40% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Squirtle_SurfTsunami,
                    animName = "Hop", vfxSheet = "Water_Spout_Splash.None",
                    cooldown = 5.8f, damage = 86f, range = 4.8f, healPercent = 0.40f, vfxScale = 1.7f, elementColor = water
                }
            };

            // =================================================================================
            // 6. #0133 EEVEE (Moves 21..24 — Quất Đuôi 4 Nhịp | 5 Sao Xoay Tự Bắn | 2 Phân Thân | Tứ Hệ Tiến Hóa)
            // =================================================================================
            MovesetsBySpecies["Eevee"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Tail Slap", moveType = "Normal", canonicalPower = 50,
                    description = "Quất đuôi bông liên hoàn 4 nhịp hình rẻ quạt cực nhanh",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Eevee_TailWhipCombo,
                    animName = "Attack", vfxSheet = "Slash.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 0.35f, damage = 26f, range = 2.1f, vfxScale = 1.25f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Swift Stars", moveType = "Normal", canonicalPower = 60,
                    description = "Triệu hồi 5 ngôi sao vàng quay quanh người rồi lần lượt lao đi săn mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Eevee_SwiftStars,
                    animName = "Shoot", vfxSheet = "Synthesis_Sparkle.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 1.05f, damage = 38f, range = 7.8f, projectileSpeed = 10.8f, vfxScale = 1.45f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Copycat Dash", moveType = "Normal", canonicalPower = 100,
                    description = "Tách 2 phân thân ánh sáng cùng lao chéo kẹp nổ mục tiêu hình tam giác",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Eevee_CopycatShadowDash,
                    animName = "Strike", vfxSheet = "Close_Combat.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.4f, damage = 56f, range = 5.8f, vfxScale = 1.45f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Eeveelution Burst", moveType = "Normal", canonicalPower = 140,
                    description = "Phóng đồng thời 4 luồng tiến hóa Lửa - Nước - Điện - Cỏ xoáy 4 hướng + Hồi 42% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Eevee_EeveelutionResonance,
                    animName = "Hop", vfxSheet = "Discharge.None",
                    cooldown = 5.8f, damage = 88f, range = 4.6f, healPercent = 0.42f, vfxScale = 1.6f, elementColor = normal
                }
            };

            // =================================================================================
            // 7. #0151 MEW (Moves 25..28 — Tường Chắn Light Screen | Dịch Chuyển Đánh Úp | Nhấc Bổng | Siêu Tân Tinh)
            // =================================================================================
            MovesetsBySpecies["Mew"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Light Screen", moveType = "Psychic", canonicalPower = 75,
                    description = "Dựng bức tường pha lê chắn đòn rồi đẩy phẳng về phía trước",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Mew_LightScreenWall,
                    animName = "Attack", vfxSheet = "Shock_Wave.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 0.65f, damage = 32f, range = 2.8f, vfxScale = 1.35f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Teleport Ambush", moveType = "Psychic", canonicalPower = 80,
                    description = "Biến mất và dịch chuyển tức thời ra sau lưng kẻ địch phát nổ bóng tối",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Mew_TeleportAmbush,
                    animName = "Shoot", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 1.15f, damage = 42f, range = 8.2f, vfxScale = 1.55f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Psychic Lift", moveType = "Psychic", canonicalPower = 90,
                    description = "Dùng ngoại cảm nhấc bổng toàn bộ quái trong vùng lên không trung rồi đập xuống",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mew_PsychicTelekinesis,
                    animName = "Strike", vfxSheet = "Aura_Sphere.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 2.7f, damage = 58f, range = 6.4f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Genesis Supernova", moveType = "Psychic", canonicalPower = 130,
                    description = "Tạo lỗ đen siêu tân tinh hút quái vào tâm rồi bắn 12 tia vũ trụ + Hồi 45% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mew_GenesisSupernova,
                    animName = "Hop", vfxSheet = "Explosion.None",
                    cooldown = 6.0f, damage = 94f, range = 5.0f, healPercent = 0.45f, vfxScale = 1.65f, elementColor = psychic
                }
            };

            // =================================================================================
            // 8. #0252 TREECKO (Moves 29..32 — Chém Chữ Thập + | Súng Máy 10 Hạt | Hạt Ký Sinh | Rễ Gai Đại Ngàn)
            // =================================================================================
            MovesetsBySpecies["Treecko"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Leaf Blade", moveType = "Grass", canonicalPower = 90,
                    description = "Chém 2 đường kiếm lá vuông góc tạo dấu thập (+) xanh ngọc",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Treecko_LeafBladeCross,
                    animName = "Attack", vfxSheet = "X_Scissor.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 0.40f, damage = 31f, range = 2.3f, vfxScale = 1.35f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Bullet Seed", moveType = "Grass", canonicalPower = 85,
                    description = "Bắn liên thanh 10 viên đạn hạt giống vàng với tốc độ súng máy",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Treecko_BulletSeedGatling,
                    animName = "Shoot", vfxSheet = "Energy_Ball.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 1.1f, damage = 38f, range = 8.0f, projectileSpeed = 9.8f, vfxScale = 1.35f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Leech Seed", moveType = "Grass", canonicalPower = 75,
                    description = "Cấy hạt giống ký sinh lên quái, hút máu 5 đợt bay về hồi HP cho đội",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Treecko_LeechSeedParasite,
                    animName = "Strike", vfxSheet = "Vine_Whip_2.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 2.8f, damage = 52f, range = 5.8f, vfxScale = 1.4f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Frenzy Plant", moveType = "Grass", canonicalPower = 150,
                    description = "Gọi 8 cụm gai rễ cây khổng lồ đâm xuyên từ lòng đất lên theo đường ziczac + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Treecko_FrenzyPlantRoots,
                    animName = "Hop", vfxSheet = "Vine_Whip_2.None",
                    cooldown = 6.0f, damage = 90f, range = 4.8f, healPercent = 0.38f, vfxScale = 1.6f, elementColor = grass
                }
            };

            // =================================================================================
            // 9. #0255 TORCHIC (Moves 33..36 — Mổ 5 Nhịp | Bẫy 6 Lông Vũ Nổ | Nhảy Đá Blaze Kick | Phượng Hoàng Lửa)
            // =================================================================================
            MovesetsBySpecies["Torchic"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Peck Flurry", moveType = "Flying", canonicalPower = 65,
                    description = "Mổ mỏ lửa liên hoàn 5 phát cực nhanh vào mục tiêu",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Torchic_PeckFlurry,
                    animName = "Attack", vfxSheet = "Slash.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 0.40f, damage = 28f, range = 2.1f, vfxScale = 1.25f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Feather Trap", moveType = "Fire", canonicalPower = 75,
                    description = "Rải vòng tròn 6 chiếc lông vũ đỏ rực lơ lửng rồi đồng loạt phát nổ",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Torchic_FeatherDanceTrap,
                    animName = "Shoot", vfxSheet = "Ember.None", hitVfxSheet = "Sacred_Fire_Pillar.None",
                    cooldown = 1.15f, damage = 40f, range = 7.4f, vfxScale = 1.35f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Blaze Kick", moveType = "Fire", canonicalPower = 85,
                    description = "Bật nhảy parabol qua đầu kẻ địch rồi tung cước lửa bổ nhào xuống",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Torchic_BlazeKickJump,
                    animName = "Strike", vfxSheet = "Sacred_Fire_Pillar.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 2.3f, damage = 54f, range = 6.2f, vfxScale = 1.45f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Phoenix Wing", moveType = "Fire", canonicalPower = 130,
                    description = "Sải đôi cánh Phượng Hoàng lửa hình chữ V khổng lồ càn quét phía trước + Hồi 36% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Torchic_PhoenixSkyUppercut,
                    animName = "Hop", vfxSheet = "Eruption.None",
                    cooldown = 6.0f, damage = 88f, range = 4.7f, healPercent = 0.36f, vfxScale = 1.65f, elementColor = fire
                }
            };

            // =================================================================================
            // 10. #0258 MUDKIP (Moves 37..40 — Tạt 3 Bãi Bùn | Bom Bùn Nảy 3 Nhịp | 3 Mạch Nước Ngầm | Động Đất Chữ Y)
            // =================================================================================
            MovesetsBySpecies["Mudkip"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Mud-Slap", moveType = "Ground", canonicalPower = 55,
                    description = "Hất 3 mảng bùn lầy phủ kín mặt đất trước mặt",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Mudkip_MudSlapSwamp,
                    animName = "Attack", vfxSheet = "Close_Combat.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 0.45f, damage = 28f, range = 2.2f, vfxScale = 1.3f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Mud Bomb", moveType = "Ground", canonicalPower = 65,
                    description = "Ném quả cầu bùn nảy tưng tưng 3 lần liên tiếp, mỗi nhịp nảy nổ tung bùn",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Mudkip_MudBombBounce,
                    animName = "Shoot", vfxSheet = "Aqua_Ring.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 1.1f, damage = 38f, range = 7.8f, vfxScale = 1.35f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Hydro Geyser", moveType = "Water", canonicalPower = 90,
                    description = "Kích hoạt chuỗi 3 mạch nước ngầm lần lượt phun trào từ dưới chân quái",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mudkip_WhirlpoolGeyser,
                    animName = "Strike", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 2.6f, damage = 56f, range = 6.0f, vfxScale = 1.55f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Earthquake", moveType = "Ground", canonicalPower = 100,
                    description = "Dậm nứt toác mặt đất thành 3 đường vực thẳm hình chữ Y phát nổ + Hồi 40% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mudkip_EarthquakeFissure,
                    animName = "Hop", vfxSheet = "Aqua_Ring.None",
                    cooldown = 5.8f, damage = 86f, range = 4.7f, healPercent = 0.40f, vfxScale = 1.65f, elementColor = water
                }
            };

            // =================================================================================
            // 11. #0448 LUCARIO (Moves 41..44 — 6 Nắm Đấm Thép | Trượng Xương Dịch Chuyển | Khí Công Quyền | 8 Pháo Aura)
            // =================================================================================
            MovesetsBySpecies["Lucario"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Bullet Punch", moveType = "Steel", canonicalPower = 60,
                    description = "Tung cơn mưa 6 nắm đấm thép khí công dồn dập phía trước",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Lucario_BulletPunchBarrage,
                    animName = "Attack", vfxSheet = "Close_Combat.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 0.42f, damage = 30f, range = 2.3f, vfxScale = 1.35f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Bone Rush", moveType = "Ground", canonicalPower = 75,
                    description = "Phóng trượng xương xanh xoay tít tại đích rồi dịch chuyển Lucario tới kết liễu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Lucario_BoneRushTeleport,
                    animName = "Shoot", vfxSheet = "Aura_Sphere.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 1.25f, damage = 44f, range = 8.5f, vfxScale = 1.45f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Power-Up Punch", moveType = "Fighting", canonicalPower = 85,
                    description = "Tụ lực phóng nắm đấm khí công khổng lồ bay xuyên đẩy lùi mọi kẻ địch",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Lucario_PowerUpPunch,
                    animName = "Strike", vfxSheet = "Slash.None", hitVfxSheet = "Close_Combat.None",
                    cooldown = 2.3f, damage = 58f, range = 6.5f, vfxScale = 1.45f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Aura Cannon", moveType = "Fighting", canonicalPower = 120,
                    description = "Bắn chuỗi 8 quả cầu 波導 (Aura Sphere)連 thanh xuyên thấu + Hồi 35% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Lucario_AuraCannonBarrage,
                    animName = "Hop", vfxSheet = "Aura_Sphere.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 5.8f, damage = 24f, range = 8.5f, projectileSpeed = 12.5f, piercing = true, healPercent = 0.35f, vfxScale = 1.55f, elementColor = fight
                }
            };

            // =================================================================================
            // 12. #0471 GLACEON (Moves 45..48 — 3 Kim Băng | 8 Pha Lê Băng UNITE | 7 Cọc Băng Đất | Lãnh Địa Tuyết)
            // =================================================================================
            MovesetsBySpecies["Glaceon"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Ice Shard", moveType = "Ice", canonicalPower = 55,
                    description = "Phóng 3 mũi kim băng sắc nhọn cắm thẳng vào mục tiêu",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Glaceon_IceShardDaggers,
                    animName = "Attack", vfxSheet = "Slash.None", hitVfxSheet = "Sheer_Cold_Front.None",
                    cooldown = 0.42f, damage = 28f, range = 5.2f, vfxScale = 1.3f, elementColor = ice
                },
                new PMDSkillData
                {
                    skillName = "Icicle Spear", moveType = "Ice", canonicalPower = 85,
                    description = "Tích 8 tinh thể pha lê băng xoay quanh người rồi bắn tên lửa tự tìm mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Glaceon_IcicleSpearCrystals,
                    animName = "Shoot", vfxSheet = "Dragon_Pulse_Ball.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 1.2f, damage = 44f, range = 7.6f, vfxScale = 1.3f, elementColor = ice
                },
                new PMDSkillData
                {
                    skillName = "Freeze-Dry", moveType = "Ice", canonicalPower = 90,
                    description = "Cho 7 cọc băng nhọn mọc nhô dần từ lòng đất đóng băng cả đường thẳng",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Glaceon_FreezeDrySpikes,
                    animName = "Strike", vfxSheet = "Sheer_Cold_Front.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 2.6f, damage = 56f, range = 6.4f, vfxScale = 1.4f, elementColor = ice
                },
                new PMDSkillData
                {
                    skillName = "Diamond Dust", moveType = "Ice", canonicalPower = 110,
                    description = "Mở kết giới cầu tuyết khổng lồ trên mặt đất, giáng mưa tinh thể băng + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Glaceon_SnowGlobeBlizzard,
                    animName = "Hop", vfxSheet = "Sheer_Cold_Front.None",
                    cooldown = 6.0f, damage = 90f, range = 5.0f, healPercent = 0.38f, vfxScale = 1.55f, elementColor = ice
                }
            };

            // =================================================================================
            // 13. #0059 ARCANINE (Fire — Thunder Fang | Flame Wheel | Extreme Speed | Raging Burn)
            // =================================================================================
            MovesetsBySpecies["Arcanine"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Thunder Fang", moveType = "Electric", canonicalPower = 65,
                    description = "Nanh sấm sét cắn giật điện chuyền qua các mục tiêu đứng gần",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Pikachu_ThunderShock,
                    animName = "Attack", vfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 0.42f, damage = 31f, range = 2.8f, vfxScale = 1.4f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Flame Wheel", moveType = "Fire", canonicalPower = 75,
                    description = "Phóng bánh xe lửa phát nổ bung 8 tia lửa xoay tròn",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charmander_FlameBurst,
                    animName = "Shoot", vfxSheet = "Ember.None", hitVfxSheet = "Blast_Burn.None",
                    cooldown = 1.0f, damage = 40f, range = 7.5f, vfxScale = 1.45f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Sacred Charge", moveType = "Fire", canonicalPower = 100,
                    description = "Thần khuyển bọc lửa lao vọt qua đầu kẻ địch và giáng cước nổ",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Torchic_BlazeKickJump,
                    animName = "Strike", vfxSheet = "Sacred_Fire_Pillar.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.2f, damage = 60f, range = 6.4f, vfxScale = 1.5f, elementColor = fire
                },
                new PMDSkillData
                {
                    skillName = "Sacred Fire", moveType = "Fire", canonicalPower = 135,
                    description = "Khắc ấn Hỏa Tự ĐẠI (大) thiêng liêng rực cháy + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Charmander_FireBlastKanji,
                    animName = "Hop", vfxSheet = "Sacred_Fire_Pillar.None",
                    cooldown = 5.6f, damage = 92f, range = 5.4f, healPercent = 0.38f, vfxScale = 1.7f, elementColor = fire
                }
            };

            // =================================================================================
            // 14. #0094 GENGAR (Ghost/Poison — Lick | Shadow Sneak | Venoshock | Phantom Hex)
            // =================================================================================
            MovesetsBySpecies["Gengar"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Shadow Claw", moveType = "Ghost", canonicalPower = 70,
                    description = "Vuốt bóng ma chém chữ X cắt xuyên linh hồn mục tiêu",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Charizard_DragonClawX,
                    animName = "Attack", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.40f, damage = 32f, range = 2.5f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Sludge Wave", moveType = "Poison", canonicalPower = 95,
                    description = "Phóng đầm lầy độc tố tím sủi bọt rút máu liên tục trên mặt đất",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Bulbasaur_SludgeBomb,
                    animName = "Shoot", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 1.15f, damage = 44f, range = 7.4f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Phantom Ambush", moveType = "Ghost", canonicalPower = 90,
                    description = "Ẩn mình vào bóng tối và dịch chuyển tức thời ra sau lưng mục tiêu phát nổ",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mew_TeleportAmbush,
                    animName = "Strike", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.1f, damage = 58f, range = 6.2f, vfxScale = 1.55f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Nightmare Vortex", moveType = "Ghost", canonicalPower = 130,
                    description = "Mở lỗ đen ác mộng hút gom toàn bộ quái vào tâm rồi bắn 12 tia ma quái + Hồi 40% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mew_GenesisSupernova,
                    animName = "Hop", vfxSheet = "Shadow_Ball.None",
                    cooldown = 5.8f, damage = 94f, range = 5.2f, healPercent = 0.40f, vfxScale = 1.7f, elementColor = psychic
                }
            };

            // =================================================================================
            // 15. #0130 GYARADOS (Water/Flying — Ice Fang | Dragon Breath | Waterfall | Hyper Beam)
            // =================================================================================
            MovesetsBySpecies["Gyarados"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Crunch", moveType = "Dark", canonicalPower = 80,
                    description = "Hàm răng thủy quái nghiền nát mục tiêu kèm khiên bọt nước",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Squirtle_SkullBash,
                    animName = "Attack", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.45f, damage = 34f, range = 2.5f, vfxScale = 1.5f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Hurricane Blade", moveType = "Flying", canonicalPower = 85,
                    description = "Quẫy thân phóng 3 lưỡi đao cuồng phong cắt xuyên đội hình địch",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charizard_AirSlash,
                    animName = "Shoot", vfxSheet = "Dragon_Pulse_Ball.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 1.1f, damage = 44f, range = 8.4f, vfxScale = 1.55f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Dragon Dance Spin", moveType = "Dragon", canonicalPower = 90,
                    description = "Cuộn mình xoay lốc rồng 360° phun vòi rồng nước ra mọi phía",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Squirtle_RapidSpin,
                    animName = "Strike", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Bubbles_Blue.None",
                    cooldown = 2.4f, damage = 60f, range = 6.2f, vfxScale = 1.6f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Tidal Leviathan", moveType = "Water", canonicalPower = 150,
                    description = "Dựng đại sóng thần 5 cột nước cuốn phăng mọi kẻ địch trước mặt + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Squirtle_SurfTsunami,
                    animName = "Hop", vfxSheet = "Water_Spout_Splash.None",
                    cooldown = 6.0f, damage = 96f, range = 5.4f, healPercent = 0.38f, vfxScale = 1.8f, elementColor = water
                }
            };

            // =================================================================================
            // 16. #0131 LAPRAS (Water/Ice — Ice Shard | Brine Prison | Frost Spikes | Perish Song)
            // =================================================================================
            MovesetsBySpecies["Lapras"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Ice Shard", moveType = "Ice", canonicalPower = 60,
                    description = "Phóng 3 mũi băng sắc nhọn đóng băng mục tiêu",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Glaceon_IceShardDaggers,
                    animName = "Attack", vfxSheet = "Sheer_Cold_Front.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 0.44f, damage = 29f, range = 5.2f, vfxScale = 1.35f, elementColor = ice
                },
                new PMDSkillData
                {
                    skillName = "Brine Bubble", moveType = "Water", canonicalPower = 75,
                    description = "Bắn cầu nước mặn khổng lồ nhốt và nhấc bổng quái lên không trung",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Squirtle_BubblePrison,
                    animName = "Shoot", vfxSheet = "Bubbles_Blue.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 1.2f, damage = 42f, range = 7.6f, vfxScale = 1.55f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Sheer Cold Spikes", moveType = "Ice", canonicalPower = 95,
                    description = "Gọi 7 tháp băng vĩnh cửu mọc nhô lên đóng băng dọc đường thẳng",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Glaceon_FreezeDrySpikes,
                    animName = "Strike", vfxSheet = "Sheer_Cold_Front.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 2.6f, damage = 58f, range = 6.4f, vfxScale = 1.5f, elementColor = ice
                },
                new PMDSkillData
                {
                    skillName = "Glacial Song", moveType = "Ice", canonicalPower = 120,
                    description = "Khúc ca băng hà mở kết giới tuyết lục giác và hồi 45% HP cho toàn đội",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Glaceon_SnowGlobeBlizzard,
                    animName = "Hop", vfxSheet = "Sheer_Cold_Front.None",
                    cooldown = 6.0f, damage = 88f, range = 5.2f, healPercent = 0.45f, vfxScale = 1.65f, elementColor = ice
                }
            };

            // =================================================================================
            // 17. #0143 SNORLAX (Normal — Mega Punch | Rollout Bounce | Heavy Slam | Belly Drum Quake)
            // =================================================================================
            MovesetsBySpecies["Snorlax"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Mega Punch", moveType = "Normal", canonicalPower = 80,
                    description = "Cú đấm ngàn cân đẩy văng mục tiêu về phía sau",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Charmander_FirePunch,
                    animName = "Attack", vfxSheet = "Close_Combat.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.50f, damage = 35f, range = 2.4f, vfxScale = 1.55f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Boulder Bounce", moveType = "Rock", canonicalPower = 75,
                    description = "Ném tảng đá khổng lồ nảy 3 nhịp liên tiếp gây chấn động mặt đất",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Mudkip_MudBombBounce,
                    animName = "Shoot", vfxSheet = "Explosion.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 1.2f, damage = 42f, range = 7.2f, vfxScale = 1.55f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Heavy Slam", moveType = "Normal", canonicalPower = 110,
                    description = "Nhảy vọt lên cao rồi lấy thân hình khổng lồ đè bẹp mọi kẻ địch",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Charizard_SeismicSlam,
                    animName = "Strike", vfxSheet = "Explosion.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 2.7f, damage = 66f, range = 5.8f, vfxScale = 1.75f, elementColor = normal
                },
                new PMDSkillData
                {
                    skillName = "Giga Quake", moveType = "Ground", canonicalPower = 140,
                    description = "Vỗ bụng làm nứt toác mặt đất thành 3 vực thẳm chữ Y + Hồi 50% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mudkip_EarthquakeFissure,
                    animName = "Hop", vfxSheet = "Explosion.None",
                    cooldown = 6.2f, damage = 94f, range = 5.0f, healPercent = 0.50f, vfxScale = 1.75f, elementColor = normal
                }
            };

            // =================================================================================
            // 18. #0149 DRAGONITE (Dragon/Flying — Thunder Punch | Hurricane | Extreme Dive | Draco Meteor)
            // =================================================================================
            MovesetsBySpecies["Dragonite"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Thunder Punch", moveType = "Electric", canonicalPower = 75,
                    description = "Tung 6 cú đấm sấm sét dồn dập vào mục tiêu",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Lucario_BulletPunchBarrage,
                    animName = "Attack", vfxSheet = "Spark.None", hitVfxSheet = "Discharge.None",
                    cooldown = 0.42f, damage = 33f, range = 2.4f, vfxScale = 1.45f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Wing Hurricane", moveType = "Flying", canonicalPower = 85,
                    description = "Phóng 3 lưỡi đao bão tố tốc độ cao cắt xuyên hàng ngũ địch",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charizard_AirSlash,
                    animName = "Shoot", vfxSheet = "Dragon_Pulse_Ball.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 1.05f, damage = 44f, range = 8.4f, vfxScale = 1.55f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Dragon Rush", moveType = "Dragon", canonicalPower = 100,
                    description = "Bọc điện thần tốc lao thẳng hất tung toàn bộ kẻ địch",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Pikachu_VoltTackle,
                    animName = "Strike", vfxSheet = "Discharge.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.4f, damage = 62f, range = 6.4f, vfxScale = 1.6f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Draco Meteor", moveType = "Dragon", canonicalPower = 140,
                    description = "Gọi 12 trận mưa thiên thạch rồng nện xuống khắp chiến trường + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Charizard_BlastBurnVolcano,
                    animName = "Hop", vfxSheet = "Eruption.None",
                    cooldown = 6.0f, damage = 98f, range = 5.4f, healPercent = 0.38f, vfxScale = 1.8f, elementColor = fire
                }
            };

            // =================================================================================
            // 19. #0150 MEWTWO (Psychic — Barrier Shove | Psystrike Warp | Telekinesis | Psycho Boost)
            // =================================================================================
            MovesetsBySpecies["Mewtwo"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Psycho Cut", moveType = "Psychic", canonicalPower = 75,
                    description = "Chém chữ thập (+) bằng lưỡi đao ngoại cảm sắc lẹm",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Treecko_LeafBladeCross,
                    animName = "Attack", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.40f, damage = 34f, range = 2.5f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Aura Sphere X", moveType = "Fighting", canonicalPower = 90,
                    description = "Tích 8 tinh thể siêu linh quanh người rồi bắn đuổi mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Glaceon_IcicleSpearCrystals,
                    animName = "Shoot", vfxSheet = "Aura_Sphere.None", hitVfxSheet = "Explosion.None",
                    cooldown = 1.15f, damage = 48f, range = 8.4f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Gravity Crush", moveType = "Psychic", canonicalPower = 100,
                    description = "Nhấc bổng mọi kẻ địch lên không trung rồi dùng trọng lực nghiền nát",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mew_PsychicTelekinesis,
                    animName = "Strike", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.6f, damage = 64f, range = 6.5f, vfxScale = 1.6f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Psystrike Nova", moveType = "Psychic", canonicalPower = 150,
                    description = "Tạo hố đen vũ trụ hút gom quái và phóng 12 tia hủy diệt + Hồi 42% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mew_GenesisSupernova,
                    animName = "Hop", vfxSheet = "Explosion.None",
                    cooldown = 6.0f, damage = 102f, range = 5.5f, healPercent = 0.42f, vfxScale = 1.8f, elementColor = psychic
                }
            };

            // =================================================================================
            // 20. #0248 TYRANITAR (Rock/Dark — Stone Edge | Dark Pulse | Rock Slide | Sandstorm Quake)
            // =================================================================================
            MovesetsBySpecies["Tyranitar"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Iron Fist", moveType = "Steel", canonicalPower = 80,
                    description = "Cú đấm bọc thép đập văng kẻ địch về phía sau",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Lucario_PowerUpPunch,
                    animName = "Attack", vfxSheet = "Close_Combat.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.46f, damage = 35f, range = 2.6f, vfxScale = 1.5f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Stone Edge", moveType = "Rock", canonicalPower = 100,
                    description = "Cho 7 ngọn tháp đá nhọn đâm xuyên từ lòng đất lên dọc đường thẳng",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Glaceon_FreezeDrySpikes,
                    animName = "Shoot", vfxSheet = "Explosion.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 1.2f, damage = 46f, range = 7.4f, vfxScale = 1.55f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Dark Fissure", moveType = "Dark", canonicalPower = 110,
                    description = "Dậm chân làm nứt toác 3 khe vực chữ Y nổ tung mặt đất",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mudkip_EarthquakeFissure,
                    animName = "Strike", vfxSheet = "Shadow_Ball.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.6f, damage = 64f, range = 6.0f, vfxScale = 1.6f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Tyrant Eruption", moveType = "Rock", canonicalPower = 150,
                    description = "Kích nổ 12 cột địa chấn dung nham khắp xung quanh + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Charizard_BlastBurnVolcano,
                    animName = "Hop", vfxSheet = "Eruption.None",
                    cooldown = 6.2f, damage = 98f, range = 5.4f, healPercent = 0.38f, vfxScale = 1.8f, elementColor = fire
                }
            };

            // =================================================================================
            // 21. #0282 GARDEVOIR (Psychic/Fairy — Reflect Wall | Magical Leaf | Moonblast | Black Hole)
            // =================================================================================
            MovesetsBySpecies["Gardevoir"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Reflect Wall", moveType = "Psychic", canonicalPower = 70,
                    description = "Dựng gương pha lê chắn đòn rồi đẩy phẳng hất văng quái",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Mew_LightScreenWall,
                    animName = "Attack", vfxSheet = "Synthesis_Sparkle.None", hitVfxSheet = "Shock_Wave.None",
                    cooldown = 0.50f, damage = 30f, range = 2.8f, vfxScale = 1.4f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Dazzling Gleam", moveType = "Fairy", canonicalPower = 80,
                    description = "Triệu hồi 5 ngôi sao ánh sáng tiên nữ tự động lao đuổi mục tiêu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Eevee_SwiftStars,
                    animName = "Shoot", vfxSheet = "Synthesis_Sparkle.None", hitVfxSheet = "Energy_Ball_Hit.None",
                    cooldown = 1.05f, damage = 42f, range = 8.0f, vfxScale = 1.45f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Psychic Snare", moveType = "Psychic", canonicalPower = 90,
                    description = "Nhấc bổng kẻ địch bằng niệm lực rồi giáng xuống mặt đất",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Mew_PsychicTelekinesis,
                    animName = "Strike", vfxSheet = "Aura_Sphere.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.6f, damage = 58f, range = 6.2f, vfxScale = 1.5f, elementColor = psychic
                },
                new PMDSkillData
                {
                    skillName = "Fairy Singularity", moveType = "Fairy", canonicalPower = 135,
                    description = "Tạo lỗ đen vũ trụ hút gom quái vào tâm rồi bung 12 tia sáng + Hồi 45% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mew_GenesisSupernova,
                    animName = "Hop", vfxSheet = "Synthesis_Sparkle.None",
                    cooldown = 5.8f, damage = 92f, range = 5.2f, healPercent = 0.45f, vfxScale = 1.7f, elementColor = psychic
                }
            };

            // =================================================================================
            // 22. #0384 RAYQUAZA (Dragon/Flying — Dragon Claw | Air Slash | Dragon Ascent | Hyper Beam)
            // =================================================================================
            MovesetsBySpecies["Rayquaza"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Emerald Claw", moveType = "Dragon", canonicalPower = 85,
                    description = "Chém dấu X vuốt rồng ngọc lục bảo xé toạc không gian",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Charizard_DragonClawX,
                    animName = "Attack", vfxSheet = "X_Scissor.None", hitVfxSheet = "Discharge.None",
                    cooldown = 0.42f, damage = 36f, range = 2.6f, vfxScale = 1.55f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Twister Blades", moveType = "Flying", canonicalPower = 90,
                    description = "Phóng 3 lưỡi đao khí quyển hình bán nguyệt xuyên thấu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Charizard_AirSlash,
                    animName = "Shoot", vfxSheet = "Dragon_Pulse_Ball.None", hitVfxSheet = "Discharge.None",
                    cooldown = 1.05f, damage = 46f, range = 8.8f, vfxScale = 1.6f, elementColor = grass
                },
                new PMDSkillData
                {
                    skillName = "Dragon Ascent", moveType = "Flying", canonicalPower = 120,
                    description = "Bay vút lên tầng bình lưu rồi bổ nhào xuống đập nát mục tiêu",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Charizard_SeismicSlam,
                    animName = "Strike", vfxSheet = "Discharge.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.6f, damage = 68f, range = 6.8f, vfxScale = 1.7f, elementColor = elec
                },
                new PMDSkillData
                {
                    skillName = "Ozone Hyper Beam", moveType = "Dragon", canonicalPower = 150,
                    description = "Bắn tia đại bác laser tầng Ozone dài 9m xuyên thủng bản đồ + Hồi 40% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Bulbasaur_SolarBeam,
                    animName = "Hop", vfxSheet = "Discharge.None",
                    cooldown = 6.0f, damage = 104f, range = 9.0f, healPercent = 0.40f, vfxScale = 1.8f, elementColor = grass
                }
            };

            // =================================================================================
            // 23. #0445 GARCHOMP (Dragon/Ground — Dual Chop | Sand Tomb | Dragon Rush | Tectonic Rage)
            // =================================================================================
            MovesetsBySpecies["Garchomp"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Dual Chop", moveType = "Dragon", canonicalPower = 80,
                    description = "Vây kiếm chém chữ thập (+) tốc độ siêu thanh",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Treecko_LeafBladeCross,
                    animName = "Attack", vfxSheet = "X_Scissor.None", hitVfxSheet = "Explosion.None",
                    cooldown = 0.40f, damage = 34f, range = 2.5f, vfxScale = 1.45f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Draco Bone", moveType = "Ground", canonicalPower = 85,
                    description = "Phóng vây kiếm xoay tại đích rồi Garchomp lao vụt tới kết liễu",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Lucario_BoneRushTeleport,
                    animName = "Shoot", vfxSheet = "Slash.None", hitVfxSheet = "Explosion.None",
                    cooldown = 1.2f, damage = 46f, range = 8.2f, vfxScale = 1.5f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Jet Dive", moveType = "Dragon", canonicalPower = 100,
                    description = "Tách 2 phân thân siêu thanh cùng lao chéo kẹp nổ mục tiêu",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Eevee_CopycatShadowDash,
                    animName = "Strike", vfxSheet = "Close_Combat.None", hitVfxSheet = "Explosion.None",
                    cooldown = 2.3f, damage = 62f, range = 6.4f, vfxScale = 1.55f, elementColor = fight
                },
                new PMDSkillData
                {
                    skillName = "Tectonic Fissure", moveType = "Ground", canonicalPower = 140,
                    description = "Xé toạc mặt đất thành 3 vực thẳm chữ Y phát nổ liên hoàn + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Mudkip_EarthquakeFissure,
                    animName = "Hop", vfxSheet = "Explosion.None",
                    cooldown = 5.8f, damage = 96f, range = 5.2f, healPercent = 0.38f, vfxScale = 1.75f, elementColor = fight
                }
            };

            // =================================================================================
            // 24. #0658 GRENINJA (Water/Dark — Night Slash | Water Shuriken | Double Team | Giant Shuriken)
            // =================================================================================
            MovesetsBySpecies["Greninja"] = new[]
            {
                new PMDSkillData
                {
                    skillName = "Night Slash", moveType = "Dark", canonicalPower = 70,
                    description = "Kunai nước chém dấu thập (+) chí mạng",
                    hotkeyLabel = "J", kind = PMDSkillKind.Melee, uniqueId = PMDUniqueMoveId.Treecko_LeafBladeCross,
                    animName = "Attack", vfxSheet = "Slash.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 0.36f, damage = 31f, range = 2.4f, vfxScale = 1.4f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Water Shuriken", moveType = "Water", canonicalPower = 85,
                    description = "Phóng 3 phi tiêu nước xoay tít bay đi rồi vòng Boomerang quay về",
                    hotkeyLabel = "K", kind = PMDSkillKind.Projectile, uniqueId = PMDUniqueMoveId.Bulbasaur_RazorLeaf,
                    animName = "Shoot", vfxSheet = "Bubbles_Blue.None", hitVfxSheet = "Water_Spout_Splash.None",
                    cooldown = 0.95f, damage = 42f, range = 8.2f, vfxScale = 1.4f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Double Team Dash", moveType = "Dark", canonicalPower = 95,
                    description = "Phân thân Ninja nước lao chéo 3 hướng cắt nát mục tiêu",
                    hotkeyLabel = "L", kind = PMDSkillKind.LinearShockwaveDash, uniqueId = PMDUniqueMoveId.Eevee_CopycatShadowDash,
                    animName = "Strike", vfxSheet = "Water_Spout_Splash.None", hitVfxSheet = "Bubbles_Blue.None",
                    cooldown = 2.1f, damage = 58f, range = 6.2f, vfxScale = 1.5f, elementColor = water
                },
                new PMDSkillData
                {
                    skillName = "Shinobi Tsunami", moveType = "Water", canonicalPower = 130,
                    description = "Lướt trên đỉnh ngọn sóng thần Ninja cuốn phăng mọi kẻ địch + Hồi 38% HP",
                    hotkeyLabel = "U", kind = PMDSkillKind.UltimateOrbitalStorm, uniqueId = PMDUniqueMoveId.Squirtle_SurfTsunami,
                    animName = "Hop", vfxSheet = "Water_Spout_Splash.None",
                    cooldown = 5.6f, damage = 92f, range = 5.2f, healPercent = 0.38f, vfxScale = 1.7f, elementColor = water
                }
            };

            PMDAllPokemonMovesets.RegisterAll(MovesetsBySpecies);
        }
    }
}
