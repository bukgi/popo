using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Complete, zero-redundancy 12-Archetype Moveset Registry for all Pokémon (#13..#84),
    /// ensuring every single Pokémon has a distinct tactical combo across all 12 Skill Groups
    /// (Single-target Burst, Mobility/Blink, Stasis/CC, Buff/Debuff, Parry/Shield, Turrets/Mines,
    /// Black Hole/Clones, Life Drain/Blood Sacrifice, Time Bomb/Time Loop, Shapeshift, Execute, and Domain/Cataclysm).
    /// </summary>
    public static class PMDAllPokemonMovesets
    {
        private static readonly Color Elec = new Color(1f, 0.88f, 0.22f);
        private static readonly Color Grass = new Color(0.35f, 0.92f, 0.42f);
        private static readonly Color Fire = new Color(1f, 0.45f, 0.2f);
        private static readonly Color Water = new Color(0.3f, 0.78f, 1f);
        private static readonly Color Fight = new Color(0.95f, 0.4f, 0.35f);
        private static readonly Color Psychic = new Color(0.95f, 0.48f, 0.92f);
        private static readonly Color Ice = new Color(0.55f, 0.92f, 1f);
        private static readonly Color Normal = new Color(0.92f, 0.88f, 0.78f);

        private static PMDSkillData M(
            string name, string type, int bp, string desc, string hotkey, PMDSkillKind kind,
            PMDUniqueMoveId uid, string anim, string vfx, string hitVfx,
            float cd, float dmg, float range, Color col, float heal = 0f, float scale = 1.4f)
        {
            return new PMDSkillData
            {
                skillName = name, moveType = type, canonicalPower = bp, description = desc,
                hotkeyLabel = hotkey, kind = kind, uniqueId = uid,
                animName = anim, vfxSheet = vfx, hitVfxSheet = hitVfx,
                cooldown = cd, damage = dmg, range = range, elementColor = col, healPercent = heal, vfxScale = scale
            };
        }

        private static PMDSkillData A(
            string name, string type, int bp, string desc, string hotkey, PMDSkillKind kind,
            PMDArchetypeMechanic arch, PMDVisualAssetKind vis, string anim,
            float cd, float dmg, float range, Color col, float heal = 0f, float scale = 1.4f)
        {
            return new PMDSkillData
            {
                skillName = name, moveType = type, canonicalPower = bp, description = desc,
                hotkeyLabel = hotkey, kind = kind, archetype = arch, visualKind = vis,
                animName = anim, cooldown = cd, damage = dmg, range = range,
                elementColor = col, healPercent = heal, vfxScale = scale
            };
        }

        public static void RegisterAll(Dictionary<string, PMDSkillData[]> db)
        {
            // =================================================================================
            // UPGRADED #13..#24 (Zero Duplicate Kits)
            // =================================================================================
            db["Arcanine"] = new[]
            {
                A("Thunder Fang", "Electric", 65, "Đâm nanh điện xuyên giáp đẩy lùi + Tê liệt", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.Bolt, "Attack", 0.42f, 32f, 2.5f, Elec),
                A("Flame Wheel", "Fire", 75, "Lướt bất tử trong bánh xe lửa xuyên qua quái", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.FireOrb, "Shoot", 1.1f, 44f, 5.5f, Fire),
                A("Howl Frenzy", "Fire", 80, "Gầm tăng 38% tốc chạy & hồi 28% HP", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.FireFeather, "Strike", 2.6f, 40f, 3.0f, Fire, 0.28f),
                M("Raging Sacred Fire", "Fire", 135, "Giáng 7 cột lửa thánh vàng kim thiêu rụi chiến trường", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.HoOh_SacredFire, "Hop", "Sacred_Fire_Pillar.None", "Explosion.None", 5.8f, 94f, 5.4f, Fire, 0.35f)
            };

            db["Gengar"] = new[]
            {
                A("Lick Mark", "Ghost", 55, "Đánh dấu mục tiêu nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.SludgeGlob, "Attack", 0.44f, 28f, 4.5f, Psychic),
                A("Toxic Hex", "Poison", 95, "Kích nổ ấn độc trên quái gây sát thương lan", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.SludgePool, "Shoot", 1.15f, 48f, 6.0f, Psychic),
                A("Dream Eater", "Psychic", 100, "Dây liên kết hút máu 5 đợt hồi HP cho chủ nhân", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.VoidPortal, "Strike", 2.5f, 56f, 5.5f, Psychic),
                M("Phantom Shadow Force", "Ghost", 150, "Biến mất vào cổng hư vô rồi trồi lên dưới chân quái phát nổ", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Giratina_ShadowForce, "Hop", "Shadow_Ball.None", "Explosion.None", 5.8f, 98f, 6.2f, Psychic, 0.38f)
            };

            db["Gyarados"] = new[]
            {
                A("Waterfall Slam", "Water", 85, "Quét 3 nhát hình nón hất văng kẻ địch", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.WaveCrest, "Attack", 0.45f, 34f, 2.6f, Water),
                A("Dragon Rage", "Dragon", 90, "Sóng đẩy văng cực mạnh kèm làm choáng", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.DoomStar, "Shoot", 1.25f, 46f, 4.5f, Water),
                A("Hyper Beam", "Normal", 150, "Bắn tia đại bác laser dài 9m xuyên thủng hàng địch", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Strike", 2.8f, 64f, 8.8f, Water),
                M("Leviathan Tsunami", "Water", 150, "Cưỡi sóng thần 5 cột cuốn trôi toàn bản đồ", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Squirtle_SurfTsunami, "Hop", "Water_Spout_Splash.None", "Aqua_Ring.None", 6.0f, 96f, 5.6f, Water, 0.38f)
            };

            db["Lapras"] = new[]
            {
                A("Ice Beam Sweep", "Ice", 90, "Tia cực hàn xuyên thẳng 9m đóng băng", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.IceCrystal, "Attack", 0.55f, 32f, 8.4f, Ice),
                A("Brine Bubble", "Water", 75, "Phóng bong bóng giam quái lơ lửng trên không", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.StasisFreezeField, PMDVisualAssetKind.BubblePrison, "Shoot", 1.2f, 44f, 6.2f, Water),
                A("Aurora Veil", "Ice", 80, "Tạo giáp ảo +65 HP & dựng tường pha lê đẩy quái", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.IceSpike, "Strike", 2.6f, 46f, 3.0f, Ice),
                M("Perish Snow Globe", "Ice", 130, "Mở kết giới tuyết lục giác khổng lồ + Hồi 46% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Glaceon_SnowGlobeBlizzard, "Hop", "Sheer_Cold_Front.None", "Aqua_Ring.None", 5.8f, 90f, 5.4f, Ice, 0.46f)
            };

            db["Snorlax"] = new[]
            {
                A("Block Counter", "Normal", 75, "Vào thế phản đòn triệt tiêu 100% sát thương & làm choáng", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.CrystalWall, "Attack", 0.55f, 32f, 2.5f, Normal),
                A("Rollout Bounce", "Rock", 85, "Đạn nảy bật giữa 4 mục tiêu liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.MudGlob, "Shoot", 1.2f, 44f, 5.5f, Normal),
                A("Heavy Slam", "Normal", 120, "Nhảy vọt lên không né đòn rồi giáng đè bẹp quái", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.Fissure, "Strike", 2.6f, 68f, 4.5f, Normal),
                A("Belly Drum Domain", "Normal", 140, "Khai triển lãnh địa giảm 50% hồi chiêu & hồi 50% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.DoomStar, "Hop", 6.2f, 92f, 5.0f, Normal, 0.50f)
            };

            db["Dragonite"] = new[]
            {
                A("Thunder Punch", "Electric", 75, "Hất tung + làm choáng cứng mục tiêu", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.FireFist, "Attack", 0.42f, 34f, 2.5f, Elec),
                A("Dragon Tail", "Dragon", 85, "Sóng đẩy văng quái cực mạnh kèm choáng", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.ClawX, "Shoot", 1.1f, 46f, 4.2f, Elec),
                A("Extreme Speed", "Normal", 100, "Lướt bất tử xuyên đội hình chém gục quái", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.WindCrescent, "Strike", 2.2f, 62f, 6.2f, Elec),
                A("Draco Cataclysm", "Dragon", 150, "Tụ lực 0.35s rồi giáng 14 thiên thạch toàn màn hình", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 6.0f, 100f, 5.5f, Fire, 0.38f)
            };

            db["Mewtwo"] = new[]
            {
                A("Barrier Shove", "Psychic", 70, "Tạo khiên ảo +65 HP & đẩy bức tường chặn đạn", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Attack", 0.48f, 32f, 3.0f, Psychic),
                A("Future Sight", "Psychic", 120, "Gắn bom thời gian tích sát thương 2.2s rồi kích nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.ClockRing, "Shoot", 1.25f, 48f, 6.5f, Psychic),
                A("Teleport Slash", "Psychic", 105, "Dịch chuyển tức thời ra sau lưng đánh chí mạng", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.VoidPortal, "Strike", 2.1f, 64f, 6.5f, Psychic),
                A("Psystrike Cataclysm", "Psychic", 160, "Giáng 14 tia hủy diệt siêu linh toàn màn hình + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.Supernova, "Hop", 6.0f, 105f, 5.8f, Psychic, 0.42f)
            };

            db["Tyranitar"] = new[]
            {
                A("Payback Counter", "Dark", 80, "Vào thế phản đòn triệt tiêu 100% & phản sát thương 2.2x", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.TectonicBlade, "Attack", 0.50f, 35f, 2.5f, Fight),
                A("Stealth Rock", "Rock", 90, "Chôn 3 bẫy đá ẩn phát nổ choáng khi quái bước vào", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.MudGlob, "Shoot", 1.3f, 48f, 6.0f, Fight),
                A("Crunch Execute", "Dark", 110, "Kết liễu ngay quái dưới 26% HP & Reset hồi chiêu", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.ClawX, "Strike", 2.4f, 64f, 2.8f, Fight),
                M("Sandstorm Precipice", "Rock", 150, "Triệu hồi 10 lưỡi kiếm đá khổng lồ khép vào tâm", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Groudon_PrecipiceBlades, "Hop", "Eruption.None", "Explosion.None", 6.0f, 98f, 5.4f, Fire, 0.38f)
            };

            db["Gardevoir"] = new[]
            {
                A("Moonblast Orb", "Fairy", 95, "Nhảy lùi thả diều + bắn quạt 3 tia ánh trăng", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.PetalBlossom, "Attack", 0.45f, 31f, 6.5f, Psychic),
                A("Psyshock Mark", "Psychic", 85, "Đánh dấu mục tiêu nhận thêm +40% sát thương", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.GoldStar, "Shoot", 1.05f, 44f, 6.5f, Psychic),
                A("Time Loop Warp", "Psychic", 95, "Đánh dấu vị trí & giật ngược quái về sau 1.8s", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.TimeLoopSnapback, PMDVisualAssetKind.ClockRing, "Strike", 2.5f, 58f, 5.5f, Psychic),
                A("Black Hole Singularity", "Psychic", 145, "Hố đen hút gom mọi quái vào tâm nghiền nát + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.Supernova, "Hop", 5.8f, 94f, 5.2f, Psychic, 0.45f)
            };

            db["Rayquaza"] = new[]
            {
                A("Outrage Sweep", "Dragon", 90, "Quét 3 nhát hình nón vuốt rồng lục bảo", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.40f, 36f, 2.8f, Grass),
                A("Dragon Ascent", "Flying", 120, "Bay vọt lên không né đòn rồi bổ nhào đập đất", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.WindCrescent, "Shoot", 1.25f, 56f, 5.0f, Grass),
                M("Emerald Aeroblast", "Flying", 130, "Bắn tia đại bác bão khí quyển xuyên bản đồ", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.Lugia_Aeroblast, "Strike", "Discharge.None", "Explosion.None", 2.4f, 68f, 8.5f, Elec),
                A("Sky Pillar Domain", "Dragon", 160, "Khai triển lãnh địa Bầu Trời (-50% hồi chiêu) + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.StormCloud, "Hop", 6.0f, 104f, 5.6f, Grass, 0.42f)
            };

            db["Garchomp"] = new[]
            {
                A("Sand Tomb", "Ground", 75, "Vùng cát lún hút chậm & rút máu liên tục 2.8s", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.Fissure, "Attack", 0.48f, 32f, 4.5f, Fight),
                A("Dig Ambush", "Ground", 85, "Độn thổ dịch chuyển ra sau lưng quái hất tung", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.MudGlob, "Shoot", 1.15f, 48f, 6.2f, Fight),
                A("Dragon Rush", "Dragon", 100, "Lướt bất tử xuyên đội hình chém gục quái", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.ClawX, "Strike", 2.2f, 62f, 6.0f, Fight),
                A("Livid Outrage", "Dragon", 145, "Kết liễu ngay quái dưới 26% HP & Reset hồi chiêu", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.TectonicBlade, "Hop", 5.6f, 98f, 4.5f, Fight, 0.38f)
            };

            db["Greninja"] = new[]
            {
                A("Smokescreen Kite", "Water", 65, "Nhảy lùi giữ khoảng cách + bắn quạt 3 phi tiêu", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.BubblePrison, "Attack", 0.38f, 30f, 6.5f, Water),
                A("Water Shuriken", "Water", 85, "Phóng 3 phi tiêu Boomerang bay đi quay về", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BoomerangReturn, PMDVisualAssetKind.GoldStar, "Shoot", 0.95f, 44f, 6.5f, Water),
                A("Double Team", "Dark", 95, "Tách 2 phân thân kẹp đánh và phát nổ tam giác", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.ShadowArrow, "Strike", 2.1f, 58f, 5.5f, Water),
                A("Surf Execute", "Water", 140, "Lướt sóng kết liễu quái <26% HP & Reset hồi chiêu", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.WaveCrest, "Hop", 5.5f, 94f, 4.8f, Water, 0.38f)
            };

            // =================================================================================
            // REMAINING 60 POKÉMON (#25..#84) — EVERY ONE WITH DISTINCT 4-ARCHETYPE COMBO
            // =================================================================================

            // 25. #0002 IVYSAUR
            db["Ivysaur"] = new[]
            {
                A("Vine Tether", "Grass", 65, "Dây leo hút máu 5 đợt hồi HP cho chủ nhân", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.VineSeg, "Attack", 0.50f, 28f, 5.2f, Grass),
                A("Razor Boomerang", "Grass", 70, "Phóng 3 lá Boomerang bay đi quay về cắt 2 lượt", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BoomerangReturn, PMDVisualAssetKind.LeafBlade, "Shoot", 1.05f, 38f, 6.5f, Grass),
                A("Poison Swamp", "Poison", 85, "Tạo đầm lầy độc sủi bọt rút máu suốt 2.8s", "L", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.SludgePool, "Strike", 2.5f, 52f, 5.2f, Grass),
                A("Overgrow Roots", "Grass", 125, "Triệu hồi ụ cây tự bắn đạn hạt giống 5 lần + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.ThornRoot, "Hop", 5.6f, 88f, 6.0f, Grass, 0.40f)
            };

            // 26. #0003 VENUSAUR
            db["Venusaur"] = new[]
            {
                A("Sweet Scent Mark", "Grass", 60, "Đánh dấu mục tiêu nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.PetalBlossom, "Attack", 0.45f, 30f, 5.0f, Grass),
                A("Sludge Detonate", "Poison", 95, "Kích nổ ấn độc gây sát thương lan diện rộng", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.SludgeGlob, "Shoot", 1.15f, 48f, 6.2f, Grass),
                M("Petal Dance", "Grass", 120, "Xoáy bão cánh hoa anh đào quanh thân liên hoàn", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.Venusaur_PetalDance, "Strike", "Synthesis_Sparkle.None", "Energy_Ball_Hit.None", 2.5f, 64f, 5.6f, Grass),
                A("Solar Beam Cannon", "Grass", 150, "Bắn tia laser Mặt Trời 9m xuyên thấu + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Hop", 6.0f, 98f, 8.8f, Grass, 0.45f)
            };

            // 27. #0005 CHARMELEON
            db["Charmeleon"] = new[]
            {
                A("Fire Claw", "Fire", 70, "Quét 3 nhát vuốt lửa hình nón trước mặt", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.42f, 30f, 2.5f, Fire),
                A("Flame Charge", "Fire", 75, "Lướt bất tử trong lửa xuyên qua kẻ địch", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.FireOrb, "Shoot", 1.1f, 42f, 5.5f, Fire),
                A("Blaze Sacrifice", "Fire", 110, "Hiến 12% HP để Reset toàn bộ hồi chiêu & nổ lớn", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.FireFist, "Strike", 3.0f, 64f, 3.6f, Fire),
                A("Fire Spin Vortex", "Fire", 120, "Hố lửa hút gom mọi quái vào tâm thiêu đốt + Hồi 35% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.FireOrb, "Hop", 5.8f, 88f, 5.2f, Fire, 0.35f)
            };

            // 28. #0008 WARTORTLE
            db["Wartortle"] = new[]
            {
                A("Shell Parry", "Water", 70, "Thu mai phản đòn triệt tiêu 100% sát thương & làm choáng", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.BubblePrison, "Attack", 0.50f, 30f, 2.5f, Water),
                A("Water Pulse", "Water", 75, "Đạn sóng nước nảy bật giữa 4 mục tiêu", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.WaveCrest, "Shoot", 1.05f, 40f, 5.5f, Water),
                A("Aqua Shield", "Water", 85, "Tạo khiên nước +65 HP & đẩy tường sóng ra trước", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Strike", 2.5f, 50f, 3.0f, Water),
                A("Rain Dance Domain", "Water", 120, "Khai triển lãnh địa mưa (-50% hồi chiêu) + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.BubblePrison, "Hop", 5.8f, 86f, 5.0f, Water, 0.40f)
            };

            // 29. #0009 BLASTOISE
            db["Blastoise"] = new[]
            {
                A("Shell Smash", "Normal", 80, "Hất tung + làm choáng cứng mục tiêu cận chiến", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.WaveCrest, "Attack", 0.45f, 34f, 2.5f, Water),
                A("Water Turret", "Water", 85, "Đặt trụ đại bác nước tự bắn 5 phát vào quái gần", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.BubblePrison, "Shoot", 1.3f, 44f, 6.2f, Water),
                A("Hydro Repulse", "Water", 100, "Sóng đẩy văng cực mạnh kèm làm choáng", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.WaveCrest, "Strike", 2.4f, 58f, 4.0f, Water),
                M("Dual Hydro Cannon", "Water", 150, "Khai hỏa song pháo thủy lực từ hai ống đại bác trên vai", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Blastoise_HydroCannon, "Hop", "Water_Spout_Splash.None", "Explosion.None", 6.0f, 98f, 7.2f, Water, 0.40f)
            };

            // 30. #0026 RAICHU
            db["Raichu"] = new[]
            {
                A("Nuzzle Mark", "Electric", 55, "Đánh dấu tĩnh điện tăng +40% sát thương nhận vào", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.Bolt, "Attack", 0.40f, 28f, 4.5f, Elec),
                A("Volt Bounce", "Electric", 85, "Tia sét nảy bật giữa 4 kẻ địch liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.Electroweb, "Shoot", 1.0f, 44f, 5.5f, Elec),
                A("Thunder Detonate", "Electric", 105, "Kích nổ ấn tĩnh điện gây giật lan diện rộng", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.Bolt, "Strike", 2.2f, 62f, 5.8f, Elec),
                A("Surge Cataclysm", "Electric", 150, "Tụ điện giáng 14 tia sét toàn màn hình + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.StormCloud, "Hop", 5.6f, 96f, 5.5f, Elec, 0.38f)
            };

            // 31. #0038 NINETALES
            db["Ninetales"] = new[]
            {
                A("Foxfire Kite", "Fire", 65, "Nhảy lùi thả diều + bắn quạt 3 tia hồ ly hỏa", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.FireFeather, "Attack", 0.44f, 30f, 6.2f, Fire),
                A("Cursed Mines", "Ghost", 85, "Đặt 3 bẫy ma trơi tự phát nổ choáng khi quái chạm", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.FireOrb, "Shoot", 1.25f, 44f, 6.0f, Fire),
                A("Drought Transform", "Fire", 100, "Hóa thân Cửu Vĩ tăng 45% tốc chạy & giáp lửa", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.FireFeather, "Strike", 2.6f, 56f, 3.5f, Fire),
                A("Sacred Sun Domain", "Fire", 140, "Khai triển lãnh địa Mặt Trời (-50% hồi chiêu) + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.DoomStar, "Hop", 5.8f, 92f, 5.2f, Fire, 0.40f)
            };

            // 32. #0065 ALAKAZAM
            db["Alakazam"] = new[]
            {
                A("Spoon Bend", "Psychic", 70, "Đâm xuyên giáp tâm linh 1 điểm sát thương lớn", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.GoldStar, "Attack", 0.42f, 32f, 4.5f, Psychic),
                A("Time Loop", "Psychic", 90, "Đánh dấu & giật ngược quái về vị trí cũ sau 1.8s", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeLoopSnapback, PMDVisualAssetKind.ClockRing, "Shoot", 1.25f, 46f, 5.5f, Psychic),
                A("Psychic Bomb", "Psychic", 110, "Gắn bom thời gian tích 85% sát thương trong 2.2s rồi nổ", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.DoomStar, "Strike", 2.4f, 56f, 6.2f, Psychic),
                A("Miracle Eye Stasis", "Psychic", 145, "Đóng băng không gian toàn vùng & dồn sát thương + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.StasisFreezeField, PMDVisualAssetKind.Supernova, "Hop", 5.8f, 96f, 5.4f, Psychic, 0.42f)
            };

            // 33. #0068 MACHAMP
            db["Machamp"] = new[]
            {
                A("Karate Chop", "Fighting", 75, "Chặt tay xuyên giáp 1 mục tiêu cực mạnh", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.AuraFist, "Attack", 0.38f, 34f, 2.4f, Fight),
                A("Bulk Up Parry", "Fighting", 85, "Thế phản đòn triệt tiêu 100% & đánh bật choáng", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.FireFist, "Shoot", 1.2f, 46f, 2.8f, Fight),
                A("Vital Throw", "Fighting", 105, "Quật văng kẻ địch cực mạnh gây choáng cứng", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.AuraFist, "Strike", 2.3f, 62f, 4.0f, Fight),
                M("Dynamic Punch", "Fighting", 150, "Tung 8 cú đấm 4 tay liên hoàn ORA-ORA! + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Machamp_DynamicPunch, "Hop", "Close_Combat.None", "Explosion.None", 5.6f, 98f, 4.8f, Fight, 0.38f)
            };

            // 34. #0134 VAPOREON
            db["Vaporeon"] = new[]
            {
                A("Acid Armor", "Water", 65, "Hóa lỏng lướt bất tử xuyên qua kẻ địch", "J", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.BubblePrison, "Attack", 0.45f, 30f, 5.0f, Water),
                A("Aqua Turret", "Water", 80, "Triệu hồi cầu nước tự bắn 5 tia vào quái gần", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.WaveCrest, "Shoot", 1.2f, 42f, 6.2f, Water),
                A("Aqua Ring Cleanse", "Water", 85, "Thanh tẩy hồi 28% HP & tăng 38% tốc chạy", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.BubblePrison, "Strike", 2.5f, 44f, 3.2f, Water, 0.28f),
                A("Maelstrom Vortex", "Water", 140, "Tạo hố xoáy đại dương hút gom toàn bộ quái + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.WaveCrest, "Hop", 5.8f, 92f, 5.4f, Water, 0.45f)
            };

            // 35. #0135 JOLTEON
            db["Jolteon"] = new[]
            {
                A("Quick Blink", "Electric", 70, "Dịch chuyển tức thời ra sau lưng chích điện", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.Bolt, "Attack", 0.42f, 32f, 5.8f, Elec),
                A("Shock Mines", "Electric", 80, "Chôn 3 mìn tĩnh điện nổ choáng khi quái bước vào", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.Electroweb, "Shoot", 1.2f, 44f, 6.0f, Elec),
                A("Volt Mirage", "Electric", 95, "Tách 2 phân thân sấm sét kẹp nổ tam giác", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.Bolt, "Strike", 2.1f, 58f, 5.5f, Elec),
                A("Lightning Form", "Electric", 135, "Hóa thành luồng sét thuần túy tăng 45% tốc & giật liên hoàn", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.StormCloud, "Hop", 5.6f, 92f, 4.8f, Elec, 0.36f)
            };

            // 36. #0136 FLAREON
            db["Flareon"] = new[]
            {
                A("Flare Sacrificial", "Fire", 100, "Hiến 12% HP để Reset toàn bộ hồi chiêu & nổ lửa", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.FireOrb, "Attack", 2.0f, 52f, 3.5f, Fire),
                A("Lava Pool", "Fire", 85, "Để lại hồ dung nham cháy bỏng mặt đất suốt 2.8s", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.FireFist, "Shoot", 1.15f, 44f, 5.5f, Fire),
                A("Fire Spin Push", "Fire", 95, "Quét 3 nhát lửa hình nón đẩy lùi quái", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.FireFeather, "Strike", 2.0f, 58f, 2.8f, Fire),
                A("Supernova Cataclysm", "Fire", 145, "Giáng 14 thiên thạch lửa toàn màn hình + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 5.8f, 94f, 5.5f, Fire, 0.38f)
            };

            // 37. #0144 ARTICUNO
            db["Articuno"] = new[]
            {
                A("Ice Beam 9m", "Ice", 90, "Tia băng giá dài 9m xuyên thấu 3 đợt", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.IceCrystal, "Attack", 0.55f, 33f, 8.6f, Ice),
                A("Tailwind Kite", "Flying", 75, "Nhảy lùi thả diều + bắn quạt 3 tia gió tuyết", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.WindCrescent, "Shoot", 1.0f, 42f, 6.8f, Ice),
                A("Glacier Turret", "Ice", 95, "Dựng trụ pha lê băng tự bắn 5 tia vào quái", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.IceSpike, "Strike", 2.6f, 52f, 6.2f, Ice),
                A("Absolute Zero Stasis", "Ice", 150, "Đóng băng hoàn toàn không gian vùng rộng + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.StasisFreezeField, PMDVisualAssetKind.SnowGlobe, "Hop", 5.8f, 98f, 5.6f, Ice, 0.42f)
            };

            // 38. #0145 ZAPDOS
            db["Zapdos"] = new[]
            {
                A("Shock Bounce", "Electric", 80, "Tia sét nảy bật giữa 4 kẻ địch liên tiếp", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.Bolt, "Attack", 0.44f, 33f, 5.5f, Elec),
                A("Thunder Dive", "Flying", 100, "Bay vọt lên trời né đòn rồi giáng sét xuống", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.StormCloud, "Shoot", 1.25f, 52f, 4.8f, Elec),
                A("Static Laser", "Electric", 110, "Bắn tia điện cao áp 9m xuyên thủng hàng địch", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.Bolt, "Strike", 2.4f, 62f, 8.8f, Elec),
                A("Thunder Domain", "Electric", 150, "Khai triển lãnh địa Bão Sấm (-50% hồi chiêu) + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.StormCloud, "Hop", 5.8f, 98f, 5.5f, Elec, 0.40f)
            };

            // 39. #0146 MOLTRES
            db["Moltres"] = new[]
            {
                A("Flame Plume", "Fire", 75, "Quét 3 nhát cánh lửa hình nón trước mặt", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.FireFeather, "Attack", 0.42f, 33f, 2.8f, Fire),
                A("Scorching Pool", "Fire", 85, "Để lại hồ lửa thiêu đốt mặt đất suốt 2.8s", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.FireOrb, "Shoot", 1.15f, 46f, 5.5f, Fire),
                A("Phoenix Rebirth", "Fire", 110, "Hiến 12% HP để Reset toàn bộ hồi chiêu & nổ tung", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.FireFeather, "Strike", 2.8f, 66f, 3.8f, Fire),
                A("Solar Cataclysm", "Fire", 155, "Giáng 14 thiên thạch lửa toàn màn hình + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 6.0f, 100f, 5.6f, Fire, 0.45f)
            };

            // 40. #0152 CHIKORITA
            db["Chikorita"] = new[]
            {
                A("Razor Boomerang", "Grass", 60, "Phóng 3 lá Boomerang bay đi quay về", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BoomerangReturn, PMDVisualAssetKind.LeafBlade, "Attack", 0.45f, 27f, 6.2f, Grass),
                A("Aromatherapy", "Grass", 65, "Hồi 28% HP & tăng 38% tốc độ di chuyển", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Shoot", 1.3f, 34f, 3.2f, Grass, 0.28f),
                A("Reflect Barrier", "Psychic", 75, "Tạo khiên +65 HP & đẩy tường lá ra trước", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Strike", 2.5f, 46f, 3.0f, Grass),
                A("Flower Turret", "Grass", 110, "Triệu hồi hoa mặt trời tự bắn 5 phát vào quái + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.PetalBlossom, "Hop", 5.6f, 84f, 6.2f, Grass, 0.45f)
            };

            // 41. #0155 CYNDAQUIL
            db["Cyndaquil"] = new[]
            {
                A("Flame Backstep", "Fire", 60, "Nhảy lùi giữ khoảng cách + bắn quạt 3 đốm lửa", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.FireOrb, "Attack", 0.40f, 27f, 6.0f, Fire),
                A("Ember Mines", "Fire", 75, "Đặt 3 mìn lửa ẩn phát nổ choáng khi quái chạm", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.FireFeather, "Shoot", 1.2f, 40f, 5.8f, Fire),
                A("Flame Wheel", "Fire", 85, "Lướt bất tử trong vòng lửa xuyên kẻ địch", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.FireOrb, "Strike", 2.2f, 54f, 5.5f, Fire),
                A("Eruption Storm", "Fire", 125, "Giáng 14 tia dung nham toàn màn hình + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 5.8f, 88f, 5.2f, Fire, 0.38f)
            };

            // 42. #0157 TYPHLOSION
            db["Typhlosion"] = new[]
            {
                A("Flame Mark", "Fire", 70, "Đánh dấu mục tiêu nhận thêm +40% sát thương lửa", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.FireOrb, "Attack", 0.42f, 32f, 5.0f, Fire),
                A("Inferno Detonate", "Fire", 100, "Kích nổ ấn lửa gây sát thương lan diện rộng", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.FireFist, "Shoot", 1.15f, 48f, 6.2f, Fire),
                A("Blast Burn Pool", "Fire", 115, "Đốt mặt đất thành hồ dung nham suốt 2.8s", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.SludgePool, "Strike", 2.5f, 62f, 5.8f, Fire),
                A("Hisui Cataclysm", "Fire", 155, "Tụ lực giáng 14 cột dung nham toàn màn hình + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 6.0f, 98f, 5.6f, Fire, 0.40f)
            };

            // 43. #0158 TOTODILE
            db["Totodile"] = new[]
            {
                A("Bite Pierce", "Dark", 65, "Cắn xuyên giáp 1 mục tiêu gây sát thương lớn", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.ClawX, "Attack", 0.40f, 29f, 2.4f, Water),
                A("Water Gun Bounce", "Water", 65, "Tia nước nảy bật giữa 4 mục tiêu liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.BubblePrison, "Shoot", 1.05f, 38f, 5.2f, Water),
                A("Crunch Jump", "Dark", 85, "Nhảy vọt lên không rồi cắn bổ nhào xuống đất", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.WaveCrest, "Strike", 2.2f, 54f, 4.5f, Water),
                A("Aqua Execute", "Water", 115, "Kết liễu ngay quái dưới 26% HP & Reset hồi chiêu", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.WaveCrest, "Hop", 5.5f, 86f, 4.2f, Water, 0.38f)
            };

            // 44. #0160 FERALIGATR
            db["Feraligatr"] = new[]
            {
                A("Ice Fang Stun", "Ice", 70, "Nanh băng hất tung + làm choáng cứng mục tiêu", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.IceSpike, "Attack", 0.42f, 33f, 2.5f, Water),
                A("Jaw Execute", "Dark", 100, "Kết liễu ngay quái dưới 26% HP & Reset hồi chiêu", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.ClawX, "Shoot", 1.25f, 50f, 2.8f, Water),
                A("Dragon Dance", "Dragon", 95, "Hóa cuồng nộ tăng 45% tốc chạy & giáp nước", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.WaveCrest, "Strike", 2.6f, 58f, 3.6f, Water),
                A("Raging Repulse", "Water", 145, "Sóng đẩy văng toàn vùng cực mạnh + Choáng + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.WaveCrest, "Hop", 5.8f, 96f, 4.5f, Water, 0.38f)
            };

            // 45. #0181 AMPHAROS
            db["Ampharos"] = new[]
            {
                A("Signal Beam", "Bug", 75, "Tia ánh sáng 9m xuyên thẳng 3 đợt", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Attack", 0.50f, 32f, 8.2f, Elec),
                A("Beacon Turret", "Electric", 85, "Đặt ngọn hải đăng tự bắn 5 tia sét vào quái", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.DoomStar, "Shoot", 1.25f, 44f, 6.2f, Elec),
                A("Cotton Guard", "Grass", 80, "Tạo khiên bông +65 HP & đẩy sóng xung kích", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Strike", 2.5f, 48f, 3.0f, Elec),
                M("Lighthouse Judgment", "Electric", 145, "Giáng 16 tia sáng phán quyết đa sắc từ bầu trời", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Arceus_Judgment, "Hop", "Discharge.None", "Synthesis_Sparkle.None", 5.8f, 96f, 5.4f, Elec, 0.44f)
            };

            // 46. #0196 ESPEON
            db["Espeon"] = new[]
            {
                A("Psybeam", "Psychic", 70, "Bắn tia tâm linh dài 9m xuyên thấu 3 đợt", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Attack", 0.48f, 31f, 8.2f, Psychic),
                A("Future Sight Bomb", "Psychic", 110, "Gắn bom thời gian tích sát thương 2.2s rồi nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.ClockRing, "Shoot", 1.2f, 46f, 6.5f, Psychic),
                A("Morning Sun", "Normal", 80, "Hồi 28% HP & tăng 38% tốc độ di chuyển", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.DoomStar, "Strike", 2.4f, 42f, 3.2f, Psychic, 0.28f),
                A("Psychic Snapback", "Psychic", 135, "Đánh dấu & giật ngược toàn bộ quái về vị trí cũ + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.TimeLoopSnapback, PMDVisualAssetKind.VoidPortal, "Hop", 5.8f, 92f, 5.4f, Psychic, 0.40f)
            };

            // 47. #0197 UMBREON
            db["Umbreon"] = new[]
            {
                A("Mean Look Mark", "Dark", 60, "Đánh dấu mục tiêu nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.VoidPortal, "Attack", 0.44f, 29f, 4.8f, Psychic),
                A("Moonlight Parry", "Dark", 80, "Thế phản đòn triệt tiêu 100% sát thương & làm choáng", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.CrystalWall, "Shoot", 1.2f, 44f, 2.8f, Psychic),
                A("Foul Play Clones", "Dark", 95, "Tạo 2 phân thân bóng tối kẹp nổ tam giác", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.ShadowArrow, "Strike", 2.2f, 58f, 5.5f, Psychic),
                M("Dark Void Stasis", "Dark", 135, "Mở hố đen ác mộng hút chặt và gây choáng toàn bộ quái + Hồi 50% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Darkrai_DarkVoid, "Hop", "Shadow_Ball.None", "Explosion.None", 5.8f, 90f, 5.2f, Psychic, 0.50f)
            };

            // 48. #0212 SCIZOR
            db["Scizor"] = new[]
            {
                A("Bullet Punch", "Steel", 65, "Đâm xuyên giáp tốc độ ánh sáng", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.AuraFist, "Attack", 0.36f, 32f, 2.4f, Fight),
                A("Double Hit Clones", "Bug", 85, "Tạo 2 phân thân đỏ rực kẹp chém phát nổ", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.ClawX, "Shoot", 1.05f, 46f, 5.5f, Fight),
                A("X-Scissor Dash", "Bug", 100, "Lướt bất tử xuyên đội hình chém gục quái", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.CrossSlash, "Strike", 2.1f, 60f, 6.0f, Fight),
                A("Technician Execute", "Steel", 145, "Kết liễu ngay quái <26% HP & Reset toàn bộ hồi chiêu", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.ClawX, "Hop", 5.5f, 96f, 4.2f, Fight, 0.38f)
            };

            // 49. #0249 LUGIA
            db["Lugia"] = new[]
            {
                A("Gust Repulse", "Flying", 75, "Sóng gió đẩy văng toàn bộ quái ra xa + Choáng", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.WindCrescent, "Attack", 0.48f, 33f, 4.0f, Water),
                A("Multiscale Veil", "Psychic", 80, "Tạo khiên +65 HP & đẩy tường pha lê ra trước", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Shoot", 1.2f, 44f, 3.2f, Water),
                M("Aeroblast", "Flying", 130, "Bắn tia đại bác bão khí quyển bạc-lam dài 8m", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.Lugia_Aeroblast, "Strike", "Dragon_Pulse_Ball.None", "Shock_Wave.None", 2.4f, 68f, 8.2f, Water),
                A("Silver Sea Domain", "Water", 155, "Khai triển lãnh địa Biển Bạc (-50% hồi chiêu) + Hồi 48% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.WaveCrest, "Hop", 6.0f, 100f, 5.6f, Water, 0.48f)
            };

            // 50. #0250 HO_OH
            db["Ho_Oh"] = new[]
            {
                A("Rainbow Kite", "Fire", 75, "Nhảy lùi thả diều + bắn quạt 3 lông vũ cầu vồng", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.FireFeather, "Attack", 0.44f, 32f, 6.5f, Fire),
                A("Sacred Regen", "Fire", 85, "Thanh tẩy hồi 28% HP & tăng 38% tốc độ bay", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Shoot", 1.3f, 42f, 3.5f, Fire, 0.28f),
                M("Sacred Fire", "Fire", 120, "Giáng 7 cột lửa thánh vàng kim thiêu đốt vùng địch", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.HoOh_SacredFire, "Strike", "Sacred_Fire_Pillar.None", "Explosion.None", 2.5f, 68f, 6.5f, Fire),
                A("Phoenix Rebirth", "Fire", 160, "Hiến 12% HP Reset hồi chiêu & nổ Phượng Hoàng + Hồi 55% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.FireFeather, "Hop", 6.0f, 102f, 5.0f, Fire, 0.55f)
            };

            // 51. #0251 CELEBI
            db["Celebi"] = new[]
            {
                A("Time Loop Seed", "Psychic", 70, "Đánh dấu & giật ngược quái về vị trí cũ sau 1.8s", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeLoopSnapback, PMDVisualAssetKind.ClockRing, "Attack", 0.55f, 30f, 5.5f, Grass),
                A("Time Bomb", "Psychic", 90, "Gắn bom thời gian tích 85% sát thương rồi kích nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.DoomStar, "Shoot", 1.2f, 44f, 6.2f, Grass),
                A("Forest Sanctuary", "Grass", 85, "Hồi 28% HP & tăng 38% tốc độ chạy cho đồng đội", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Strike", 2.4f, 42f, 3.5f, Grass, 0.28f),
                M("Chrono Stasis", "Psychic", 135, "Đóng băng thời gian toàn vùng bằng đồng hồ cổ đại + Hồi 60% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Dialga_RoarOfTime, "Hop", "Synthesis_Sparkle.None", "Circle_Green_Out.None", 5.8f, 92f, 5.2f, Grass, 0.60f)
            };

            // 52. #0254 SCEPTILE
            db["Sceptile"] = new[]
            {
                A("X-Scissor", "Bug", 80, "Quét 3 nhát kiếm lá hình nón tốc độ cao", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.CrossSlash, "Attack", 0.36f, 33f, 2.6f, Grass),
                A("Leaf Boomerang", "Grass", 85, "Phóng 3 lưỡi kiếm lá Boomerang bay đi quay về", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BoomerangReturn, PMDVisualAssetKind.LeafBlade, "Shoot", 0.95f, 44f, 6.8f, Grass),
                A("Jungle Blink", "Grass", 100, "Dịch chuyển tức thời ra sau lưng chém chí mạng", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.VineSeg, "Strike", 2.0f, 60f, 6.4f, Grass),
                A("Frenzy Execute", "Grass", 145, "Kết liễu ngay quái <26% HP & Reset hồi chiêu + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.ThornRoot, "Hop", 5.6f, 96f, 4.5f, Grass, 0.40f)
            };

            // 53. #0257 BLAZIKEN
            db["Blaziken"] = new[]
            {
                A("Sky Uppercut", "Fighting", 85, "Đấm móc hất tung + làm choáng cứng mục tiêu", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.FireFist, "Attack", 0.40f, 34f, 2.5f, Fire),
                A("Blaze Kick", "Fire", 100, "Nhảy vọt lên không né đòn rồi đá lửa bổ xuống", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.FireFeather, "Shoot", 1.15f, 52f, 4.8f, Fire),
                A("Speed Boost Form", "Fire", 95, "Hóa lửa tăng 45% tốc chạy & giáp lửa quanh thân", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.FireOrb, "Strike", 2.5f, 58f, 3.6f, Fire),
                M("Overheat Phoenix", "Fire", 150, "Sải cánh Phượng Hoàng lửa chữ V khổng lồ + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Torchic_PhoenixSkyUppercut, "Hop", "Eruption.None", "Sacred_Fire_Pillar.None", 5.8f, 98f, 5.4f, Fire, 0.38f)
            };

            // 54. #0260 SWAMPERT
            db["Swampert"] = new[]
            {
                A("Hammer Arm", "Fighting", 90, "Đập tay búa xuyên giáp gây sát thương đơn cực lớn", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.AuraFist, "Attack", 0.46f, 35f, 2.5f, Water),
                A("Mud Swamp", "Ground", 80, "Tạo đầm lầy bùn nước rút máu suốt 2.8s", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.SludgePool, "Shoot", 1.15f, 44f, 5.5f, Water),
                A("Muddy Repulse", "Water", 100, "Sóng bùn đẩy văng cực mạnh kèm làm choáng", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.WaveCrest, "Strike", 2.4f, 60f, 4.2f, Water),
                M("Earthquake Fissure", "Ground", 150, "Dậm nứt toác mặt đất thành 3 khe vực chữ Y + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Mudkip_EarthquakeFissure, "Hop", "Water_Spout_Splash.None", "Explosion.None", 6.0f, 98f, 5.4f, Water, 0.42f)
            };

            // 55. #0306 AGGRON
            db["Aggron"] = new[]
            {
                A("Metal Burst", "Steel", 85, "Vào thế phản đòn triệt tiêu 100% & bật ngược 2.2x", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.CrystalWall, "Attack", 0.50f, 34f, 2.6f, Normal),
                A("Iron Shield", "Steel", 80, "Tạo giáp thép +65 HP & đẩy bức tường kim loại", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.TectonicBlade, "Shoot", 1.2f, 46f, 3.2f, Normal),
                A("Head Smash", "Rock", 120, "Hiến 12% HP Reset toàn bộ hồi chiêu & húc nổ lớn", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.MudGlob, "Strike", 2.8f, 68f, 3.8f, Normal),
                A("Iron Fortress Domain", "Steel", 145, "Khai triển lãnh địa Pháo Đài (-50% hồi chiêu) + Hồi 44% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.IceSpike, "Hop", 6.0f, 96f, 5.4f, Normal, 0.44f)
            };

            // 56. #0330 FLYGON
            db["Flygon"] = new[]
            {
                A("Sand Kite", "Ground", 70, "Nhảy lùi giữ khoảng cách + bắn quạt 3 tia bão cát", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.WindCrescent, "Attack", 0.42f, 30f, 6.2f, Grass),
                A("Boomburst", "Normal", 90, "Sóng âm đẩy văng quái cực mạnh + Làm choáng", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.DoomStar, "Shoot", 1.1f, 44f, 4.2f, Grass),
                A("Dragon Dive", "Dragon", 105, "Bay vọt lên trời né đòn rồi giáng xuống mặt đất", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.ClawX, "Strike", 2.3f, 60f, 4.8f, Grass),
                A("Sandstorm Domain", "Ground", 140, "Khai triển lãnh địa Bão Cát (-50% hồi chiêu) + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.Fissure, "Hop", 5.8f, 94f, 5.2f, Grass, 0.38f)
            };

            // 57. #0350 MILOTIC
            db["Milotic"] = new[]
            {
                A("Water Pulse", "Water", 70, "Sóng nước nảy bật giữa 4 mục tiêu liên tiếp", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.BubblePrison, "Attack", 0.44f, 30f, 5.5f, Water),
                A("Aqua Drain", "Water", 80, "Dây liên kết nước hút máu 5 đợt hồi HP cho đội", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.WaveCrest, "Shoot", 1.15f, 42f, 5.5f, Water),
                A("Recover Cleanse", "Water", 80, "Hồi 28% HP & tăng 38% tốc độ bơi lướt", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Strike", 2.4f, 42f, 3.5f, Water, 0.28f),
                M("Origin Pulse Wave", "Water", 140, "Phóng 3 quả cầu năng lượng xanh đại dương cực đại + Hồi 52% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Kyogre_OriginPulse, "Hop", "Water_Spout_Splash.None", "Aqua_Ring.None", 5.8f, 94f, 8.0f, Water, 0.52f)
            };

            // 58. #0359 ABSOL
            db["Absol"] = new[]
            {
                A("Pursuit Mark", "Dark", 60, "Đánh dấu con mồi nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.ShadowArrow, "Attack", 0.38f, 30f, 4.8f, Psychic),
                A("Psycho Cut", "Psychic", 85, "Kích nổ ấn định mệnh gây sát thương lan", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.ClawX, "Shoot", 1.0f, 46f, 6.0f, Psychic),
                A("Night Slash Execute", "Dark", 110, "Kết liễu ngay quái <26% HP & Reset toàn bộ hồi chiêu", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.CrossSlash, "Strike", 2.2f, 62f, 2.8f, Psychic),
                M("Spacial Disaster", "Dark", 145, "Xé rách không gian bằng 4 nhát trảm bóng tối + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Palkia_SpacialRend, "Hop", "Shadow_Ball.None", "Explosion.None", 5.6f, 96f, 5.5f, Psychic, 0.38f)
            };

            // 59. #0373 SALAMENCE
            db["Salamence"] = new[]
            {
                A("Dragon Fang", "Dragon", 80, "Quét 3 nhát vuốt rồng hình nón trước mặt", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.40f, 34f, 2.7f, Fire),
                A("Fire Breath", "Fire", 90, "Phun tia lửa 9m xuyên thấu 3 đợt liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Shoot", 1.15f, 46f, 8.6f, Fire),
                A("Fly Dive", "Flying", 115, "Bay vọt lên cao né đòn rồi bổ nhào đập đất", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.WindCrescent, "Strike", 2.4f, 64f, 5.0f, Fire),
                M("Draco Meteor", "Dragon", 155, "Kích hoạt 12 cột núi lửa thiên thạch giáng xuống + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Charizard_BlastBurnVolcano, "Hop", "Eruption.None", "Explosion.None", 6.0f, 100f, 5.4f, Fire, 0.38f)
            };

            // 60. #0376 METAGROSS
            db["Metagross"] = new[]
            {
                A("Meteor Mash", "Steel", 90, "Hất tung + làm choáng cứng bằng nắm đấm thiên thạch", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.AuraFist, "Attack", 0.44f, 35f, 2.5f, Normal),
                A("Magnet Mines", "Steel", 85, "Đặt 3 mìn từ trường phát nổ choáng khi quái chạm", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.ClockRing, "Shoot", 1.25f, 46f, 6.2f, Normal),
                A("Zen Shield", "Psychic", 95, "Tạo khiên thép +65 HP & đẩy tường từ trường", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Strike", 2.5f, 54f, 3.2f, Normal),
                M("Doom Desire", "Steel", 155, "Khắc ấn ngôi sao hủy diệt, phát nổ sau 1.2s + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Jirachi_DoomDesire, "Hop", "Explosion.None", "Shock_Wave.None", 5.8f, 102f, 7.0f, Normal, 0.42f)
            };

            // 61. #0382 KYOGRE
            db["Kyogre"] = new[]
            {
                A("Ice Beam", "Ice", 90, "Bắn tia băng đại dương dài 9m xuyên thấu", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.IceCrystal, "Attack", 0.50f, 35f, 8.8f, Water),
                M("Origin Pulse", "Water", 120, "Phóng 3 quả cầu năng lượng xanh đại dương cực đại", "K", PMDSkillKind.Projectile, PMDUniqueMoveId.Kyogre_OriginPulse, "Shoot", "Water_Spout_Splash.None", "Aqua_Ring.None", 1.15f, 54f, 8.5f, Water),
                A("Abyssal Vortex", "Water", 115, "Hố xoáy biển sâu hút gom mọi quái vào tâm", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.WaveCrest, "Strike", 2.5f, 64f, 5.5f, Water),
                A("Primordial Cataclysm", "Water", 165, "Tạo mưa thiên tai đại dương toàn màn hình + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.BubblePrison, "Hop", 6.0f, 106f, 5.8f, Water, 0.45f)
            };

            // 62. #0383 GROUDON
            db["Groudon"] = new[]
            {
                A("Hammer Smash", "Fighting", 90, "Hất tung + làm choáng cứng bằng đòn nện đất", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.TectonicBlade, "Attack", 0.45f, 36f, 2.6f, Fire),
                A("Lava Fissure", "Fire", 95, "Tạo vùng dung nham cháy bỏng mặt đất suốt 2.8s", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.FireOrb, "Shoot", 1.2f, 48f, 5.8f, Fire),
                M("Precipice Blades", "Ground", 130, "Triệu hồi 10 lưỡi kiếm đá khổng lồ khép vào tâm", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.Groudon_PrecipiceBlades, "Strike", "Eruption.None", "Explosion.None", 2.5f, 70f, 6.6f, Fire),
                A("Desolate Domain", "Fire", 165, "Khai triển lãnh địa Dung Nham (-50% hồi chiêu) + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.FireOrb, "Hop", 6.0f, 106f, 5.8f, Fire, 0.42f)
            };

            // 63. #0385 JIRACHI
            db["Jirachi"] = new[]
            {
                A("Wish Cleanse", "Psychic", 65, "Hồi 28% HP & tăng 38% tốc độ di chuyển", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.DoomStar, "Attack", 0.65f, 28f, 3.5f, Elec, 0.28f),
                A("Starlight Bomb", "Psychic", 95, "Gắn bom thời gian tích sát thương 2.2s rồi nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.ClockRing, "Shoot", 1.2f, 46f, 6.4f, Elec),
                A("Wish Turret", "Steel", 90, "Triệu hồi ngôi sao ước nguyện tự bắn 5 tia vào quái", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.GoldStar, "Strike", 2.5f, 52f, 6.4f, Elec),
                M("Doom Desire", "Steel", 155, "Khắc ấn ngôi sao hủy diệt, phát nổ sau 1.2s + Hồi 55% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Jirachi_DoomDesire, "Hop", "Explosion.None", "Synthesis_Sparkle.None", 5.8f, 102f, 7.0f, Elec, 0.55f)
            };

            // 64. #0387 TURTWIG
            db["Turtwig"] = new[]
            {
                A("Bite Stun", "Dark", 60, "Cắn hất tung + làm choáng nhẹ mục tiêu", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.KnockUpStunImpact, PMDVisualAssetKind.ThornRoot, "Attack", 0.44f, 28f, 2.2f, Grass),
                A("Seed Turret", "Grass", 70, "Trồng mầm cây tự bắn 5 hạt giống vào quái gần", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.BulletSeed, "Shoot", 1.25f, 38f, 6.0f, Grass),
                A("Synthesis Shield", "Grass", 75, "Tạo khiên lá +65 HP & đẩy tường gai ra trước", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ShieldBarrierPush, PMDVisualAssetKind.CrystalWall, "Strike", 2.4f, 46f, 3.0f, Grass),
                A("Leech Drain", "Grass", 110, "Dây leo hút máu 5 đợt hồi HP cho chủ nhân + Hồi 44% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.VineSeg, "Hop", 5.6f, 84f, 5.5f, Grass, 0.44f)
            };

            // 65. #0389 TORTERRA
            db["Torterra"] = new[]
            {
                A("Wood Hammer", "Grass", 110, "Hiến 12% HP Reset toàn bộ hồi chiêu & nện búa gỗ", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.BloodSacrificeReset, PMDVisualAssetKind.ThornRoot, "Attack", 2.0f, 58f, 3.5f, Grass),
                A("Earth Mines", "Ground", 85, "Chôn 3 mìn đất ẩn nổ choáng khi quái bước vào", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.MudGlob, "Shoot", 1.25f, 46f, 6.0f, Grass),
                A("Tectonic Repulse", "Ground", 105, "Sóng địa chấn đẩy văng toàn bộ quái + Choáng", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.Fissure, "Strike", 2.4f, 60f, 4.2f, Grass),
                M("Continental Fissure", "Ground", 150, "Dậm nứt toác mặt đất thành 3 khe vực chữ Y + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Mudkip_EarthquakeFissure, "Hop", "Explosion.None", "Vine_Whip_2.None", 6.0f, 98f, 5.4f, Grass, 0.45f)
            };

            // 66. #0390 CHIMCHAR
            db["Chimchar"] = new[]
            {
                A("Scratch Sweep", "Normal", 55, "Quét 3 nhát móng vuốt lửa hình nón", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.36f, 26f, 2.2f, Fire),
                A("Taunt Backstab", "Dark", 70, "Dịch chuyển tức thời ra sau lưng đánh chí mạng", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.FireOrb, "Shoot", 1.0f, 40f, 5.8f, Fire),
                A("Acrobatics", "Flying", 85, "Nhảy vọt lên không né đòn rồi bổ nhào xuống", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.FireFeather, "Strike", 2.2f, 54f, 4.5f, Fire),
                A("Blaze Form", "Fire", 115, "Hóa khỉ lửa tăng 45% tốc chạy & giáp lửa + Hồi 36% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.FireOrb, "Hop", 5.6f, 86f, 4.8f, Fire, 0.36f)
            };

            // 67. #0392 INFERNAPE
            db["Infernape"] = new[]
            {
                A("Mach Punch", "Fighting", 70, "Đấm xuyên giáp tốc độ ánh sáng", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.FireFist, "Attack", 0.36f, 33f, 2.4f, Fire),
                A("Fire Clone", "Fire", 85, "Tách 2 phân thân lửa kẹp đánh phát nổ tam giác", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.FireOrb, "Shoot", 1.1f, 46f, 5.8f, Fire),
                A("Close Combat Dash", "Fighting", 110, "Lướt bất tử xuyên đội hình tung liên hoàn quyền", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.AuraFist, "Strike", 2.1f, 62f, 6.2f, Fire),
                M("Raging Dynamic Punch", "Fighting", 150, "Tung 8 cú đấm lửa liên hoàn cực đại + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Machamp_DynamicPunch, "Hop", "Close_Combat.None", "Blast_Burn.None", 5.6f, 98f, 5.0f, Fire, 0.38f)
            };

            // 68. #0393 PIPLUP
            db["Piplup"] = new[]
            {
                A("Peck Kite", "Flying", 60, "Nhảy lùi giữ khoảng cách + bắn quạt 3 giọt nước", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.BubblePrison, "Attack", 0.40f, 27f, 6.0f, Water),
                A("Bubble Bounce", "Water", 65, "Bong bóng nảy bật giữa 4 mục tiêu liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ChainBounce4, PMDVisualAssetKind.WaveCrest, "Shoot", 1.05f, 38f, 5.4f, Water),
                A("Whirlpool Trap", "Water", 85, "Tạo xoáy nước hút gom mọi quái vào tâm", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.BubblePrison, "Strike", 2.4f, 54f, 5.0f, Water),
                A("Hydro Turret", "Water", 115, "Đặt trụ vòi nước tự bắn 5 phát vào quái + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.WaveCrest, "Hop", 5.6f, 84f, 6.0f, Water, 0.40f)
            };

            // 69. #0395 EMPOLEON
            db["Empoleon"] = new[]
            {
                A("Metal Claw", "Steel", 75, "Quét 3 nhát móng vuốt thép hình nón", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.40f, 33f, 2.6f, Water),
                A("Flash Parry", "Steel", 85, "Thế phản đòn giáp thép triệt tiêu 100% & làm choáng", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.CrystalWall, "Shoot", 1.2f, 46f, 2.8f, Water),
                A("Steel Beam", "Steel", 110, "Bắn tia thép-nước dài 9m xuyên thấu 3 đợt", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Strike", 2.4f, 62f, 8.6f, Water),
                M("Emperor Hydro Cannon", "Water", 150, "Bắn song pháo thủy lực Hoàng Đế + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Blastoise_HydroCannon, "Hop", "Water_Spout_Splash.None", "Aqua_Ring.None", 5.8f, 98f, 7.2f, Water, 0.40f)
            };

            // 70. #0405 LUXRAY
            db["Luxray"] = new[]
            {
                A("Crunch Mark", "Dark", 65, "Đánh dấu con mồi nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.Bolt, "Attack", 0.40f, 30f, 4.8f, Elec),
                A("Wild Charge", "Electric", 95, "Lướt bất tử bọc điện xuyên đội hình chém gục quái", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.InvulnDashSlash, PMDVisualAssetKind.Electroweb, "Shoot", 1.15f, 48f, 5.8f, Elec),
                A("Electric Detonate", "Electric", 105, "Kích nổ ấn điện gây giật lan diện rộng", "L", PMDSkillKind.Projectile, PMDArchetypeMechanic.MarkDetonateChain, PMDVisualAssetKind.Bolt, "Strike", 2.2f, 60f, 5.8f, Elec),
                A("Volt Execute", "Electric", 145, "Kết liễu ngay quái <26% HP & Reset toàn bộ hồi chiêu", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.StormCloud, "Hop", 5.6f, 96f, 4.4f, Elec, 0.38f)
            };

            // 71. #0447 RIOLU
            db["Riolu"] = new[]
            {
                A("Quick Palm", "Fighting", 60, "Đâm chưởng khí công xuyên giáp 1 điểm", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.AuraFist, "Attack", 0.38f, 27f, 2.3f, Fight),
                A("Vacuum Kite", "Fighting", 65, "Nhảy lùi giữ khoảng cách + bắn quạt 3 sóng khí", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.BoneStaff, "Shoot", 1.0f, 38f, 6.0f, Fight),
                A("Aura Detect", "Fighting", 80, "Thế phản đòn triệt tiêu 100% & làm choáng kẻ địch", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.CrystalWall, "Strike", 2.2f, 48f, 2.8f, Fight),
                A("Copycat Rush", "Normal", 110, "Tách 2 phân thân khí công kẹp nổ tam giác + Hồi 36% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.AuraFist, "Hop", 5.5f, 84f, 5.5f, Fight, 0.36f)
            };

            // 72. #0470 LEAFEON
            db["Leafeon"] = new[]
            {
                A("Leaf Blade Sweep", "Grass", 90, "Quét 3 nhát kiếm lá hình nón tốc độ cao", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.CrossSlash, "Attack", 0.38f, 33f, 2.5f, Grass),
                A("Razor Boomerang", "Grass", 75, "Phóng 3 lá Boomerang bay đi quay về", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BoomerangReturn, PMDVisualAssetKind.LeafBlade, "Shoot", 0.95f, 42f, 6.5f, Grass),
                A("Synthesis Drain", "Grass", 85, "Dây leo hút máu 5 đợt hồi HP cho bản thân", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.VineSeg, "Strike", 2.3f, 54f, 5.5f, Grass),
                M("Verdant Petal Dance", "Grass", 135, "Xoáy bão cánh hoa rừng quanh thân + Hồi 46% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Venusaur_PetalDance, "Hop", "Synthesis_Sparkle.None", "Circle_Green_Out.None", 5.6f, 92f, 5.0f, Grass, 0.46f)
            };

            // 73. #0483 DIALGA
            db["Dialga"] = new[]
            {
                A("Time Loop", "Psychic", 75, "Đánh dấu & giật ngược quái về vị trí cũ sau 1.8s", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeLoopSnapback, PMDVisualAssetKind.ClockRing, "Attack", 0.55f, 34f, 5.8f, Water),
                A("Chrono Time Bomb", "Steel", 100, "Gắn đồng hồ tích 85% sát thương trong 2.2s rồi nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.DoomStar, "Shoot", 1.2f, 50f, 6.8f, Water),
                A("Flash Cannon 9m", "Steel", 115, "Bắn tia thép thời gian dài 9m xuyên thấu 3 đợt", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Strike", 2.5f, 66f, 8.8f, Water),
                M("Roar of Time", "Dragon", 160, "Đóng băng thời gian toàn vùng bằng đồng hồ cổ đại rồi kích nổ + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Dialga_RoarOfTime, "Hop", "Discharge.None", "Circle_Blue_Out.None", 6.2f, 108f, 5.6f, Water, 0.42f)
            };

            // 74. #0484 PALKIA
            db["Palkia"] = new[]
            {
                A("Spatial Blink", "Psychic", 80, "Xé không gian dịch chuyển tức thời ra sau lưng chém", "J", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.VoidPortal, "Attack", 0.45f, 36f, 6.5f, Psychic),
                M("Spatial Origin Pulse", "Water", 100, "Phóng 3 quả cầu không gian-nước khổng lồ", "K", PMDSkillKind.Projectile, PMDUniqueMoveId.Kyogre_OriginPulse, "Shoot", "Water_Spout_Splash.None", "Aura_Sphere.None", 1.15f, 50f, 8.4f, Psychic),
                A("Space Black Hole", "Dragon", 120, "Tạo hố đen không gian hút gom toàn bộ quái vào tâm", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.Supernova, "Strike", 2.4f, 64f, 5.8f, Psychic),
                M("Spacial Rend", "Dragon", 160, "Xé toạc không gian bằng 4 nhát trảm hồng tím liên hoàn + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Palkia_SpacialRend, "Hop", "Aura_Sphere.None", "Explosion.None", 6.0f, 108f, 5.8f, Psychic, 0.42f)
            };

            // 75. #0487 GIRATINA
            db["Giratina"] = new[]
            {
                A("Shadow Claw", "Ghost", 85, "Đâm xuyên giáp móng vuốt thế giới đảo ngược", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.SingleBurstPierce, PMDVisualAssetKind.ClawX, "Attack", 0.42f, 36f, 2.7f, Psychic),
                A("Distortion Mines", "Ghost", 90, "Đặt 3 cổng hư vô ẩn nổ choáng khi quái bước vào", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.ProximityMineTrap, PMDVisualAssetKind.VoidPortal, "Shoot", 1.25f, 48f, 6.2f, Psychic),
                A("Antimatter Clones", "Dragon", 110, "Tách 2 phân thân phản vật chất kẹp nổ tam giác", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.ShadowArrow, "Strike", 2.4f, 64f, 5.8f, Psychic),
                M("Shadow Force", "Ghost", 160, "Biến mất vào cổng hư vô rồi trồi lên từ dưới chân quái phát nổ + Hồi 42% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Giratina_ShadowForce, "Hop", "Shadow_Ball.None", "Explosion.None", 6.0f, 108f, 6.5f, Psychic, 0.42f)
            };

            // 76. #0491 DARKRAI
            db["Darkrai"] = new[]
            {
                A("Nightmare Mark", "Dark", 65, "Đánh dấu ác mộng nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.VoidPortal, "Attack", 0.40f, 32f, 5.0f, Psychic),
                A("Bad Dreams Drain", "Dark", 90, "Dây liên kết ác mộng hút máu 5 đợt hồi HP", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.ShadowArrow, "Shoot", 1.15f, 46f, 5.8f, Psychic),
                M("Night Daze", "Dark", 105, "Giải phóng 3 vòng sóng xung kích ảo giác đỏ-đen", "L", PMDSkillKind.LinearShockwaveDash, PMDUniqueMoveId.Zoroark_NightDaze, "Strike", "Shadow_Ball.None", "Explosion.None", 2.3f, 62f, 5.5f, Psychic),
                M("Dark Void", "Dark", 150, "Mở hố đen ác mộng hút chặt và gây choáng toàn bộ quái + Hồi 45% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Darkrai_DarkVoid, "Hop", "Shadow_Ball.None", "Explosion.None", 5.8f, 98f, 5.4f, Psychic, 0.45f)
            };

            // 77. #0493 ARCEUS
            db["Arceus"] = new[]
            {
                A("Multitype Form", "Normal", 90, "Hóa thân Thần Sáng Thế tăng 45% tốc chạy & giáp", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ElementalTransform, PMDVisualAssetKind.DoomStar, "Attack", 0.65f, 36f, 4.0f, Elec),
                A("Recover Sanctuary", "Normal", 90, "Thanh tẩy hồi 28% HP & tăng 38% tốc độ bay", "K", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Shoot", 1.3f, 44f, 3.8f, Elec, 0.28f),
                A("Creation Domain", "Dragon", 130, "Khai triển lãnh địa Sáng Thế (-50% hồi chiêu)", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.DomainExpansion, PMDVisualAssetKind.ClockRing, "Strike", 2.8f, 66f, 5.6f, Elec),
                M("Judgment", "Normal", 180, "Giáng 16 tia thiên thạch phán quyết đa sắc từ bầu trời + Hồi 50% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Arceus_Judgment, "Hop", "Synthesis_Sparkle.None", "Discharge.None", 6.0f, 115f, 5.8f, Elec, 0.50f)
            };

            // 78. #0571 ZOROARK
            db["Zoroark"] = new[]
            {
                A("Illusion Clones", "Dark", 75, "Tách 2 phân thân ảo ảnh lao chéo kẹp nổ tam giác", "J", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.VoidPortal, "Attack", 0.44f, 32f, 5.5f, Fight),
                A("Bitter Malice", "Ghost", 85, "Gắn bom oán hận tích 85% sát thương trong 2.2s rồi nổ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.TimeBombDelay, PMDVisualAssetKind.ShadowArrow, "Shoot", 1.15f, 46f, 6.4f, Fight),
                A("Shadow Ambush", "Dark", 95, "Dịch chuyển tức thời ra sau lưng đánh chí mạng", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.ClawX, "Strike", 2.1f, 60f, 6.4f, Fight),
                M("Night Daze", "Dark", 140, "Giải phóng 3 vòng sóng xung kích ảo giác đỏ-đen + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Zoroark_NightDaze, "Hop", "Shadow_Ball.None", "Explosion.None", 5.6f, 94f, 5.4f, Fight, 0.38f)
            };

            // 79. #0609 CHANDELURE
            db["Chandelure"] = new[]
            {
                A("Hex Mark", "Ghost", 65, "Đánh dấu linh hồn nhận thêm +40% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.MarkVulnerability, PMDVisualAssetKind.FireOrb, "Attack", 0.42f, 30f, 5.0f, Fire),
                A("Soul Turret", "Ghost", 85, "Triệu hồi đèn chùm ma tự bắn 5 cầu lửa vào quái", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.SummonAutoTurret, PMDVisualAssetKind.DoomStar, "Shoot", 1.25f, 44f, 6.4f, Fire),
                A("Inferno Pool", "Fire", 100, "Đốt mặt đất thành biển lửa xanh tím suốt 2.8s", "L", PMDSkillKind.Projectile, PMDArchetypeMechanic.GroundDoTPool, PMDVisualAssetKind.SludgePool, "Strike", 2.4f, 58f, 5.6f, Fire),
                A("Soul Burn Cataclysm", "Fire", 145, "Giáng 14 tia lửa ma trơi toàn màn hình + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.FireOrb, "Hop", 5.8f, 96f, 5.5f, Fire, 0.40f)
            };

            // 80. #0635 HYDREIGON
            db["Hydreigon"] = new[]
            {
                A("Tri Attack", "Normal", 80, "Quét 3 nhát tam đầu hình nón trước mặt", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ConeSweep3Hit, PMDVisualAssetKind.ClawX, "Attack", 0.42f, 35f, 2.7f, Psychic),
                A("Dragon Pulse", "Dragon", 90, "Tia rồng 9m xuyên thấu 3 đợt liên tiếp", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.PiercingBeam9m, PMDVisualAssetKind.SolarBeam, "Shoot", 1.15f, 48f, 8.6f, Psychic),
                A("Dark Repulse", "Dark", 105, "Sóng bóng tối đẩy văng cực mạnh + Làm choáng", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.WallSlamRepulse, PMDVisualAssetKind.VoidPortal, "Strike", 2.4f, 62f, 4.4f, Psychic),
                A("Hydra Black Hole", "Dragon", 150, "Hố đen tam đầu hút gom mọi quái vào tâm + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.GravityBlackHole, PMDVisualAssetKind.Supernova, "Hop", 6.0f, 98f, 5.5f, Psychic, 0.38f)
            };

            // 81. #0700 SYLVEON
            db["Sylveon"] = new[]
            {
                A("Draining Kiss", "Fairy", 65, "Dây ruy-băng hút máu 5 đợt hồi HP cho chủ nhân", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.LifeDrainTether, PMDVisualAssetKind.PetalBlossom, "Attack", 0.50f, 30f, 5.5f, Psychic),
                A("Hyper Voice Kite", "Fairy", 90, "Nhảy lùi thả diều + bắn quạt 3 sóng âm tiên nữ", "K", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.DoomStar, "Shoot", 1.05f, 44f, 6.5f, Psychic),
                A("Fairy Frolic", "Fairy", 95, "Thanh tẩy hồi 28% HP & tăng 38% tốc chạy", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.RegenCleanseBurst, PMDVisualAssetKind.PetalBlossom, "Strike", 2.4f, 46f, 3.5f, Psychic, 0.28f),
                M("Moonblast", "Fairy", 145, "Phóng mặt trăng hồng khổng lồ nổ tung làn sóng tiên nữ + Hồi 50% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Sylveon_Moonblast, "Hop", "Synthesis_Sparkle.None", "Explosion.None", 5.8f, 96f, 6.2f, Psychic, 0.50f)
            };

            // 82. #0724 DECIDUEYE
            db["Decidueye"] = new[]
            {
                A("Triple Arrows", "Fighting", 75, "Nhảy lùi giữ khoảng cách + bắn quạt 3 mũi tên", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.ShadowArrow, "Attack", 0.40f, 33f, 6.8f, Grass),
                M("Spirit Shackle", "Ghost", 95, "Bắn mũi tên bóng ma ghim chặt trói đứng kẻ địch", "K", PMDSkillKind.Projectile, PMDUniqueMoveId.Decidueye_SpiritShackle, "Shoot", "Shadow_Ball.None", "Energy_Ball_Hit.None", 1.0f, 48f, 8.4f, Grass),
                A("Phantom Snipe", "Ghost", 110, "Kết liễu ngay quái tầm thấp <26% HP & Reset hồi chiêu", "L", PMDSkillKind.Melee, PMDArchetypeMechanic.ExecuteLowHp, PMDVisualAssetKind.ShadowArrow, "Strike", 2.2f, 62f, 3.2f, Grass),
                A("Sinister Arrow Rain", "Ghost", 150, "Giáng 14 mũi tên bóng tối từ bầu trời toàn màn hình + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDArchetypeMechanic.ScreenCataclysm, PMDVisualAssetKind.ShadowArrow, "Hop", 5.8f, 98f, 5.6f, Grass, 0.40f)
            };

            // 83. #0778 MIMIKYU
            db["Mimikyu"] = new[]
            {
                A("Disguise Parry", "Fairy", 70, "Thế phản đòn bằng lớp áo giả triệt tiêu 100% sát thương", "J", PMDSkillKind.Melee, PMDArchetypeMechanic.ParryReflectStance, PMDVisualAssetKind.VoidPortal, "Attack", 0.50f, 32f, 2.6f, Psychic),
                A("Shadow Sneak", "Ghost", 80, "Dịch chuyển tức thời ra sau lưng đánh chí mạng", "K", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.BlinkBackstab, PMDVisualAssetKind.ClawX, "Shoot", 1.05f, 44f, 6.2f, Psychic),
                A("Play Rough", "Fairy", 95, "Nhảy vọt lên không né đòn rồi bổ nhào vồ lấy quái", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.AerialLeapSlam, PMDVisualAssetKind.ShadowArrow, "Strike", 2.3f, 60f, 4.8f, Psychic),
                M("Let's Snuggle Forever", "Fairy", 150, "Lao trùm bóng tối vồ lấy và cào xé 6 nhát liên hoàn + Hồi 40% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Mimikyu_LetsSnuggle, "Hop", "Shadow_Ball.None", "Explosion.None", 5.8f, 96f, 5.2f, Psychic, 0.40f)
            };

            // 84. #0887 DRAGAPULT
            db["Dragapult"] = new[]
            {
                A("U-Turn Kite", "Bug", 70, "Nhảy lùi giữ khoảng cách + bắn quạt 3 tia Dreepy", "J", PMDSkillKind.Projectile, PMDArchetypeMechanic.BackstepKiteVolley, PMDVisualAssetKind.ShadowArrow, "Attack", 0.38f, 32f, 6.8f, Psychic),
                M("Dragon Darts", "Dragon", 100, "Bắn cặp phi tiêu Dreepy siêu thanh tự khóa mục tiêu", "K", PMDSkillKind.Projectile, PMDUniqueMoveId.Dragapult_DragonDarts, "Shoot", "Dragon_Pulse_Ball.None", "Explosion.None", 0.90f, 48f, 8.6f, Psychic),
                A("Stealth Clones", "Ghost", 100, "Tách 2 phân thân tàng hình kẹp nổ tam giác", "L", PMDSkillKind.LinearShockwaveDash, PMDArchetypeMechanic.ShadowCloneExplode, PMDVisualAssetKind.VoidPortal, "Strike", 2.2f, 62f, 5.8f, Psychic),
                M("Phantom Force", "Ghost", 150, "Biến mất vào cổng hư vô rồi trồi lên dưới chân quái phát nổ + Hồi 38% HP", "U", PMDSkillKind.UltimateOrbitalStorm, PMDUniqueMoveId.Giratina_ShadowForce, "Hop", "Shadow_Ball.None", "Explosion.None", 5.8f, 98f, 6.5f, Psychic, 0.38f)
            };
        }

        private static PMDVisualAssetKind Dragon_Pulse_Ball_Fallback() => PMDVisualAssetKind.DoomStar;
    }
}
