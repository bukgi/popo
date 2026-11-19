using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDUniqueMoveId
    {
        None = 0,

        // #0025 Pikachu
        Pikachu_ThunderShock = 1,
        Pikachu_Electroweb = 2,
        Pikachu_VoltTackle = 3,
        Pikachu_Thunderstorm = 4,

        // #0001 Bulbasaur
        Bulbasaur_VineWhip = 5,
        Bulbasaur_RazorLeaf = 6,
        Bulbasaur_SludgeBomb = 7,
        Bulbasaur_SolarBeam = 8,

        // #0004 Charmander
        Charmander_FirePunch = 9,
        Charmander_FlameBurst = 10,
        Charmander_FireBlastKanji = 11,
        Charmander_InfernoSpin = 12,

        // #0006 Charizard
        Charizard_DragonClawX = 13,
        Charizard_AirSlash = 14,
        Charizard_SeismicSlam = 15,
        Charizard_BlastBurnVolcano = 16,

        // #0007 Squirtle
        Squirtle_SkullBash = 17,
        Squirtle_BubblePrison = 18,
        Squirtle_RapidSpin = 19,
        Squirtle_SurfTsunami = 20,

        // #0133 Eevee
        Eevee_TailWhipCombo = 21,
        Eevee_SwiftStars = 22,
        Eevee_CopycatShadowDash = 23,
        Eevee_EeveelutionResonance = 24,

        // #0151 Mew
        Mew_LightScreenWall = 25,
        Mew_TeleportAmbush = 26,
        Mew_PsychicTelekinesis = 27,
        Mew_GenesisSupernova = 28,

        // #0252 Treecko
        Treecko_LeafBladeCross = 29,
        Treecko_BulletSeedGatling = 30,
        Treecko_LeechSeedParasite = 31,
        Treecko_FrenzyPlantRoots = 32,

        // #0255 Torchic
        Torchic_PeckFlurry = 33,
        Torchic_FeatherDanceTrap = 34,
        Torchic_BlazeKickJump = 35,
        Torchic_PhoenixSkyUppercut = 36,

        // #0258 Mudkip
        Mudkip_MudSlapSwamp = 37,
        Mudkip_MudBombBounce = 38,
        Mudkip_WhirlpoolGeyser = 39,
        Mudkip_EarthquakeFissure = 40,

        // #0448 Lucario
        Lucario_BulletPunchBarrage = 41,
        Lucario_BoneRushTeleport = 42,
        Lucario_PowerUpPunch = 43,
        Lucario_AuraCannonBarrage = 44,

        // #0471 Glaceon
        Glaceon_IceShardDaggers = 45,
        Glaceon_IcicleSpearCrystals = 46,
        Glaceon_FreezeDrySpikes = 47,
        Glaceon_SnowGlobeBlizzard = 48,

        // Signature Moves (Legendaries, Starters & Fan Favorites)
        Dialga_RoarOfTime = 49,
        Palkia_SpacialRend = 50,
        Giratina_ShadowForce = 51,
        Darkrai_DarkVoid = 52,
        Arceus_Judgment = 53,
        Kyogre_OriginPulse = 54,
        Groudon_PrecipiceBlades = 55,
        Lugia_Aeroblast = 56,
        HoOh_SacredFire = 57,
        Jirachi_DoomDesire = 58,
        Decidueye_SpiritShackle = 59,
        Dragapult_DragonDarts = 60,
        Mimikyu_LetsSnuggle = 61,
        Zoroark_NightDaze = 62,
        Venusaur_PetalDance = 63,
        Blastoise_HydroCannon = 64,
        Machamp_DynamicPunch = 65,
        Sylveon_Moonblast = 66
    }

    /// <summary>
    /// Bespoke 48-Move Execution Engine: Every single move across all 12 Pokémon has its own
    /// procedural pixel-art sprites (FilterMode.Point) and unique combat behavior inspired by
    /// Pokémon UNITE, Bulbapedia, Serebii, and PokémonDB. No two moves share the same visual or mechanic!
    /// </summary>
    public static class PMDBespokeSkillEngine
    {
        private static readonly Dictionary<string, Sprite> ProceduralSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, List<Sprite>> ProceduralAnimStrips = new Dictionary<string, List<Sprite>>();

        public static bool TryExecuteUniqueMove(
            MonoBehaviour runner,
            Transform caster,
            PMDSkillData skill,
            Vector2 aimDir)
        {
            if (runner == null || caster == null || skill == null)
                return false;

            if (skill.archetype != PMDArchetypeMechanic.None)
                return PMDArchetypeCombatEngine.TryExecute(runner, caster, skill, aimDir);

            if (skill.uniqueId == PMDUniqueMoveId.None)
                return false;

            Vector2 dir = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector2.down;
            if (skill.healPercent > 0f && PMDPlayerController.Instance != null)
            {
                PMDPlayerController.Instance.Heal(Mathf.Round(PMDPlayerController.Instance.MaxHp * skill.healPercent));
            }

            SpawnSkillCastBurst(caster, skill, dir);
            runner.StartCoroutine(DispatchMoveCoroutine(caster, skill, dir));
            return true;
        }

        private static IEnumerator DispatchMoveCoroutine(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            switch (skill.uniqueId)
            {
                // ==================== 1. PIKACHU ====================
                case PMDUniqueMoveId.Pikachu_ThunderShock:
                    yield return Co_Pikachu_ThunderShock(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Pikachu_Electroweb:
                    yield return Co_Pikachu_Electroweb(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Pikachu_VoltTackle:
                    yield return Co_Pikachu_VoltTackle(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Pikachu_Thunderstorm:
                    yield return Co_Pikachu_Thunderstorm(caster, skill);
                    break;

                // ==================== 2. BULBASAUR ====================
                case PMDUniqueMoveId.Bulbasaur_VineWhip:
                    yield return Co_Bulbasaur_VineWhip(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Bulbasaur_RazorLeaf:
                    yield return Co_Bulbasaur_RazorLeaf(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Bulbasaur_SludgeBomb:
                    yield return Co_Bulbasaur_SludgeBomb(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Bulbasaur_SolarBeam:
                    yield return Co_Bulbasaur_SolarBeam(caster, skill, dir);
                    break;

                // ==================== 3. CHARMANDER ====================
                case PMDUniqueMoveId.Charmander_FirePunch:
                    yield return Co_Charmander_FirePunch(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charmander_FlameBurst:
                    yield return Co_Charmander_FlameBurst(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charmander_FireBlastKanji:
                    yield return Co_Charmander_FireBlastKanji(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charmander_InfernoSpin:
                    yield return Co_Charmander_InfernoSpin(caster, skill);
                    break;

                // ==================== 4. CHARIZARD ====================
                case PMDUniqueMoveId.Charizard_DragonClawX:
                    yield return Co_Charizard_DragonClawX(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charizard_AirSlash:
                    yield return Co_Charizard_AirSlash(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charizard_SeismicSlam:
                    yield return Co_Charizard_SeismicSlam(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Charizard_BlastBurnVolcano:
                    yield return Co_Charizard_BlastBurnVolcano(caster, skill);
                    break;

                // ==================== 5. SQUIRTLE ====================
                case PMDUniqueMoveId.Squirtle_SkullBash:
                    yield return Co_Squirtle_SkullBash(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Squirtle_BubblePrison:
                    yield return Co_Squirtle_BubblePrison(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Squirtle_RapidSpin:
                    yield return Co_Squirtle_RapidSpin(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Squirtle_SurfTsunami:
                    yield return Co_Squirtle_SurfTsunami(caster, skill, dir);
                    break;

                // ==================== 6. EEVEE ====================
                case PMDUniqueMoveId.Eevee_TailWhipCombo:
                    yield return Co_Eevee_TailWhipCombo(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Eevee_SwiftStars:
                    yield return Co_Eevee_SwiftStars(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Eevee_CopycatShadowDash:
                    yield return Co_Eevee_CopycatShadowDash(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Eevee_EeveelutionResonance:
                    yield return Co_Eevee_EeveelutionResonance(caster, skill);
                    break;

                // ==================== 7. MEW ====================
                case PMDUniqueMoveId.Mew_LightScreenWall:
                    yield return Co_Mew_LightScreenWall(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mew_TeleportAmbush:
                    yield return Co_Mew_TeleportAmbush(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mew_PsychicTelekinesis:
                    yield return Co_Mew_PsychicTelekinesis(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mew_GenesisSupernova:
                    yield return Co_Mew_GenesisSupernova(caster, skill, dir);
                    break;

                // ==================== 8. TREECKO ====================
                case PMDUniqueMoveId.Treecko_LeafBladeCross:
                    yield return Co_Treecko_LeafBladeCross(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Treecko_BulletSeedGatling:
                    yield return Co_Treecko_BulletSeedGatling(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Treecko_LeechSeedParasite:
                    yield return Co_Treecko_LeechSeedParasite(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Treecko_FrenzyPlantRoots:
                    yield return Co_Treecko_FrenzyPlantRoots(caster, skill, dir);
                    break;

                // ==================== 9. TORCHIC ====================
                case PMDUniqueMoveId.Torchic_PeckFlurry:
                    yield return Co_Torchic_PeckFlurry(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Torchic_FeatherDanceTrap:
                    yield return Co_Torchic_FeatherDanceTrap(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Torchic_BlazeKickJump:
                    yield return Co_Torchic_BlazeKickJump(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Torchic_PhoenixSkyUppercut:
                    yield return Co_Torchic_PhoenixSkyUppercut(caster, skill, dir);
                    break;

                // ==================== 10. MUDKIP ====================
                case PMDUniqueMoveId.Mudkip_MudSlapSwamp:
                    yield return Co_Mudkip_MudSlapSwamp(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mudkip_MudBombBounce:
                    yield return Co_Mudkip_MudBombBounce(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mudkip_WhirlpoolGeyser:
                    yield return Co_Mudkip_WhirlpoolGeyser(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mudkip_EarthquakeFissure:
                    yield return Co_Mudkip_EarthquakeFissure(caster, skill, dir);
                    break;

                // ==================== 11. LUCARIO ====================
                case PMDUniqueMoveId.Lucario_BulletPunchBarrage:
                    yield return Co_Lucario_BulletPunchBarrage(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Lucario_BoneRushTeleport:
                    yield return Co_Lucario_BoneRushTeleport(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Lucario_PowerUpPunch:
                    yield return Co_Lucario_PowerUpPunch(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Lucario_AuraCannonBarrage:
                    yield return Co_Lucario_AuraCannonBarrage(caster, skill, dir);
                    break;

                // ==================== 12. GLACEON ====================
                case PMDUniqueMoveId.Glaceon_IceShardDaggers:
                    yield return Co_Glaceon_IceShardDaggers(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Glaceon_IcicleSpearCrystals:
                    yield return Co_Glaceon_IcicleSpearCrystals(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Glaceon_FreezeDrySpikes:
                    yield return Co_Glaceon_FreezeDrySpikes(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Glaceon_SnowGlobeBlizzard:
                    yield return Co_Glaceon_SnowGlobeBlizzard(caster, skill);
                    break;

                // ==================== SIGNATURE LEGENDARY & ICONIC MOVES ====================
                case PMDUniqueMoveId.Dialga_RoarOfTime:
                    yield return Co_Dialga_RoarOfTime(caster, skill);
                    break;
                case PMDUniqueMoveId.Palkia_SpacialRend:
                    yield return Co_Palkia_SpacialRend(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Giratina_ShadowForce:
                    yield return Co_Giratina_ShadowForce(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Darkrai_DarkVoid:
                    yield return Co_Darkrai_DarkVoid(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Arceus_Judgment:
                    yield return Co_Arceus_Judgment(caster, skill);
                    break;
                case PMDUniqueMoveId.Kyogre_OriginPulse:
                    yield return Co_Kyogre_OriginPulse(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Groudon_PrecipiceBlades:
                    yield return Co_Groudon_PrecipiceBlades(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Lugia_Aeroblast:
                    yield return Co_Lugia_Aeroblast(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.HoOh_SacredFire:
                    yield return Co_HoOh_SacredFire(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Jirachi_DoomDesire:
                    yield return Co_Jirachi_DoomDesire(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Decidueye_SpiritShackle:
                    yield return Co_Decidueye_SpiritShackle(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Dragapult_DragonDarts:
                    yield return Co_Dragapult_DragonDarts(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Mimikyu_LetsSnuggle:
                    yield return Co_Mimikyu_LetsSnuggle(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Zoroark_NightDaze:
                    yield return Co_Zoroark_NightDaze(caster, skill);
                    break;
                case PMDUniqueMoveId.Venusaur_PetalDance:
                    yield return Co_Venusaur_PetalDance(caster, skill);
                    break;
                case PMDUniqueMoveId.Blastoise_HydroCannon:
                    yield return Co_Blastoise_HydroCannon(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Machamp_DynamicPunch:
                    yield return Co_Machamp_DynamicPunch(caster, skill, dir);
                    break;
                case PMDUniqueMoveId.Sylveon_Moonblast:
                    yield return Co_Sylveon_Moonblast(caster, skill, dir);
                    break;
            }
        }

        // =========================================================================================
        // 1. PIKACHU (Sét Nhảy Zigzag | Lưới Điện Electroweb | Volt Tackle Hất Tung | Mây Giông Thunderstorm)
        // =========================================================================================
        private static IEnumerator Co_Pikachu_ThunderShock(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 prev = caster.position + new Vector3(0f, 0.25f, 0f);
            var visited = new HashSet<PMDWildPokemonAI>();

            for (int jump = 0; jump < 4; jump++)
            {
                PMDWildPokemonAI next = FindNearestWild(prev, 4.2f, visited);
                Vector3 nextPos = next != null ? next.transform.position : prev + (Vector3)(dir * 1.8f);

                // Draw pixel jagged lightning segments between prev and nextPos
                int segs = 5;
                for (int s = 1; s <= segs; s++)
                {
                    Vector3 p = Vector3.Lerp(prev, nextPos, s / (float)segs);
                    p += (Vector3)(Random.insideUnitCircle * 0.14f);
                    SpawnPixelVisual(GetBoltSprite(), p, 1.15f, 0.22f, Random.Range(0f, 360f));
                }

                if (next != null)
                {
                    visited.Add(next);
                    next.TakeDamage(skill.damage * Mathf.Pow(0.85f, jump), dir, skill.elementColor);
                    PMDVfxPlayer.SpawnVfx("Spark.None", nextPos, 1.1f, 30f);
                    prev = nextPos;
                }
                else
                {
                    HitEnemiesAt(nextPos, 1.4f, skill.damage, dir, skill);
                    break;
                }
                yield return new WaitForSeconds(0.06f);
            }
        }

        private static IEnumerator Co_Pikachu_Electroweb(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 webPos = caster.position + (Vector3)(dir * 3.0f);
            yield return FlyPixelObject(GetElectricWebSprite(), caster.position, webPos, 0.22f, 0.9f, 540f);

            // Deploy expanding electric cobweb trap on ground for 2.2s
            GameObject webGo = SpawnPixelVisual(GetElectricWebSprite(), webPos, 2.3f, 2.2f, 0f, -10);
            for (int t = 0; t < 7; t++)
            {
                if (webGo != null)
                    webGo.transform.localScale = Vector3.one * (2.2f + Mathf.Sin(t * 1.4f) * 0.2f);

                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var w = allWild[i];
                    if (w == null || w.IsFainted || w.IsRecruited) continue;
                    if (Vector2.Distance(webPos, w.transform.position) <= 2.2f)
                    {
                        // Snare/pull slightly toward web center & shock
                        w.transform.position = Vector2.MoveTowards(w.transform.position, webPos, 0.35f);
                        w.TakeDamage(skill.damage * 0.26f, Vector2.zero, skill.elementColor);
                        SpawnPixelVisual(GetBoltSprite(), w.transform.position, 0.9f, 0.15f, t * 45f);
                    }
                }
                yield return new WaitForSeconds(0.28f);
            }
        }

        private static IEnumerator Co_Pikachu_VoltTackle(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            PMDCameraFollow.Shake(0.22f, 0.20f);
            for (int i = 0; i < 7; i++)
            {
                if (caster == null) yield break;
                MoveCasterSafe(caster, dir * 0.78f);
                SpawnPixelVisual(GetBoltSprite(), caster.position + (Vector3)(Random.insideUnitCircle * 0.45f), 1.4f, 0.22f, i * 60f);
                HitEnemiesAt(caster.position, 1.45f, skill.damage * 0.35f, Vector2.up * 1.6f, skill);
                yield return new WaitForSeconds(0.032f);
            }
            if (caster != null)
                PMDVfxPlayer.SpawnVfx("Discharge.None", caster.position, 1.6f, 28f);
        }

        private static IEnumerator Co_Pikachu_Thunderstorm(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            // Summon storm clouds hovering above caster for 2.6 seconds, striking nearby enemies automatically
            for (int pulse = 0; pulse < 9; pulse++)
            {
                if (caster == null) yield break;
                Vector3 center = caster.position;
                for (int c = 0; c < 3; c++)
                {
                    float a = (pulse * 35f + c * 120f) * Mathf.Deg2Rad;
                    Vector3 cloudPos = center + new Vector3(Mathf.Cos(a) * 1.4f, 1.65f + Mathf.Sin(a) * 0.4f, 0f);
                    SpawnPixelVisual(GetStormCloudSprite(), cloudPos, 1.5f, 0.30f, 0f, 120);
                }

                PMDWildPokemonAI target = FindNearestWild(center, 4.6f, null);
                Vector3 strikePos = target != null ? target.transform.position : center + (Vector3)(Random.insideUnitCircle * 2.8f);
                for (int b = 0; b < 4; b++)
                {
                    SpawnPixelVisual(GetBoltSprite(), strikePos + new Vector3(0f, b * 0.45f, 0f), 1.4f, 0.18f, (b % 2 == 0) ? 15f : -15f, 115);
                }
                PMDVfxPlayer.SpawnVfx("Discharge.None", strikePos, 1.25f, 28f);
                HitEnemiesAt(strikePos, 1.6f, skill.damage * 0.28f, Vector2.down * 0.3f, skill);
                yield return new WaitForSeconds(0.24f);
            }
        }

        // =========================================================================================
        // 2. BULBASAUR (2 Dây Leo Kéo Gom | Lá Boomerang | Đầm Lầy Độc | Tia Laser Mặt Trời)
        // =========================================================================================
        private static IEnumerator Co_Bulbasaur_VineWhip(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Extend 2 long segmented green pixel vines left & right that snap together in front
            for (int step = 0; step < 5; step++)
            {
                if (caster == null) yield break;
                float closeFactor = 1f - (step / 4f);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int seg = 1; seg <= 6; seg++)
                    {
                        float dist = seg * 0.48f;
                        Vector2 pos = (Vector2)caster.position + dir * dist + perp * (side * closeFactor * (dist * 0.45f));
                        SpawnPixelVisual(GetVineSegmentSprite(), pos, 1.2f, 0.08f, baseAngle);
                    }
                }
                yield return new WaitForSeconds(0.035f);
            }

            if (caster != null)
            {
                Vector3 gatherSpot = caster.position + (Vector3)(dir * 2.2f);
                PMDVfxPlayer.SpawnVfx("Vine_Whip_2.None", gatherSpot, 1.5f, 28f);
                HitAndPullEnemiesTo(gatherSpot, 2.5f, skill.damage, gatherSpot, 0.8f, skill);
            }
        }

        private static IEnumerator Co_Bulbasaur_RazorLeaf(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            float[] spread = { -28f, 0f, 28f };
            for (int i = 0; i < spread.Length; i++)
            {
                Vector2 leafDir = PMDProjectile.RotateVector2(dir, spread[i]);
                if (caster != null)
                    PMDPlayerController.Instance.StartCoroutine(RunBoomerangLeaf(caster, leafDir, skill));
            }
            yield return null;
        }

        private static IEnumerator RunBoomerangLeaf(Transform caster, Vector2 dir, PMDSkillData skill)
        {
            Vector3 start = caster != null ? caster.position : Vector3.zero;
            Vector3 apex = start + (Vector3)(dir * 5.2f);
            var go = SpawnPixelVisual(GetLeafBladeSprite(), start, 1.35f, 1.2f, 0f);
            float t = 0f;
            while (t < 1f && go != null)
            {
                t += Time.deltaTime * 1.45f;
                Vector3 returnTarget = caster != null ? caster.position : start;
                Vector3 pos = t < 0.5f
                    ? Vector3.Lerp(start, apex, t * 2f)
                    : Vector3.Lerp(apex, returnTarget, (t - 0.5f) * 2f);
                go.transform.position = pos;
                go.transform.Rotate(0f, 0f, 920f * Time.deltaTime);
                HitEnemiesAt(pos, 0.85f, skill.damage * 0.18f, dir, skill);
                yield return null;
            }
        }

        private static IEnumerator Co_Bulbasaur_SludgeBomb(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 poolPos = caster.position + (Vector3)(dir * 3.4f);
            yield return FlyPixelObject(GetSludgeGlobSprite(), caster.position, poolPos, 0.26f, 1.3f, 360f);

            // Lingering bubbling toxic swamp pool for 3.0s
            SpawnPixelVisual(GetSludgePoolSprite(), poolPos, 2.5f, 3.0f, 0f, -15);
            for (int tick = 0; tick < 8; tick++)
            {
                Vector3 bubblePos = poolPos + (Vector3)(Random.insideUnitCircle * 1.2f);
                SpawnPixelVisual(GetSludgeGlobSprite(), bubblePos, 0.85f, 0.25f, 0f);
                HitEnemiesAt(poolPos, 2.1f, skill.damage * 0.24f, Vector2.zero, skill);
                yield return new WaitForSeconds(0.34f);
            }
        }

        private static IEnumerator Co_Bulbasaur_SolarBeam(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.32f, 0.25f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            for (int wave = 0; wave < 4; wave++)
            {
                if (caster == null) yield break;
                Vector3 origin = caster.position + new Vector3(0f, 0.25f, 0f);
                for (int seg = 1; seg <= 14; seg++)
                {
                    Vector3 p = origin + (Vector3)(dir * (seg * 0.62f));
                    SpawnPixelVisual(GetSolarBeamSegmentSprite(), p, 1.65f, 0.12f, angle, 110);
                    HitEnemiesAt(p, 1.15f, skill.damage * 0.22f, dir, skill);
                }
                yield return new WaitForSeconds(0.08f);
            }
        }

        // =========================================================================================
        // 3. CHARMANDER (Hỏa Quyền | Pháo Hoa Lửa 8 Tia | Chữ ĐẠI 大 Lửa | 4 Hỏa Cầu Xoay Quanh)
        // =========================================================================================
        private static IEnumerator Co_Charmander_FirePunch(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector3 start = caster.position + (Vector3)(dir * 0.6f);
            Vector3 end = caster.position + (Vector3)(dir * 2.2f);
            yield return FlyPixelObject(GetFireFistSprite(), start, end, 0.14f, 1.75f, angle, false);
            PMDVfxPlayer.SpawnVfx("Blast_Burn.None", end, 1.5f, 28f);
            HitEnemiesAt(end, 1.85f, skill.damage, dir * 2.8f, skill);
        }

        private static IEnumerator Co_Charmander_FlameBurst(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 burstCenter = caster.position + (Vector3)(dir * 3.6f);
            yield return FlyPixelObject(GetFireOrbSprite(), caster.position, burstCenter, 0.24f, 1.45f, 480f);

            PMDVfxPlayer.SpawnVfx("Explosion.None", burstCenter, 1.4f, 28f);
            HitEnemiesAt(burstCenter, 1.6f, skill.damage * 0.6f, dir, skill);

            // Split into 8 outward fire sparks
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkDir = PMDProjectile.RotateVector2(Vector2.right, i * 45f);
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetFireOrbSprite(), burstCenter, burstCenter + (Vector3)(sparkDir * 2.2f), 0.24f, skill, sparkDir));
            }
        }

        private static IEnumerator Co_Charmander_FireBlastKanji(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 kanjiCenter = caster.position + (Vector3)(dir * 3.2f);
            yield return FlyPixelObject(GetFireOrbSprite(), caster.position, kanjiCenter, 0.22f, 1.5f, 0f);

            PMDCameraFollow.Shake(0.28f, 0.22f);
            // Stamp the iconic Japanese 大 (Dai-Monji) Fire Blast Kanji formed by fire pillars!
            Vector2[] kanjiOffsets =
            {
                new Vector2(0f, 0f),
                new Vector2(-1.4f, 0.5f), new Vector2(-0.7f, 0.5f), new Vector2(0.7f, 0.5f), new Vector2(1.4f, 0.5f), // Horizontal bar 一
                new Vector2(0f, 1.5f), new Vector2(0f, 0.8f), // Top vertical
                new Vector2(-0.6f, -0.6f), new Vector2(-1.3f, -1.3f), // Left leg 丿
                new Vector2(0.6f, -0.6f), new Vector2(1.3f, -1.3f)    // Right leg 乀
            };

            for (int tick = 0; tick < 5; tick++)
            {
                for (int i = 0; i < kanjiOffsets.Length; i++)
                {
                    Vector3 p = kanjiCenter + (Vector3)kanjiOffsets[i];
                    SpawnPixelVisual(GetFireOrbSprite(), p, 1.35f, 0.22f, tick * 30f);
                    if (tick % 2 == 0)
                        PMDVfxPlayer.SpawnVfx("Ember.None", p, 1.1f, 30f);
                    HitEnemiesAt(p, 0.9f, skill.damage * 0.22f, Vector2.zero, skill);
                }
                yield return new WaitForSeconds(0.22f);
            }
        }

        private static IEnumerator Co_Charmander_InfernoSpin(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            const int orbs = 4;
            var orbGos = new GameObject[orbs];
            for (int i = 0; i < orbs; i++)
                orbGos[i] = SpawnPixelVisual(GetFireOrbSprite(), caster.position, 1.6f, 2.2f, 0f);

            float elapsed = 0f;
            while (elapsed < 2.0f && caster != null)
            {
                elapsed += Time.deltaTime;
                float radius = 1.1f + Mathf.Sin((elapsed / 2.0f) * Mathf.PI) * 2.2f;
                for (int i = 0; i < orbs; i++)
                {
                    if (orbGos[i] == null) continue;
                    float a = elapsed * 7.5f + (i * Mathf.PI * 2f / orbs);
                    Vector3 pos = caster.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                    orbGos[i].transform.position = pos;
                    orbGos[i].transform.Rotate(0f, 0f, 540f * Time.deltaTime);
                    HitEnemiesAt(pos, 0.95f, skill.damage * 0.08f, Vector2.zero, skill);
                }
                yield return null;
            }
        }

        // =========================================================================================
        // 4. CHARIZARD (Dấu X Vuốt Rồng | 3 Đao Gió Air Slash | Không Kích Seismic Slam | 12 Cột Núi Lửa)
        // =========================================================================================
        private static IEnumerator Co_Charizard_DragonClawX(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            Vector3 center = caster.position + (Vector3)(dir * 1.65f);
            SpawnPixelVisual(GetClawXSprite(), center, 2.1f, 0.35f, 0f);
            PMDVfxPlayer.SpawnVfx("X_Scissor.None", center, 1.65f, 28f);
            HitEnemiesAt(center, 2.2f, skill.damage, dir * 1.8f, skill);
            yield return new WaitForSeconds(0.12f);
        }

        private static IEnumerator Co_Charizard_AirSlash(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            for (int i = 0; i < 3; i++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Projectile);
                Vector2 bladeDir = PMDProjectile.RotateVector2(dir, (i - 1) * 16f);
                float ang = Mathf.Atan2(bladeDir.y, bladeDir.x) * Mathf.Rad2Deg;
                PMDPlayerController.Instance.StartCoroutine(
                    FlyAndHitSpark(GetWindCrescentSprite(), caster.position, caster.position + (Vector3)(bladeDir * 7.0f), 0.38f, skill, bladeDir, ang, 1.6f));
                yield return new WaitForSeconds(0.09f);
            }
        }

        private static IEnumerator Co_Charizard_SeismicSlam(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 2.5f), 4.0f, null);
            Vector3 slamCenter = target != null ? target.transform.position : caster.position + (Vector3)(dir * 3.5f);

            // Fly high into the sky with target, then slam down violently!
            Vector3 startPos = caster.position;
            float t = 0f;
            while (t < 1f && caster != null)
            {
                t += Time.deltaTime * 2.4f;
                float arcY = Mathf.Sin(t * Mathf.PI) * 2.8f;
                Vector3 groundLerp = Vector3.Lerp(startPos, slamCenter, t);
                caster.position = groundLerp + new Vector3(0f, arcY, 0f);
                if (target != null && !target.IsFainted)
                    target.transform.position = caster.position + new Vector3(0f, -0.25f, 0f);
                yield return null;
            }

            if (caster != null)
            {
                caster.position = slamCenter;
                PMDCameraFollow.Shake(0.38f, 0.32f);
                PMDVfxPlayer.SpawnVfx("Eruption.None", slamCenter, 1.9f, 26f);
                PMDVfxPlayer.SpawnVfx("Circle_Red_Out.None", slamCenter, 1.8f, 26f);
                HitEnemiesAt(slamCenter, 2.6f, skill.damage, dir, skill);
            }
        }

        private static IEnumerator Co_Charizard_BlastBurnVolcano(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.42f, 0.34f);
            Vector3 origin = caster.position;
            for (int geyser = 0; geyser < 12; geyser++)
            {
                Vector3 pos = origin + (Vector3)(Random.insideUnitCircle * 4.2f);
                PMDVfxPlayer.SpawnVfx("Eruption.None", pos, 1.55f, 26f);
                SpawnPixelVisual(GetFireOrbSprite(), pos, 1.6f, 0.35f, geyser * 30f);
                HitEnemiesAt(pos, 1.75f, skill.damage * 0.32f, Vector2.up, skill);
                yield return new WaitForSeconds(0.09f);
            }
        }

        // =========================================================================================
        // 5. SQUIRTLE (Húc Mai Rùa | Bong Bóng Nhốt Quái Lơ Lửng | Xoay Mai Rapid Spin | Cưỡi Sóng Surf)
        // =========================================================================================
        private static IEnumerator Co_Squirtle_SkullBash(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            MoveCasterSafe(caster, dir * 1.8f);
            SpawnPixelVisual(GetBubblePrisonSprite(), caster.position, 2.0f, 0.65f, 0f);
            HitEnemiesAt(caster.position + (Vector3)(dir * 0.6f), 2.0f, skill.damage, dir * 2.5f, skill);
            yield return null;
        }

        private static IEnumerator Co_Squirtle_BubblePrison(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 targetSpot = caster.position + (Vector3)(dir * 3.2f);
            yield return FlyPixelObject(GetBubblePrisonSprite(), caster.position, targetSpot, 0.24f, 1.6f, 0f);

            // Trap nearby monsters inside floating water bubbles lifted into the air!
            var trapped = new List<(PMDWildPokemonAI mon, Vector3 basePos, GameObject bubble)>();
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(targetSpot, w.transform.position) <= 2.3f)
                {
                    var bGo = SpawnPixelVisual(GetBubblePrisonSprite(), w.transform.position, 2.1f, 1.15f, 0f, 120);
                    trapped.Add((w, w.transform.position, bGo));
                }
            }

            float elapsed = 0f;
            while (elapsed < 1.1f)
            {
                elapsed += Time.deltaTime;
                float lift = Mathf.Sin((elapsed / 1.1f) * Mathf.PI) * 1.15f;
                for (int i = 0; i < trapped.Count; i++)
                {
                    var item = trapped[i];
                    if (item.mon != null && !item.mon.IsFainted)
                    {
                        Vector3 lifted = item.basePos + new Vector3(0f, lift, 0f);
                        item.mon.transform.position = lifted;
                        if (item.bubble != null)
                            item.bubble.transform.position = lifted;
                    }
                }
                yield return null;
            }

            for (int i = 0; i < trapped.Count; i++)
            {
                var item = trapped[i];
                if (item.mon != null && !item.mon.IsFainted)
                {
                    item.mon.transform.position = item.basePos;
                    PMDVfxPlayer.SpawnVfx("Water_Spout_Splash.None", item.basePos, 1.4f, 28f);
                    item.mon.TakeDamage(skill.damage, Vector2.zero, skill.elementColor);
                }
            }
        }

        private static IEnumerator Co_Squirtle_RapidSpin(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            // Spin rapidly for 1.3s while firing a 360-degree spiral of water jets!
            for (int step = 0; step < 12; step++)
            {
                if (caster == null) yield break;
                MoveCasterSafe(caster, dir * 0.38f);
                caster.localRotation = Quaternion.Euler(0f, 0f, step * 90f);

                for (int jet = 0; jet < 2; jet++)
                {
                    float a = (step * 45f + jet * 180f) * Mathf.Deg2Rad;
                    Vector3 jetPos = caster.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.35f;
                    PMDVfxPlayer.SpawnVfx("Bubbles_Blue.None", jetPos, 1.15f, 32f);
                }
                HitEnemiesAt(caster.position, 1.85f, skill.damage * 0.16f, dir, skill);
                yield return new WaitForSeconds(0.065f);
            }
            if (caster != null)
                caster.localRotation = Quaternion.identity;
        }

        private static IEnumerator Co_Squirtle_SurfTsunami(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.30f, 0.22f);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Ride a massive cresting wave forward, dragging enemies along with the wave!
            for (int step = 0; step < 8; step++)
            {
                if (caster == null) yield break;
                MoveCasterSafe(caster, dir * 0.65f);
                for (int w = -2; w <= 2; w++)
                {
                    Vector3 waveTile = caster.position + (Vector3)(dir * 0.6f + perp * (w * 0.72f));
                    SpawnPixelVisual(GetWaveCrestSprite(), waveTile, 1.5f, 0.14f, angle);
                    HitAndPullEnemiesTo(waveTile, 1.1f, skill.damage * 0.22f, waveTile + (Vector3)(dir * 0.7f), 0.7f, skill);
                }
                yield return new WaitForSeconds(0.065f);
            }
        }

        // =========================================================================================
        // 6. EEVEE (Vẫy Đuôi 4 Nhịp | 5 Sao Swift Tự Khóa | 2 Phân Thân Tam Giác | Tứ Hệ Tiến Hóa)
        // =========================================================================================
        private static IEnumerator Co_Eevee_TailWhipCombo(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            for (int i = 0; i < 4; i++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Melee, 0.75f);
                Vector2 swing = PMDProjectile.RotateVector2(dir, -45f + i * 30f);
                Vector3 p = caster.position + (Vector3)(swing * 1.25f);
                PMDVfxPlayer.SpawnVfx("Slash.None", p, 1.2f, 32f);
                HitEnemiesAt(p, 1.4f, skill.damage * 0.35f, swing, skill);
                yield return new WaitForSeconds(0.055f);
            }
        }

        private static IEnumerator Co_Eevee_SwiftStars(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            const int stars = 5;
            var starGos = new GameObject[stars];
            for (int i = 0; i < stars; i++)
                starGos[i] = SpawnPixelVisual(GetGoldStarSprite(), caster.position, 1.35f, 1.6f, 0f);

            // Orbit around caster briefly then fire one-by-one homing into enemies!
            float orbitTime = 0.32f;
            float elapsed = 0f;
            while (elapsed < orbitTime && caster != null)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < stars; i++)
                {
                    if (starGos[i] == null) continue;
                    float a = elapsed * 10f + (i * Mathf.PI * 2f / stars);
                    starGos[i].transform.position = caster.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.15f;
                }
                yield return null;
            }

            for (int i = 0; i < stars; i++)
            {
                if (starGos[i] != null)
                    Object.Destroy(starGos[i]);
                if (caster == null) yield break;

                PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 2.5f), 6.5f, null);
                Vector3 dest = target != null ? target.transform.position : caster.position + (Vector3)(PMDProjectile.RotateVector2(dir, (i - 2) * 14f) * 5.5f);
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetGoldStarSprite(), caster.position, dest, 0.24f, skill, dir, 0f, 1.35f));
                yield return new WaitForSeconds(0.06f);
            }
        }

        private static IEnumerator Co_Eevee_CopycatShadowDash(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            Vector3 start = caster.position;
            Vector3 apex = start + (Vector3)(dir * 4.2f);
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // 2 Golden Shadow Clones dash along left & right flanks converging on apex!
            PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetGoldStarSprite(), start + (Vector3)(perp * 1.6f), apex, 0.22f, skill, dir, 0f, 1.6f));
            PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetGoldStarSprite(), start - (Vector3)(perp * 1.6f), apex, 0.22f, skill, dir, 0f, 1.6f));
            MoveCasterSafe(caster, dir * 3.8f);
            PMDVfxPlayer.SpawnVfx("Close_Combat.None", apex, 1.55f, 28f);
            HitEnemiesAt(apex, 2.0f, skill.damage, dir, skill);
            yield return null;
        }

        private static IEnumerator Co_Eevee_EeveelutionResonance(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            // Summon 4 Eeveelution Elemental Orbs (Flareon Fire, Vaporeon Water, Jolteon Electric, Leafeon Grass)
            string[] vfx = { "Ember.None", "Bubbles_Blue.None", "Spark.None", "Energy_Ball.None" };
            for (int wave = 0; wave < 6; wave++)
            {
                if (caster == null) yield break;
                for (int e = 0; e < 4; e++)
                {
                    float a = (wave * 30f + e * 90f) * Mathf.Deg2Rad;
                    Vector2 outDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector3 pos = caster.position + (Vector3)(outDir * (1.2f + wave * 0.55f));
                    PMDVfxPlayer.SpawnVfx(vfx[e], pos, 1.45f, 28f);
                    HitEnemiesAt(pos, 1.35f, skill.damage * 0.24f, outDir, skill);
                }
                yield return new WaitForSeconds(0.11f);
            }
        }

        // =========================================================================================
        // 7. MEW (Tường Pha Lê Light Screen | Dịch Chuyển Đánh Úp | Ngoại Cảm Nhấc Bổng | Siêu Tân Tinh)
        // =========================================================================================
        private static IEnumerator Co_Mew_LightScreenWall(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Gate);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Vector3 wallStart = caster.position + (Vector3)(dir * 1.2f);

            // Spawn crystal psychic barrier wall then shove it forward!
            var wallGo = SpawnPixelVisual(GetCrystalWallSprite(), wallStart, 2.2f, 0.85f, angle);
            yield return new WaitForSeconds(0.18f);

            float t = 0f;
            while (t < 1f && wallGo != null)
            {
                t += Time.deltaTime * 2.5f;
                Vector3 pos = wallStart + (Vector3)(dir * (t * 3.8f));
                wallGo.transform.position = pos;
                HitAndPullEnemiesTo(pos, 1.75f, skill.damage * 0.25f, pos + (Vector3)(dir * 0.8f), 0.8f, skill);
                yield return null;
            }
        }

        private static IEnumerator Co_Mew_TeleportAmbush(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            PMDVfxPlayer.SpawnVfx("Circle_Blue_Out.None", caster.position, 1.4f, 28f);

            PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 3.5f), 5.5f, null);
            Vector3 ambushPos = target != null
                ? target.transform.position + (Vector3)(dir * 0.7f)
                : caster.position + (Vector3)(dir * 3.6f);

            caster.position = ambushPos;
            PMDVfxPlayer.SpawnVfx("Shadow_Ball.None", ambushPos, 1.65f, 28f);
            HitEnemiesAt(ambushPos, 2.2f, skill.damage, -dir, skill);
            yield return null;
        }

        private static IEnumerator Co_Mew_PsychicTelekinesis(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 zone = caster.position + (Vector3)(dir * 3.2f);
            PMDVfxPlayer.SpawnVfx("Aura_Sphere.None", zone, 1.8f, 26f);

            // Levitate all enemies in zone high up, then slam them down!
            var liftedList = new List<(PMDWildPokemonAI mon, Vector3 ground)>();
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(zone, w.transform.position) <= 2.8f)
                    liftedList.Add((w, w.transform.position));
            }

            for (int step = 0; step < 12; step++)
            {
                float h = step < 8 ? (step / 8f) * 1.45f : (1f - (step - 8) / 4f) * 1.45f;
                for (int i = 0; i < liftedList.Count; i++)
                {
                    if (liftedList[i].mon != null && !liftedList[i].mon.IsFainted)
                        liftedList[i].mon.transform.position = liftedList[i].ground + new Vector3(0f, h, 0f);
                }
                yield return new WaitForSeconds(0.045f);
            }

            PMDCameraFollow.Shake(0.32f, 0.25f);
            for (int i = 0; i < liftedList.Count; i++)
            {
                var item = liftedList[i];
                if (item.mon != null && !item.mon.IsFainted)
                {
                    item.mon.transform.position = item.ground;
                    PMDVfxPlayer.SpawnVfx("Explosion.None", item.ground, 1.35f, 28f);
                    item.mon.TakeDamage(skill.damage, Vector2.zero, skill.elementColor);
                }
            }
        }

        private static IEnumerator Co_Mew_GenesisSupernova(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 core = caster.position + (Vector3)(dir * 2.8f);
            var holeGo = SpawnPixelVisual(GetSupernovaOrbSprite(), core, 2.6f, 1.4f, 0f, 130);

            // Pull enemies into cosmic core then detonate 12 psychic rays outward!
            for (int p = 0; p < 5; p++)
            {
                if (holeGo != null)
                    holeGo.transform.Rotate(0f, 0f, 75f);
                HitAndPullEnemiesTo(core, 3.8f, skill.damage * 0.14f, core, 0.65f, skill);
                yield return new WaitForSeconds(0.16f);
            }

            PMDCameraFollow.Shake(0.45f, 0.35f);
            PMDVfxPlayer.SpawnVfx("Explosion.None", core, 2.2f, 24f);
            for (int r = 0; r < 12; r++)
            {
                Vector2 rayDir = PMDProjectile.RotateVector2(Vector2.right, r * 30f);
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetSupernovaOrbSprite(), core, core + (Vector3)(rayDir * 3.8f), 0.26f, skill, rayDir, 0f, 1.25f));
            }
        }

        // =========================================================================================
        // 8. TREECKO (Chém Chữ Thập + | Súng Máy 10 Hạt Giống | Hạt Ký Sinh Hút Máu | Rễ Cây Đại Ngàn)
        // =========================================================================================
        private static IEnumerator Co_Treecko_LeafBladeCross(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            Vector3 center = caster.position + (Vector3)(dir * 1.55f);
            SpawnPixelVisual(GetCrossSlashSprite(), center, 2.15f, 0.32f, 0f);
            HitEnemiesAt(center, 2.2f, skill.damage, dir, skill);
            yield return null;
        }

        private static IEnumerator Co_Treecko_BulletSeedGatling(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            // Rapid-fire 10 golden bullet seeds like a machine gun!
            for (int s = 0; s < 10; s++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Projectile, 0.6f);
                Vector2 jitterDir = PMDProjectile.RotateVector2(dir, Random.Range(-7f, 7f));
                Vector3 start = caster.position + new Vector3(0f, 0.2f, 0f);
                PMDPlayerController.Instance.StartCoroutine(
                    FlyAndHitSpark(GetBulletSeedSprite(), start, start + (Vector3)(jitterDir * 6.8f), 0.25f, skill, jitterDir, 0f, 1.15f, 0.22f));
                yield return new WaitForSeconds(0.055f);
            }
        }

        private static IEnumerator Co_Treecko_LeechSeedParasite(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 targetPos = caster.position + (Vector3)(dir * 3.2f);
            yield return FlyPixelObject(GetBulletSeedSprite(), caster.position, targetPos, 0.22f, 1.4f, 360f);

            var infected = new List<PMDWildPokemonAI>();
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(targetPos, w.transform.position) <= 2.6f)
                    infected.Add(w);
            }

            // Drain HP 5 times from infected monsters and fly green life orbs back to the player!
            for (int pulse = 0; pulse < 5; pulse++)
            {
                for (int i = 0; i < infected.Count; i++)
                {
                    var w = infected[i];
                    if (w == null || w.IsFainted) continue;
                    float dmg = skill.damage * 0.26f;
                    w.TakeDamage(dmg, Vector2.zero, skill.elementColor);
                    if (caster != null)
                        PMDPlayerController.Instance.StartCoroutine(FlyPixelObject(GetVineSegmentSprite(), w.transform.position, caster.position, 0.22f, 1.1f, 0f));
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.Heal(Mathf.Max(3f, dmg * 0.45f));
                }
                yield return new WaitForSeconds(0.38f);
            }
        }

        private static IEnumerator Co_Treecko_FrenzyPlantRoots(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.34f, 0.26f);
            Vector3 cur = caster.position;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            for (int r = 1; r <= 8; r++)
            {
                float zig = (r % 2 == 0 ? 1f : -1f) * 0.65f;
                Vector3 rootPos = cur + (Vector3)(dir * (r * 0.85f) + perp * zig);
                SpawnPixelVisual(GetThornRootSprite(), rootPos, 1.85f, 0.65f, 0f);
                PMDVfxPlayer.SpawnVfx("Vine_Whip_2.None", rootPos, 1.35f, 28f);
                HitEnemiesAt(rootPos, 1.45f, skill.damage * 0.34f, Vector2.up, skill);
                yield return new WaitForSeconds(0.075f);
            }
        }

        // =========================================================================================
        // 9. TORCHIC (Mổ 5 Nhịp | Bẫy 6 Lông Vũ Lửa | Phi Thân Cước Blaze Kick | Phượng Hoàng Lửa)
        // =========================================================================================
        private static IEnumerator Co_Torchic_PeckFlurry(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            for (int p = 0; p < 5; p++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Melee, 0.7f);
                Vector3 pt = caster.position + (Vector3)(dir * 1.2f) + (Vector3)(Random.insideUnitCircle * 0.35f);
                SpawnPixelVisual(GetFeatherSprite(), pt, 1.3f, 0.16f, p * 72f);
                HitEnemiesAt(pt, 1.2f, skill.damage * 0.28f, dir, skill);
                yield return new WaitForSeconds(0.045f);
            }
        }

        private static IEnumerator Co_Torchic_FeatherDanceTrap(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 center = caster.position + (Vector3)(dir * 3.0f);
            var feathers = new List<Vector3>();

            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Vector3 fPos = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.45f;
                feathers.Add(fPos);
                SpawnPixelVisual(GetFeatherSprite(), fPos, 1.55f, 0.65f, i * 60f);
            }

            yield return new WaitForSeconds(0.55f);
            PMDAudio.Play(PMDSfx.Ultimate, 0.8f);
            for (int i = 0; i < feathers.Count; i++)
            {
                PMDVfxPlayer.SpawnVfx("Blast_Burn.None", feathers[i], 1.35f, 28f);
                HitEnemiesAt(feathers[i], 1.45f, skill.damage * 0.42f, Vector2.zero, skill);
            }
        }

        private static IEnumerator Co_Torchic_BlazeKickJump(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            Vector3 start = caster.position;
            Vector3 land = start + (Vector3)(dir * 3.8f);
            float t = 0f;
            while (t < 1f && caster != null)
            {
                t += Time.deltaTime * 3.2f;
                Vector3 pos = Vector3.Lerp(start, land, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 1.85f;
                caster.position = pos;
                SpawnPixelVisual(GetFeatherSprite(), pos, 1.25f, 0.18f, t * 360f);
                yield return null;
            }
            if (caster != null)
            {
                PMDVfxPlayer.SpawnVfx("Sacred_Fire_Pillar.None", land, 1.7f, 28f);
                HitEnemiesAt(land, 2.1f, skill.damage, dir * 2.0f, skill);
            }
        }

        private static IEnumerator Co_Torchic_PhoenixSkyUppercut(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.35f, 0.26f);
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // Spread giant V-shaped Phoenix Fire Wings outward while surging forward!
            for (int step = 1; step <= 6; step++)
            {
                if (caster == null) yield break;
                MoveCasterSafe(caster, dir * 0.45f);
                for (int wing = -step; wing <= step; wing++)
                {
                    Vector3 wPos = caster.position + (Vector3)(dir * 0.8f + perp * (wing * 0.58f));
                    SpawnPixelVisual(GetFeatherSprite(), wPos, 1.6f, 0.24f, wing * 15f);
                    if (Mathf.Abs(wing) == step || wing == 0)
                        PMDVfxPlayer.SpawnVfx("Sacred_Fire_Pillar.None", wPos, 1.25f, 28f);
                    HitEnemiesAt(wPos, 0.95f, skill.damage * 0.25f, dir, skill);
                }
                yield return new WaitForSeconds(0.075f);
            }
        }

        // =========================================================================================
        // 10. MUDKIP (Tạt Bùn Mud-Slap | Bom Bùn Nảy 3 Lần | 3 Mạch Nước Phun Trào | Động Đất Nứt Vực)
        // =========================================================================================
        private static IEnumerator Co_Mudkip_MudSlapSwamp(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 d = PMDProjectile.RotateVector2(dir, i * 24f);
                Vector3 p = caster.position + (Vector3)(d * 1.5f);
                SpawnPixelVisual(GetMudGlobSprite(), p, 1.65f, 1.4f, i * 40f, -10);
                HitEnemiesAt(p, 1.35f, skill.damage * 0.48f, d, skill);
            }
            yield return null;
        }

        private static IEnumerator Co_Mudkip_MudBombBounce(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            Vector3 cur = caster.position;
            for (int bounce = 1; bounce <= 3; bounce++)
            {
                PMDAudio.Play(PMDSfx.Projectile);
                Vector3 next = cur + (Vector3)(dir * 2.1f);
                yield return FlyPixelObject(GetMudGlobSprite(), cur, next, 0.20f, 1.45f, 180f);
                SpawnPixelVisual(GetMudGlobSprite(), next, 1.8f, 0.9f, 0f, -10);
                PMDVfxPlayer.SpawnVfx("Water_Spout_Splash.None", next, 1.3f, 28f);
                HitEnemiesAt(next, 1.65f, skill.damage * 0.45f, dir, skill);
                cur = next;
            }
        }

        private static IEnumerator Co_Mudkip_WhirlpoolGeyser(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
            for (int g = 1; g <= 3; g++)
            {
                Vector3 gPos = caster.position + (Vector3)(dir * (g * 1.75f));
                PMDVfxPlayer.SpawnVfx("Aqua_Ring.None", gPos, 1.45f, 28f);
                yield return new WaitForSeconds(0.14f);
                PMDVfxPlayer.SpawnVfx("Water_Spout_Splash.None", gPos, 1.75f, 26f);
                HitEnemiesAt(gPos, 1.75f, skill.damage * 0.48f, Vector2.up * 2.0f, skill);
            }
        }

        private static IEnumerator Co_Mudkip_EarthquakeFissure(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.42f, 0.34f);
            float[] branchAngles = { -32f, 0f, 32f };
            Vector3 origin = caster.position;

            // Crack the earth along 3 jagged Y-shaped fissure lines!
            for (int step = 1; step <= 6; step++)
            {
                for (int b = 0; b < branchAngles.Length; b++)
                {
                    Vector2 bDir = PMDProjectile.RotateVector2(dir, branchAngles[b]);
                    Vector3 fPos = origin + (Vector3)(bDir * (step * 0.92f));
                    SpawnPixelVisual(GetFissureCrackSprite(), fPos, 1.65f, 1.5f, branchAngles[b], -12);
                    if (step % 2 == 0)
                        PMDVfxPlayer.SpawnVfx("Water_Spout_Splash.None", fPos, 1.25f, 28f);
                    HitEnemiesAt(fPos, 1.25f, skill.damage * 0.26f, bDir, skill);
                }
                yield return new WaitForSeconds(0.065f);
            }
        }

        // =========================================================================================
        // 11. LUCARIO (6 Nắm Đấm Bullet Punch | Trượng Xương Bone Rush | Khí Công Quyền | 8 Pháo Aura)
        // =========================================================================================
        private static IEnumerator Co_Lucario_BulletPunchBarrage(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int p = 0; p < 6; p++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Melee, 0.7f);
                Vector3 offset = (Vector3)(Random.insideUnitCircle * 0.55f);
                Vector3 fistPos = caster.position + (Vector3)(dir * 1.45f) + offset;
                SpawnPixelVisual(GetAuraFistSprite(), fistPos, 1.35f, 0.14f, angle);
                HitEnemiesAt(fistPos, 1.35f, skill.damage * 0.25f, dir, skill);
                yield return new WaitForSeconds(0.04f);
            }
        }

        private static IEnumerator Co_Lucario_BoneRushTeleport(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 boneSpot = caster.position + (Vector3)(dir * 3.4f);

            // Throw spinning cyan Bone Staff that stays spinning at boneSpot, then teleport Lucario to it!
            var boneGo = SpawnPixelVisual(GetBoneStaffSprite(), boneSpot, 1.85f, 0.95f, 0f);
            for (int t = 0; t < 10; t++)
            {
                if (boneGo != null)
                    boneGo.transform.Rotate(0f, 0f, 55f);
                HitEnemiesAt(boneSpot, 1.55f, skill.damage * 0.14f, Vector2.zero, skill);
                yield return new WaitForSeconds(0.07f);
            }

            if (caster != null)
            {
                caster.position = boneSpot;
                PMDAudio.Play(PMDSfx.Dash);
                PMDVfxPlayer.SpawnVfx("Close_Combat.None", boneSpot, 1.6f, 28f);
                HitEnemiesAt(boneSpot, 2.0f, skill.damage * 0.55f, dir, skill);
            }
        }

        private static IEnumerator Co_Lucario_PowerUpPunch(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector3 start = caster.position + (Vector3)(dir * 0.8f);
            Vector3 end = caster.position + (Vector3)(dir * 4.8f);
            MoveCasterSafe(caster, dir * 1.8f);
            yield return FlyAndHitSpark(GetAuraFistSprite(), start, end, 0.24f, skill, dir * 2.5f, angle, 2.35f);
        }

        private static IEnumerator Co_Lucario_AuraCannonBarrage(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDCameraFollow.Shake(0.30f, 0.22f);
            for (int s = 0; s < 8; s++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Projectile, 0.8f);
                Vector2 d = PMDProjectile.RotateVector2(dir, Mathf.Sin(s * 1.2f) * 10f);
                PMDProjectile.SpawnProjectile(skill, caster.position, d, true, caster);
                yield return new WaitForSeconds(0.08f);
            }
        }

        // =========================================================================================
        // 12. GLACEON (3 Kim Băng | 8 Tinh Thể Icicle Spear UNITE | Cọc Băng Đất | Lãnh Địa Tuyết Lục Giác)
        // =========================================================================================
        private static IEnumerator Co_Glaceon_IceShardDaggers(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            float[] angs = { -16f, 0f, 16f };
            for (int i = 0; i < angs.Length; i++)
            {
                Vector2 d = PMDProjectile.RotateVector2(dir, angs[i]);
                float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                PMDPlayerController.Instance.StartCoroutine(
                    FlyAndHitSpark(GetIceCrystalSprite(), caster.position, caster.position + (Vector3)(d * 5.2f), 0.22f, skill, d, rot, 1.3f, 0.45f));
            }
            yield return null;
        }

        private static IEnumerator Co_Glaceon_IcicleSpearCrystals(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            // Pokémon UNITE Signature: Generate 8 floating Ice Diamonds around Glaceon, then rapid-fire them one by one!
            const int count = 8;
            var crystals = new GameObject[count];
            for (int i = 0; i < count; i++)
                crystals[i] = SpawnPixelVisual(GetIceCrystalSprite(), caster.position, 1.25f, 1.8f, 0f, 120);

            for (int i = 0; i < count; i++)
            {
                if (caster == null) yield break;
                for (int k = i; k < count; k++)
                {
                    if (crystals[k] == null) continue;
                    float a = (Time.time * 6f) + (k * Mathf.PI * 2f / count);
                    crystals[k].transform.position = caster.position + new Vector3(Mathf.Cos(a) * 1.1f, 0.4f + Mathf.Sin(a) * 0.65f, 0f);
                }

                if (crystals[i] != null)
                    Object.Destroy(crystals[i]);

                PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 3.0f), 6.5f, null);
                Vector3 dest = target != null ? target.transform.position : caster.position + (Vector3)(dir * 6.0f);
                Vector2 shotDir = ((Vector2)(dest - caster.position)).normalized;
                float rot = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;

                PMDAudio.Play(PMDSfx.Projectile, 0.65f);
                PMDPlayerController.Instance.StartCoroutine(
                    FlyAndHitSpark(GetIceCrystalSprite(), caster.position + new Vector3(0f, 0.35f, 0f), dest, 0.18f, skill, shotDir, rot, 1.3f, 0.22f));
                yield return new WaitForSeconds(0.065f);
            }
        }

        private static IEnumerator Co_Glaceon_FreezeDrySpikes(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            Vector3 start = caster.position;
            for (int s = 1; s <= 7; s++)
            {
                Vector3 spikePos = start + (Vector3)(dir * (s * 0.92f));
                SpawnPixelVisual(GetIceSpikePillarSprite(), spikePos, 1.75f, 0.85f, 0f);
                PMDVfxPlayer.SpawnVfx("Sheer_Cold_Front.None", spikePos, 1.2f, 28f);
                HitEnemiesAt(spikePos, 1.25f, skill.damage * 0.32f, Vector2.zero, skill);
                yield return new WaitForSeconds(0.06f);
            }
        }

        private static IEnumerator Co_Glaceon_SnowGlobeBlizzard(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 globeCenter = caster.position;
            SpawnPixelVisual(GetSnowGlobeFieldSprite(), globeCenter, 3.4f, 2.6f, 0f, -14);

            for (int flake = 0; flake < 10; flake++)
            {
                Vector3 drop = globeCenter + (Vector3)(Random.insideUnitCircle * 2.8f);
                SpawnPixelVisual(GetIceCrystalSprite(), drop, 1.65f, 0.35f, flake * 30f);
                PMDVfxPlayer.SpawnVfx("Sheer_Cold_Front.None", drop, 1.35f, 28f);
                HitEnemiesAt(globeCenter, 3.0f, skill.damage * 0.18f, Vector2.zero, skill);
                yield return new WaitForSeconds(0.22f);
            }
        }

        // =========================================================================================
        // 13..30 SIGNATURE LEGENDARY & ICONIC MOVES (Roar of Time, Spacial Rend, Shadow Force, etc.)
        // =========================================================================================
        private static IEnumerator Co_Dialga_RoarOfTime(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 center = caster.position;
            SpawnPixelVisual(GetClockRingSprite(), center, 3.8f, 1.8f, 0f, -10);

            // Freeze all enemies in temporal stasis then shatter with a giant chrono blast!
            var frozenTargets = new List<PMDWildPokemonAI>();
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(center, w.transform.position) <= 4.8f)
                {
                    w.ApplyStatusAilment(PMDStatusAilment.Frozen, 1.8f);
                    frozenTargets.Add(w);
                }
            }

            yield return new WaitForSeconds(0.9f);
            PMDCameraFollow.Shake(0.45f, 0.35f);
            SpawnExpandingRing(center, new Color(0.35f, 0.88f, 1f), 0.6f, 4.8f, 0.45f);
            for (int i = 0; i < frozenTargets.Count; i++)
            {
                var w = frozenTargets[i];
                if (w != null && !w.IsFainted)
                {
                    PMDVfxPlayer.SpawnVfx("Discharge.None", w.transform.position, 1.5f, 28f);
                    w.TakeDamage(skill.damage, Vector2.zero, skill.elementColor);
                }
            }
        }

        private static IEnumerator Co_Palkia_SpacialRend(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.35f, 0.28f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 tearPos = caster.position + (Vector3)(dir * (1.5f + i * 1.15f)) + (Vector3)(Random.insideUnitCircle * 0.45f);
                SpawnPixelVisual(GetClawXSprite(), tearPos, 2.3f, 0.45f, i * 35f, 110, new Color(1f, 0.42f, 0.95f));
                HitEnemiesAt(tearPos, 2.1f, skill.damage * 0.42f, dir, skill);
                yield return new WaitForSeconds(0.08f);
            }
        }

        private static IEnumerator Co_Giratina_ShadowForce(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            var sr = caster.GetComponent<SpriteRenderer>();
            SpawnPixelVisual(GetVoidPortalSprite(), caster.position, 2.2f, 0.7f, 0f, -5);
            if (sr != null) sr.enabled = false;

            yield return new WaitForSeconds(0.45f);
            if (caster == null) yield break;

            PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 3.5f), 6.5f, null);
            Vector3 exitPos = target != null ? target.transform.position : caster.position + (Vector3)(dir * 4.2f);
            caster.position = exitPos;
            if (sr != null) sr.enabled = true;

            PMDCameraFollow.Shake(0.4f, 0.32f);
            SpawnPixelVisual(GetVoidPortalSprite(), exitPos, 2.8f, 0.9f, 0f, -5);
            PMDVfxPlayer.SpawnVfx("Explosion.None", exitPos, 1.8f, 26f);
            HitEnemiesAt(exitPos, 2.6f, skill.damage, dir, skill);
        }

        private static IEnumerator Co_Darkrai_DarkVoid(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 holeCenter = caster.position + (Vector3)(dir * 3.0f);
            var hole = SpawnPixelVisual(GetVoidPortalSprite(), holeCenter, 3.0f, 2.4f, 0f, -10);

            for (int p = 0; p < 6; p++)
            {
                if (hole != null) hole.transform.Rotate(0f, 0f, 60f);
                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var w = allWild[i];
                    if (w == null || w.IsFainted || w.IsRecruited) continue;
                    if (Vector2.Distance(holeCenter, w.transform.position) <= 3.6f)
                    {
                        w.transform.position = Vector2.MoveTowards(w.transform.position, holeCenter, 0.55f);
                        w.ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.2f);
                        w.TakeDamage(skill.damage * 0.22f, Vector2.zero, skill.elementColor);
                    }
                }
                yield return new WaitForSeconds(0.32f);
            }
        }

        private static IEnumerator Co_Arceus_Judgment(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.45f, 0.35f);
            Color[] judgmentColors =
            {
                new Color(1f, 0.92f, 0.35f), new Color(1f, 0.45f, 0.25f),
                new Color(0.35f, 0.85f, 1f), new Color(0.95f, 0.45f, 0.95f),
                new Color(0.45f, 0.98f, 0.52f)
            };

            Vector3 origin = caster.position;
            for (int m = 0; m < 16; m++)
            {
                float a = (m * 22.5f) * Mathf.Deg2Rad;
                Vector3 drop = origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(1.4f, 4.2f);
                Color col = judgmentColors[m % judgmentColors.Length];
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetDoomStarSprite(), drop + new Vector3(0f, 3.0f, 0f), drop, 0.22f, skill, Vector2.zero, 0f, 1.5f, 0.28f));
                SpawnExpandingRing(drop, col, 0.3f, 1.7f, 0.28f);
                yield return new WaitForSeconds(0.06f);
            }
        }

        private static IEnumerator Co_Kyogre_OriginPulse(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            float[] spreads = { -26f, 0f, 26f };
            for (int i = 0; i < spreads.Length; i++)
            {
                Vector2 d = PMDProjectile.RotateVector2(dir, spreads[i]);
                Vector3 dest = caster.position + (Vector3)(d * 5.8f);
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetDoomStarSprite(), caster.position, dest, 0.45f, skill, d, 0f, 2.0f, 0.45f));
            }
            yield return new WaitForSeconds(0.45f);
        }

        private static IEnumerator Co_Groudon_PrecipiceBlades(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.42f, 0.34f);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector3 origin = caster.position;

            for (int row = 1; row <= 5; row++)
            {
                float pinch = (5 - row) * 0.42f;
                Vector3 leftBlade = origin + (Vector3)(dir * (row * 1.05f) + perp * pinch);
                Vector3 rightBlade = origin + (Vector3)(dir * (row * 1.05f) - perp * pinch);
                SpawnPixelVisual(GetTectonicBladeSprite(), leftBlade, 1.95f, 0.75f, 0f);
                SpawnPixelVisual(GetTectonicBladeSprite(), rightBlade, 1.95f, 0.75f, 0f);
                HitEnemiesAt(leftBlade, 1.4f, skill.damage * 0.28f, dir, skill);
                HitEnemiesAt(rightBlade, 1.4f, skill.damage * 0.28f, dir, skill);
                yield return new WaitForSeconds(0.08f);
            }
        }

        private static IEnumerator Co_Lugia_Aeroblast(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.32f, 0.25f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int seg = 1; seg <= 12; seg++)
            {
                Vector3 p = caster.position + (Vector3)(dir * (seg * 0.65f));
                SpawnPixelVisual(GetWindCrescentSprite(), p, 1.85f, 0.24f, angle, 110, new Color(0.75f, 0.96f, 1f));
                HitEnemiesAt(p, 1.35f, skill.damage * 0.25f, dir * 2.0f, skill);
                yield return new WaitForSeconds(0.03f);
            }
        }

        private static IEnumerator Co_HoOh_SacredFire(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            Vector3 center = caster.position + (Vector3)(dir * 2.5f);
            for (int i = 0; i < 7; i++)
            {
                Vector3 p = center + (Vector3)(Random.insideUnitCircle * 2.2f);
                SpawnPixelVisual(GetFeatherSprite(), p, 1.85f, 0.55f, i * 50f, 110, new Color(1f, 0.85f, 0.28f));
                PMDVfxPlayer.SpawnVfx("Sacred_Fire_Pillar.None", p, 1.65f, 26f);
                HitEnemiesAt(p, 1.75f, skill.damage * 0.35f, Vector2.up, skill);
                yield return new WaitForSeconds(0.09f);
            }
        }

        private static IEnumerator Co_Jirachi_DoomDesire(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 targetZone = caster.position + (Vector3)(dir * 3.4f);
            PMDWildPokemonAI lockMon = FindNearestWild(targetZone, 3.8f, null);
            if (lockMon != null) targetZone = lockMon.transform.position;

            // Lock star mark for 1.2s before cosmic detonation
            var mark = SpawnPixelVisual(GetDoomStarSprite(), targetZone + new Vector3(0f, 0.6f, 0f), 1.8f, 1.25f, 0f, 120);
            for (int t = 0; t < 12; t++)
            {
                if (lockMon != null && !lockMon.IsFainted)
                {
                    targetZone = lockMon.transform.position;
                    if (mark != null) mark.transform.position = targetZone + new Vector3(0f, 0.6f, 0f);
                }
                if (mark != null) mark.transform.Rotate(0f, 0f, 30f);
                yield return new WaitForSeconds(0.10f);
            }

            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.4f, 0.32f);
            PMDVfxPlayer.SpawnVfx("Explosion.None", targetZone, 2.1f, 24f);
            SpawnExpandingRing(targetZone, new Color(1f, 0.94f, 0.45f), 0.5f, 3.8f, 0.38f);
            HitEnemiesAt(targetZone, 2.8f, skill.damage, Vector2.zero, skill);
        }

        private static IEnumerator Co_Decidueye_SpiritShackle(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector3 dest = caster.position + (Vector3)(dir * 6.8f);
            yield return FlyAndHitSpark(GetShadowArrowSprite(), caster.position, dest, 0.25f, skill, dir, angle, 1.65f, 0.9f);

            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(dest, w.transform.position) <= 2.4f)
                    w.ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.8f);
            }
        }

        private static IEnumerator Co_Dragapult_DragonDarts(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            for (int d = 0; d < 2; d++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Projectile);
                Vector2 dartDir = PMDProjectile.RotateVector2(dir, d == 0 ? -12f : 12f);
                float angle = Mathf.Atan2(dartDir.y, dartDir.x) * Mathf.Rad2Deg;
                PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dartDir * 3.5f), 7.5f, null);
                Vector3 dest = target != null ? target.transform.position : caster.position + (Vector3)(dartDir * 6.5f);
                PMDPlayerController.Instance.StartCoroutine(FlyAndHitSpark(GetShadowArrowSprite(), caster.position, dest, 0.22f, skill, dartDir, angle, 1.55f, 0.58f));
                yield return new WaitForSeconds(0.11f);
            }
        }

        private static IEnumerator Co_Mimikyu_LetsSnuggle(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            PMDWildPokemonAI target = FindNearestWild(caster.position + (Vector3)(dir * 2.5f), 4.8f, null);
            Vector3 spot = target != null ? target.transform.position : caster.position + (Vector3)(dir * 2.8f);
            caster.position = spot;

            for (int hit = 0; hit < 6; hit++)
            {
                SpawnPixelVisual(GetClawXSprite(), spot + (Vector3)(Random.insideUnitCircle * 0.45f), 1.65f, 0.22f, hit * 45f, 110, new Color(0.85f, 0.45f, 1f));
                HitEnemiesAt(spot, 1.9f, skill.damage * 0.24f, Random.insideUnitCircle, skill);
                yield return new WaitForSeconds(0.07f);
            }
        }

        private static IEnumerator Co_Zoroark_NightDaze(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.32f, 0.26f);
            Vector3 center = caster.position;
            for (int r = 0; r < 3; r++)
            {
                SpawnExpandingRing(center, new Color(0.92f, 0.14f, 0.28f), 0.5f, 3.8f + r * 0.8f, 0.35f);
                SpawnPixelVisual(GetVoidPortalSprite(), center, 2.2f + r * 0.7f, 0.3f, r * 40f, 90, new Color(0.95f, 0.18f, 0.32f));
                HitEnemiesAt(center, 3.6f, skill.damage * 0.40f, Vector2.zero, skill);
                yield return new WaitForSeconds(0.14f);
            }
        }

        private static IEnumerator Co_Venusaur_PetalDance(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            for (int wave = 0; wave < 8; wave++)
            {
                if (caster == null) yield break;
                for (int p = 0; p < 5; p++)
                {
                    float a = (wave * 32f + p * 72f) * Mathf.Deg2Rad;
                    Vector3 pos = caster.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (1.4f + (wave % 3) * 0.55f);
                    SpawnPixelVisual(GetPetalBlossomSprite(), pos, 1.45f, 0.35f, wave * 45f);
                    HitEnemiesAt(pos, 1.35f, skill.damage * 0.16f, Vector2.zero, skill);
                }
                yield return new WaitForSeconds(0.14f);
            }
        }

        private static IEnumerator Co_Blastoise_HydroCannon(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.35f, 0.28f);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            for (int seg = 1; seg <= 9; seg++)
            {
                Vector3 leftP = caster.position + (Vector3)(dir * (seg * 0.72f) + perp * 0.42f);
                Vector3 rightP = caster.position + (Vector3)(dir * (seg * 0.72f) - perp * 0.42f);
                SpawnPixelVisual(GetWaveCrestSprite(), leftP, 1.55f, 0.24f, angle);
                SpawnPixelVisual(GetWaveCrestSprite(), rightP, 1.55f, 0.24f, angle);
                HitEnemiesAt(leftP, 1.25f, skill.damage * 0.22f, dir * 1.8f, skill);
                HitEnemiesAt(rightP, 1.25f, skill.damage * 0.22f, dir * 1.8f, skill);
                yield return new WaitForSeconds(0.04f);
            }
        }

        private static IEnumerator Co_Machamp_DynamicPunch(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int p = 0; p < 8; p++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Melee, 0.75f);
                Vector3 pos = caster.position + (Vector3)(dir * 1.5f) + (Vector3)(Random.insideUnitCircle * 0.65f);
                SpawnPixelVisual(GetFireFistSprite(), pos, 1.5f, 0.18f, angle, 105, new Color(1f, 0.78f, 0.25f));
                HitEnemiesAt(pos, 1.5f, skill.damage * 0.22f, dir * 1.2f, skill);
                yield return new WaitForSeconds(0.05f);
            }
        }

        private static IEnumerator Co_Sylveon_Moonblast(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 targetPos = caster.position + (Vector3)(dir * 3.8f);
            yield return FlyPixelObject(GetDoomStarSprite(), caster.position, targetPos, 0.28f, 2.2f, 180f);
            PMDCameraFollow.Shake(0.32f, 0.25f);
            SpawnExpandingRing(targetPos, new Color(1f, 0.55f, 0.88f), 0.5f, 3.4f, 0.35f);
            SpawnPixelVisual(GetPetalBlossomSprite(), targetPos, 2.4f, 0.5f, 0f, 110, new Color(1f, 0.62f, 0.92f));
            HitEnemiesAt(targetPos, 2.6f, skill.damage, dir, skill);
        }

        // =========================================================================================
        // SHARED COMBAT & VISUAL HELPERS
        // =========================================================================================
        public static void MoveCasterSafePublic(Transform caster, Vector2 delta) => MoveCasterSafe(caster, delta);
        public static PMDWildPokemonAI FindNearestWildPublic(Vector3 fromPos, float maxRadius, HashSet<PMDWildPokemonAI> exclude) => FindNearestWild(fromPos, maxRadius, exclude);
        public static void RegisterAnimStripPublic(string key, List<Sprite> frames)
        {
            if (!string.IsNullOrEmpty(key) && frames != null && frames.Count > 0)
                ProceduralAnimStrips[key] = frames;
        }
        public static GameObject SpawnPixelVisualPublic(Sprite sp, Vector3 pos, float scale, float duration, float rotZ, int sortOffset = 95, Color? tint = null) => SpawnPixelVisual(sp, pos, scale, duration, rotZ, sortOffset, tint);

        public static IEnumerator FlyAndHitSparkPublic(
            Sprite sp,
            Vector3 start,
            Vector3 end,
            float duration,
            PMDSkillData skill,
            Vector2 knock,
            float initialRot = 0f,
            float scale = 1.25f,
            float dmgMult = 0.45f,
            Color? tint = null)
        {
            var go = SpawnPixelVisual(sp, start, scale, duration + 0.05f, initialRot, 105, tint);
            var hitSet = new HashSet<PMDWildPokemonAI>();
            float elapsed = 0f;
            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(start, end, t);
                go.transform.position = p;

                // Beautify: stepped afterimage smear + elemental trail motes behind projectiles
                if ((Time.frameCount & 1) == 0)
                {
                    PMDPixelVfxSystem.SpawnAfterimage(go.GetComponent<SpriteRenderer>(), 0.5f, 0.15f);
                    if (skill != null)
                        PMDPixelVfxSystem.SpawnTrailMote(p, skill.elementColor, skill.moveType);
                }

                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var w = allWild[i];
                    if (w == null || w.IsFainted || w.IsRecruited || hitSet.Contains(w)) continue;
                    if (Vector2.Distance(p, w.transform.position) <= 0.95f)
                    {
                        hitSet.Add(w);
                        w.TakeDamage(Mathf.Max(1f, skill.damage * dmgMult), knock, skill.elementColor);
                    }
                }
                yield return null;
            }
        }

        public static Sprite GetSpriteByVisualKey(string key) => key switch
        {
            "Bolt" => GetBoltSprite(),
            "Electroweb" => GetElectricWebSprite(),
            "StormCloud" => GetStormCloudSprite(),
            "VineSeg" => GetVineSegmentSprite(),
            "LeafBlade" => GetLeafBladeSprite(),
            "SludgeGlob" => GetSludgeGlobSprite(),
            "SludgePool" => GetSludgePoolSprite(),
            "SolarBeam" => GetSolarBeamSegmentSprite(),
            "FireFist" => GetFireFistSprite(),
            "FireOrb" => GetFireOrbSprite(),
            "ClawX" => GetClawXSprite(),
            "WindCrescent" => GetWindCrescentSprite(),
            "BubblePrison" => GetBubblePrisonSprite(),
            "WaveCrest" => GetWaveCrestSprite(),
            "GoldStar" => GetGoldStarSprite(),
            "CrystalWall" => GetCrystalWallSprite(),
            "Supernova" => GetSupernovaOrbSprite(),
            "CrossSlash" => GetCrossSlashSprite(),
            "BulletSeed" => GetBulletSeedSprite(),
            "ThornRoot" => GetThornRootSprite(),
            "FireFeather" => GetFeatherSprite(),
            "MudGlob" => GetMudGlobSprite(),
            "Fissure" => GetFissureCrackSprite(),
            "AuraFist" => GetAuraFistSprite(),
            "BoneStaff" => GetBoneStaffSprite(),
            "IceCrystal" => GetIceCrystalSprite(),
            "IceSpike" => GetIceSpikePillarSprite(),
            "SnowGlobe" => GetSnowGlobeFieldSprite(),
            "ClockRing" => GetClockRingSprite(),
            "VoidPortal" => GetVoidPortalSprite(),
            "TectonicBlade" => GetTectonicBladeSprite(),
            "DoomStar" => GetDoomStarSprite(),
            "ShadowArrow" => GetShadowArrowSprite(),
            "PetalBlossom" => GetPetalBlossomSprite(),
            _ => GetDoomStarSprite()
        };

        private static void MoveCasterSafe(Transform caster, Vector2 delta)
        {
            if (caster == null) return;
            SpawnAfterimage(caster, new Color(0.55f, 0.92f, 1f, 0.68f));
            Vector2 next = (Vector2)caster.position + delta;
            var mapGen = PMDOpenWorldMapGenerator.Instance;
            if (mapGen == null || mapGen.IsWorldPointWalkable(next))
                caster.position = next;
        }

        private static void HitEnemiesAt(Vector3 center, float radius, float dmg, Vector2 knock, PMDSkillData skill)
        {
            if (!string.IsNullOrEmpty(skill.hitVfxSheet) && Random.value < 0.45f)
                PMDVfxPlayer.SpawnVfx(skill.hitVfxSheet, center, 1.15f, 28f);

            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = allWild.Count - 1; i >= 0; i--)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited) continue;
                if (Vector2.Distance(center, w.transform.position) <= radius)
                {
                    w.TakeDamage(Mathf.Max(1f, dmg), knock, skill.elementColor);
                }
            }
        }

        private static void HitAndPullEnemiesTo(Vector3 checkCenter, float radius, float dmg, Vector3 pullDest, float maxStep, PMDSkillData skill)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = allWild.Count - 1; i >= 0; i--)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited) continue;
                if (Vector2.Distance(checkCenter, w.transform.position) <= radius)
                {
                    w.transform.position = Vector2.MoveTowards(w.transform.position, pullDest, maxStep);
                    w.TakeDamage(Mathf.Max(1f, dmg), Vector2.zero, skill.elementColor);
                }
            }
        }

        private static PMDWildPokemonAI FindNearestWild(Vector3 fromPos, float maxRadius, HashSet<PMDWildPokemonAI> exclude)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            PMDWildPokemonAI best = null;
            float bestDist = maxRadius;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited || (exclude != null && exclude.Contains(w)))
                    continue;
                float d = Vector2.Distance(fromPos, w.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = w;
                }
            }
            return best;
        }

        private static GameObject SpawnPixelVisual(Sprite sp, Vector3 pos, float scale, float duration, float rotZ, int sortOffset = 95, Color? tint = null)
        {
            var go = new GameObject("PMD_SkillPixelVfx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            if (tint.HasValue)
                sr.color = tint.Value;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + sortOffset;

            List<Sprite> frames = null;
            if (sp != null && !string.IsNullOrEmpty(sp.name))
                ProceduralAnimStrips.TryGetValue(sp.name, out frames);

            var anim = go.AddComponent<PMDAnimatedPixelVfx>();
            anim.Initialize(sr, scale, duration, true, frames);

            // Beautify: pulsing dithered glow halo behind every skill sprite (skip ground decals)
            if (sortOffset >= 0)
                PMDPixelVfxSystem.AttachGlowHalo(go, tint ?? Color.white, 1.65f, 0.4f);
            return go;
        }

        // =========================================================================================
        // MULTI-LAYERED SKILL JUICE, AFTERIMAGES, EXPANDING RINGS, PARTICLES & STATUS VFX
        // =========================================================================================
        public static void SpawnSkillCastBurst(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            if (caster == null || skill == null)
                return;

            Vector3 pos = caster.position + new Vector3(0f, 0.18f, 0f);
            bool ult = skill.kind == PMDSkillKind.UltimateOrbitalStorm;
            PMDPixelVfxSystem.SpawnChargeIn(caster, skill.elementColor, skill.moveType, ult ? 16 : 10, ult ? 2.1f : 1.35f, ult ? 0.3f : 0.2f);
            SpawnExpandingRing(pos, skill.elementColor, 0.5f, ult ? 3.2f : 1.85f, 0.32f);
            SpawnParticleBurst(pos + (Vector3)(dir * 0.35f), skill.elementColor, skill.kind == PMDSkillKind.UltimateOrbitalStorm ? 14 : 8, 3.6f);

            if (!string.IsNullOrEmpty(skill.ringVfxSheet))
                PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, pos, 1.25f, 28f);
        }

        public static void SpawnHitImpactJuice(Vector3 pos, Color elementColor, PMDElement atk)
        {
            PMDPixelVfxSystem.SpawnImpactStar(pos, elementColor, 1.5f);
            SpawnExpandingRing(pos + new Vector3(0f, 0.15f, 0f), elementColor, 0.35f, 1.55f, 0.24f);
            SpawnParticleBurst(pos + new Vector3(0f, 0.2f, 0f), elementColor, 9, 4.2f);

            // Leave subtle elemental ground decal (scorch / frost / puddle) that fades out
            Color groundCol = new Color(elementColor.r * 0.55f, elementColor.g * 0.55f, elementColor.b * 0.55f, 0.55f);
            var decal = SpawnPixelVisual(GetGroundDecalSprite(), pos + new Vector3(0f, -0.05f, 0f), 1.45f, 1.4f, Random.Range(0f, 360f), -18);
            if (decal != null)
            {
                var sr = decal.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = groundCol;
            }
        }

        public static void SpawnStatusAppliedBurst(Transform target, PMDStatusAilment ailment, float duration)
        {
            if (target == null)
                return;

            Vector3 pos = target.position + new Vector3(0f, 0.35f, 0f);
            switch (ailment)
            {
                case PMDStatusAilment.Paralyzed:
                    PMDVfxPlayer.SpawnFloatingText(pos, "⚡ TÊ LIỆT! (-60% Tốc)", new Color(1f, 0.92f, 0.22f), 1.0f);
                    SpawnExpandingRing(target.position, new Color(1f, 0.92f, 0.22f), 0.4f, 1.6f, 0.28f);
                    break;
                case PMDStatusAilment.Burned:
                    PMDVfxPlayer.SpawnFloatingText(pos, "🔥 BỎNG LỬA! (Rút HP)", new Color(1f, 0.45f, 0.15f), 1.0f);
                    SpawnExpandingRing(target.position, new Color(1f, 0.45f, 0.15f), 0.4f, 1.6f, 0.28f);
                    break;
                case PMDStatusAilment.Frozen:
                {
                    PMDVfxPlayer.SpawnFloatingText(pos, "❄️ ĐÓNG BĂNG!", new Color(0.55f, 0.95f, 1f), 1.15f);
                    SpawnExpandingRing(target.position, new Color(0.55f, 0.95f, 1f), 0.4f, 1.8f, 0.32f);
                    // Attach translucent Pixel Ice Block Cage directly onto frozen target!
                    var cage = SpawnPixelVisual(GetIceCageSprite(), target.position + new Vector3(0f, 0.18f, 0f), 1.85f, duration, 0f, 110);
                    if (cage != null)
                        cage.transform.SetParent(target, true);
                    break;
                }
                case PMDStatusAilment.Poisoned:
                    PMDVfxPlayer.SpawnFloatingText(pos, "☠️ TRÚNG ĐỘC!", new Color(0.78f, 0.35f, 0.98f), 1.0f);
                    SpawnExpandingRing(target.position, new Color(0.78f, 0.35f, 0.98f), 0.4f, 1.6f, 0.28f);
                    break;
                case PMDStatusAilment.SoakedSlow:
                    PMDVfxPlayer.SpawnFloatingText(pos, "💧 ƯỚT SŨNG! (Chậm)", new Color(0.32f, 0.80f, 1f), 1.0f);
                    SpawnExpandingRing(target.position, new Color(0.32f, 0.80f, 1f), 0.4f, 1.6f, 0.28f);
                    break;
                case PMDStatusAilment.PsychicStun:
                    PMDVfxPlayer.SpawnFloatingText(pos, "🔮 CHOÁNG NIỆM LỰC!", new Color(0.95f, 0.48f, 0.92f), 1.0f);
                    SpawnExpandingRing(target.position, new Color(0.95f, 0.48f, 0.92f), 0.4f, 1.6f, 0.28f);
                    break;
            }
        }

        public static void SpawnStatusTickJuice(Vector3 pos, PMDStatusAilment ailment)
        {
            switch (ailment)
            {
                case PMDStatusAilment.Paralyzed:
                    SpawnPixelVisual(GetBoltSprite(), pos + (Vector3)(Random.insideUnitCircle * 0.35f), 0.95f, 0.22f, Random.Range(0f, 360f));
                    break;
                case PMDStatusAilment.Burned:
                    SpawnPixelVisual(GetFireOrbSprite(), pos + new Vector3(Random.Range(-0.25f, 0.25f), 0.3f, 0f), 0.9f, 0.28f, 0f);
                    SpawnParticleBurst(pos, new Color(1f, 0.48f, 0.15f), 4, 2.2f);
                    break;
                case PMDStatusAilment.Frozen:
                    SpawnPixelVisual(GetIceCrystalSprite(), pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.35f, 0f), 0.95f, 0.28f, 45f);
                    break;
                case PMDStatusAilment.Poisoned:
                    SpawnPixelVisual(GetSludgeGlobSprite(), pos + new Vector3(Random.Range(-0.25f, 0.25f), 0.35f, 0f), 0.85f, 0.30f, 0f);
                    break;
                case PMDStatusAilment.SoakedSlow:
                    SpawnParticleBurst(pos, new Color(0.32f, 0.82f, 1f), 4, 1.8f);
                    break;
                case PMDStatusAilment.PsychicStun:
                    SpawnExpandingRing(pos + new Vector3(0f, 0.35f, 0f), new Color(0.95f, 0.48f, 0.92f), 0.3f, 1.1f, 0.25f);
                    break;
            }
        }

        public static void SpawnAfterimage(Transform caster, Color tint)
        {
            if (caster == null) return;
            var srcSr = caster.GetComponent<SpriteRenderer>();
            if (srcSr == null || srcSr.sprite == null) return;

            var go = new GameObject("PMD_Afterimage");
            go.transform.position = caster.position;
            go.transform.localScale = caster.localScale;
            go.transform.localRotation = caster.localRotation;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = srcSr.sprite;
            sr.flipX = srcSr.flipX;
            sr.color = tint;
            sr.sortingOrder = srcSr.sortingOrder - 2;

            var anim = go.AddComponent<PMDAnimatedPixelVfx>();
            anim.Initialize(sr, caster.localScale.x, 0.22f, false);
        }

        public static void SpawnExpandingRing(Vector3 pos, Color color, float startScale, float endScale, float duration)
        {
            var go = new GameObject("PMD_ShockwaveRing");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetShockwaveRingSprite();
            sr.color = color;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 88;

            var ring = go.AddComponent<PMDExpandingRingVfx>();
            ring.Initialize(sr, startScale, endScale, duration);

            // Beautify: thin white leading shockwave inside the colored ring (2-tone pixel ring)
            var inner = new GameObject("PMD_ShockwaveRingInner");
            inner.transform.position = pos;
            var isr = inner.AddComponent<SpriteRenderer>();
            isr.sprite = GetShockwaveRingSprite();
            isr.color = Color.Lerp(color, Color.white, 0.75f);
            isr.sortingOrder = sr.sortingOrder + 1;
            var iring = inner.AddComponent<PMDExpandingRingVfx>();
            iring.Initialize(isr, startScale * 0.8f, endScale * 0.82f, duration * 0.7f);
        }

        public static void SpawnParticleBurst(Vector3 pos, Color color, int count, float speed)
        {
            Sprite sp = GetSparkParticleSprite();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("PMD_PixelParticle");
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.color = i % 2 == 0 ? Color.Lerp(color, Color.white, 0.45f) : color;
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 105;

                float ang = (i * (360f / count) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(speed * 0.55f, speed * 1.15f);
                var part = go.AddComponent<PMDPhysicsParticleVfx>();
                part.Initialize(sr, vel, Random.Range(2.2f, 4.2f), Random.Range(0.28f, 0.45f));
            }
        }

        private static IEnumerator FlyPixelObject(Sprite sp, Vector3 start, Vector3 end, float duration, float scale, float spinSpeed, bool arc = true)
        {
            var go = SpawnPixelVisual(sp, start, scale, duration + 0.05f, 0f);
            float elapsed = 0f;
            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(start, end, t);
                if (arc)
                    p.y += Mathf.Sin(t * Mathf.PI) * 0.75f;
                go.transform.position = p;
                if (Mathf.Abs(spinSpeed) > 0.1f)
                    go.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private static IEnumerator FlyAndHitSpark(
            Sprite sp,
            Vector3 start,
            Vector3 end,
            float duration,
            PMDSkillData skill,
            Vector2 knock,
            float initialRot = 0f,
            float scale = 1.25f,
            float dmgMult = 0.45f)
        {
            var go = SpawnPixelVisual(sp, start, scale, duration + 0.05f, initialRot);
            var hitSet = new HashSet<PMDWildPokemonAI>();
            float elapsed = 0f;
            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(start, end, t);
                go.transform.position = p;

                // Beautify: stepped afterimage smear + elemental trail motes behind projectiles
                if ((Time.frameCount & 1) == 0)
                {
                    PMDPixelVfxSystem.SpawnAfterimage(go.GetComponent<SpriteRenderer>(), 0.5f, 0.15f);
                    if (skill != null)
                        PMDPixelVfxSystem.SpawnTrailMote(p, skill.elementColor, skill.moveType);
                }

                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var w = allWild[i];
                    if (w == null || w.IsFainted || w.IsRecruited || hitSet.Contains(w)) continue;
                    if (Vector2.Distance(p, w.transform.position) <= 0.9f)
                    {
                        hitSet.Add(w);
                        w.TakeDamage(Mathf.Max(1f, skill.damage * dmgMult), knock, skill.elementColor);
                    }
                }
                yield return null;
            }
        }

        // =========================================================================================
        // HIGH-DETAIL 20-FRAME ANIMATED PIXEL-ART SKILL ASSET GENERATORS (FilterMode.Point)
        // Every skill asset is rendered as a 20-frame animated pixel-art sprite strip (24x24 to 48x48).
        // Each builder receives a continuous animation phase f in [0, 6) sampled at 20 steps, so the
        // original 6-key motion cycle is subdivided into 20 smooth in-between frames, with 5-shade
        // pixel palettes, dithering, specular highlights, and a crisp 1px dark outline.
        // =========================================================================================
        public const int SkillAnimFrameCount = 20;
        private const float SkillAnimPhaseRange = 6f;
        private static readonly Color PixelOutlineDark = new Color(0.08f, 0.07f, 0.13f, 0.96f);

        /// <summary>Integer key-frame of a continuous phase (for discrete flicker / dither toggles).</summary>
        private static int Fi(float phase) => Mathf.FloorToInt(phase);

        private static Sprite GetOrBuildSprite(string key, int size, System.Func<int, int, int, float, Color> framePixelFunc, bool addOutline = true)
        {
            if (ProceduralSprites.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            const int frameCount = SkillAnimFrameCount;
            int sheetW = size * frameCount;
            int sheetH = size;

            var tex = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            var sheetBuf = new Color[sheetW * sheetH];
            var frames = new List<Sprite>(frameCount);

            for (int f = 0; f < frameCount; f++)
            {
                float phase = f * (SkillAnimPhaseRange / frameCount);
                var raw = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        raw[y * size + x] = framePixelFunc(x, y, size, phase);

                if (addOutline)
                    ApplyCrispPixelOutline(raw, size);

                int xOff = f * size;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        sheetBuf[y * sheetW + (xOff + x)] = raw[y * size + x];

                var sp = Sprite.Create(tex, new Rect(xOff, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
                sp.name = key;
                frames.Add(sp);
            }

            tex.SetPixels(sheetBuf);
            tex.Apply(false);

            ProceduralSprites[key] = frames[0];
            ProceduralAnimStrips[key] = frames;
            return frames[0];
        }

        private static void ApplyCrispPixelOutline(Color[] buf, int size)
        {
            var copy = (Color[])buf.Clone();
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    if (copy[y * size + x].a > 0.15f)
                        continue;

                    for (int k = 0; k < 4; k++)
                    {
                        Color n = copy[(y + dy[k]) * size + (x + dx[k])];
                        if (n.a > 0.45f)
                        {
                            buf[y * size + x] = PixelOutlineDark;
                            break;
                        }
                    }
                }
            }
        }

        // 1. Forked Multi-Branching Lightning Bolt (28x28, 6 frames)
        private static Sprite GetBoltSprite() => GetOrBuildSprite("Bolt", 28, (x, y, s, f) =>
        {
            int shift = (Fi(f) % 2 == 0) ? 1 : -1;
            int trunkX = 14 + (((y / 4) % 2 == 0) ? (y % 4) - 1 : 2 - (y % 4)) * shift;
            int dMain = Mathf.Abs(x - trunkX);

            // Animated side forks branching off at y=8 and y=18
            int fork1X = trunkX + (y - 7) * 2 * shift;
            int dFork1 = (y >= 7 && y <= 14) ? Mathf.Abs(x - fork1X) : 99;
            int fork2X = trunkX - (y - 16) * 2 * shift;
            int dFork2 = (y >= 16 && y <= 23) ? Mathf.Abs(x - fork2X) : 99;

            int d = Mathf.Min(dMain, Mathf.Min(dFork1, dFork2));
            if (y < 2 || y > 25 || d > 3) return Color.clear;
            if (d == 0) return Color.white;
            if (d == 1) return new Color(1f, 0.96f, 0.32f);
            if (d == 2) return new Color(1f, 0.68f, 0.12f);
            return ((x + y + Fi(f)) % 2 == 0) ? new Color(0.35f, 0.85f, 1f, 0.85f) : Color.clear;
        });

        // 2. Octagonal Crackling Spider-Web Electric Trap (36x36, 6 frames)
        private static Sprite GetElectricWebSprite() => GetOrBuildSprite("Electroweb", 36, (x, y, s, f) =>
        {
            float dx = x - 17.5f, dy = y - 17.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 16.2f) return Color.clear;

            float angle = Mathf.Atan2(dy, dx);
            float scallop = Mathf.Sin(angle * 8f) * 0.65f;
            bool ring = Mathf.Abs(r + scallop - 5.5f) < 0.85f
                     || Mathf.Abs(r + scallop - 10.5f) < 0.85f
                     || Mathf.Abs(r + scallop - 15.0f) < 0.85f;
            bool spoke = Mathf.Abs(dx) < 0.75f || Mathf.Abs(dy) < 0.75f || Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) < 0.85f;

            if (!ring && !spoke) return Color.clear;
            bool pulseNode = ((Mathf.RoundToInt(r) + Fi(f) * 2) % 5 == 0);
            if (pulseNode) return Color.white;
            return (ring && spoke) ? new Color(0.55f, 0.96f, 1f) : new Color(1f, 0.90f, 0.20f);
        });

        // 3. Billowing Multi-Puff Thunderhead Storm Cloud with Internal Lightning (36x32, 6 frames)
        private static Sprite GetStormCloudSprite() => GetOrBuildSprite("StormCloud", 36, (x, y, s, f) =>
        {
            Vector2 p = new Vector2(x, y);
            float d1 = Vector2.Distance(p, new Vector2(11f, 17f)) / 7.5f;
            float d2 = Vector2.Distance(p, new Vector2(18f, 20f)) / 8.8f;
            float d3 = Vector2.Distance(p, new Vector2(25f, 17f)) / 7.2f;
            float d4 = Vector2.Distance(p, new Vector2(18f, 13f)) / 9.5f;
            float d = Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4));
            if (d > 1f || y < 7) return Color.clear;

            // Internal lightning flash veins inside cloud belly
            bool flash = (Fi(f) % 2 == 0) && Mathf.Abs(x - (16 + (y % 4) - f)) <= 1 && y < 18;
            if (flash) return Color.white;
            if (y < 12 && ((x + Fi(f)) % 3 == 0)) return new Color(1f, 0.90f, 0.28f);
            if (y > 22) return new Color(0.52f, 0.58f, 0.74f);
            if (y > 16) return new Color(0.32f, 0.36f, 0.52f);
            return new Color(0.18f, 0.20f, 0.32f);
        });

        // 4. Braided Thorny Jungle Vine Segment with Leaf Sprouts (28x28, 6 frames)
        private static Sprite GetVineSegmentSprite() => GetOrBuildSprite("VineSeg", 28, (x, y, s, f) =>
        {
            int waveY = 14 + Mathf.RoundToInt(Mathf.Sin((x + f * 2) * 0.45f) * 2.2f);
            int dy = y - waveY;

            // Thorns & mini leaf sprouts along the vine
            bool thorn = (x % 7 == 3) && (dy >= 3 && dy <= 6);
            if (thorn) return dy == 6 ? Color.white : new Color(0.65f, 0.98f, 0.45f);

            if (Mathf.Abs(dy) > 3 || x < 2 || x > 25) return Color.clear;
            if (dy == 0 && (x + Fi(f)) % 4 == 0) return new Color(0.75f, 1f, 0.65f);
            if (dy >= 1) return new Color(0.38f, 0.92f, 0.42f);
            if (dy == 0) return new Color(0.22f, 0.74f, 0.30f);
            return new Color(0.12f, 0.46f, 0.18f);
        });

        // 5. Serrated Scimitar Razor Leaf with Veins & Specular Glint (28x28, 6 frames)
        private static Sprite GetLeafBladeSprite() => GetOrBuildSprite("LeafBlade", 28, (x, y, s, f) =>
        {
            float dx = (x - 13.5f) / 11.5f;
            float curveY = (y - 13.5f - Mathf.Sin(dx * 2.2f) * 2.0f) / (5.2f * (1f - Mathf.Abs(dx) * 0.65f + 0.1f));
            float d = dx * dx + curveY * curveY;
            if (d > 1f) return Color.clear;

            // Central midrib vein + diagonal lateral veins
            bool midrib = Mathf.Abs(curveY) < 0.18f;
            bool lateralVein = ((x + Mathf.Abs(y - 14)) % 5 == 0) && Mathf.Abs(curveY) < 0.72f;
            bool glint = Mathf.Abs(x - (6 + f * 3)) <= 1;

            if (glint || midrib) return new Color(0.92f, 1f, 0.85f);
            if (lateralVein) return new Color(0.58f, 0.98f, 0.55f);
            return curveY > 0f ? new Color(0.32f, 0.88f, 0.38f) : new Color(0.16f, 0.58f, 0.24f);
        });

        // 6. Bubbling Toxic Venom Glob with Acid Spores (28x28, 6 frames)
        private static Sprite GetSludgeGlobSprite() => GetOrBuildSprite("SludgeGlob", 28, (x, y, s, f) =>
        {
            Vector2 p = new Vector2(x, y);
            float mainD = Vector2.Distance(p, new Vector2(13.5f, 12.5f)) / 9.2f;
            float b1 = Vector2.Distance(p, new Vector2(9f + (f % 3), 18f + (f % 2))) / 4.2f;
            float b2 = Vector2.Distance(p, new Vector2(18f - (f % 2), 17f + ((f + 1) % 3))) / 4.0f;
            float d = Mathf.Min(mainD, Mathf.Min(b1, b2));
            if (d > 1f) return Color.clear;

            // Toxic acid eyes / popping gas pockets
            if ((Vector2.Distance(p, new Vector2(10f, 15f)) < 2.1f) || (Vector2.Distance(p, new Vector2(17f, 14f)) < 1.8f))
                return new Color(0.68f, 1f, 0.32f);
            if (x >= 9 && x <= 12 && y >= 17 && y <= 19) return new Color(0.95f, 0.72f, 1f);
            if (d < 0.55f) return new Color(0.76f, 0.32f, 0.95f);
            if (d < 0.82f) return new Color(0.52f, 0.16f, 0.72f);
            return new Color(0.32f, 0.08f, 0.46f);
        });

        // 7. Bubbling Toxic Swamp Pool with Concentric Acid Ripples (36x32, 6 frames)
        private static Sprite GetSludgePoolSprite() => GetOrBuildSprite("SludgePool", 36, (x, y, s, f) =>
        {
            float dx = (x - 17.5f) / 15.5f, dy = (y - 15.5f) / 9.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float wobble = Mathf.Sin(Mathf.Atan2(dy, dx) * 6f + f) * 0.06f;
            if (r + wobble > 1f) return Color.clear;

            bool ripple = Mathf.Abs(((r * 10f - f * 0.8f) % 3.5f) - 1.5f) < 0.38f;
            if (ripple) return new Color(0.72f, 0.98f, 0.38f, 0.9f);
            if (r > 0.78f) return new Color(0.36f, 0.10f, 0.50f, 0.92f);
            return ((x + y) % 2 == 0) ? new Color(0.58f, 0.20f, 0.78f, 0.88f) : new Color(0.46f, 0.14f, 0.64f, 0.88f);
        }, false);

        // 8. Helical Solar Plasma Laser Segment (32x32, 6 frames)
        private static Sprite GetSolarBeamSegmentSprite() => GetOrBuildSprite("SolarBeam", 32, (x, y, s, f) =>
        {
            int dy = Mathf.Abs(y - 16);
            int helixY = 16 + Mathf.RoundToInt(Mathf.Sin((x + f * 3) * 0.45f) * 7f);
            int dHelix = Mathf.Abs(y - helixY);

            if (dHelix <= 1) return Color.white;
            if (dy > 9) return Color.clear;
            if (dy <= 2) return Color.white;
            if (dy <= 5) return new Color(1f, 0.98f, 0.45f);
            if (dy <= 7) return new Color(0.58f, 1f, 0.42f);
            return new Color(0.22f, 0.82f, 0.35f, 0.85f);
        });

        // 9. Sculpted 4-Knuckle Flame Gauntlet Fist (32x32, 6 frames)
        private static Sprite GetFireFistSprite() => GetOrBuildSprite("FireFist", 32, (x, y, s, f) =>
        {
            // Clenched fist head on right (x: 12..28, y: 8..24) + trailing fire mane on left (x: 2..14)
            float dx = (x - 19f) / 8.5f, dy = (y - 15.5f) / 7.5f;
            float fistD = dx * dx + dy * dy;

            // Knuckle ridges on the front of the fist
            bool knuckle = x >= 22 && x <= 26 && (y == 10 || y == 14 || y == 18 || y == 21);
            if (knuckle && fistD <= 1.05f) return Color.white;

            // Trailing flame tongues licking left
            int flameWave = Mathf.RoundToInt(Mathf.Sin((y + f * 2) * 0.6f) * 3f);
            bool inTrail = x >= 3 + flameWave && x <= 16 && Mathf.Abs(y - 16) <= (x - 1) / 2;

            if (fistD > 1f && !inTrail) return Color.clear;
            if (fistD < 0.38f) return Color.white;
            if (fistD < 0.68f) return new Color(1f, 0.92f, 0.25f);
            if (fistD <= 1f) return ((x + y) % 2 == 0) ? new Color(1f, 0.48f, 0.12f) : new Color(0.92f, 0.22f, 0.10f);
            return (x + Fi(f)) % 2 == 0 ? new Color(1f, 0.62f, 0.15f) : new Color(0.85f, 0.18f, 0.08f);
        });

        // 10. Roiling Spiral Fireball with Licking Flame Tongues (28x28, 6 frames)
        private static Sprite GetFireOrbSprite() => GetOrBuildSprite("FireOrb", 28, (x, y, s, f) =>
        {
            float dx = x - 13.5f, dy = y - 12.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float tongue = Mathf.Sin(ang * 5f - f * 1.1f) * 2.2f + (y > 14 ? (y - 14) * 0.25f : 0f);
            float effR = r - tongue;

            if (effR > 9.5f) return Color.clear;
            if (effR < 3.0f) return Color.white;
            if (effR < 5.5f) return new Color(1f, 0.94f, 0.28f);
            if (effR < 7.8f) return new Color(1f, 0.46f, 0.12f);
            return new Color(0.78f, 0.14f, 0.08f);
        });

        // 11. 6-Talon Dragon Claw X-Slash (36x36, 6 frames)
        private static Sprite GetClawXSprite() => GetOrBuildSprite("ClawX", 36, (x, y, s, f) =>
        {
            if (x < 3 || x > 32 || y < 3 || y > 32) return Color.clear;
            // 3 parallel diagonal slashes each way (-5, 0, +5 offset)
            int d1 = Mathf.Min(Mathf.Abs((x - y)), Mathf.Min(Mathf.Abs((x - y) - 6), Mathf.Abs((x - y) + 6)));
            int d2 = Mathf.Min(Mathf.Abs((x - (35 - y))), Mathf.Min(Mathf.Abs((x - (35 - y)) - 6), Mathf.Abs((x - (35 - y)) + 6)));
            int m = Mathf.Min(d1, d2);
            if (m > 2) return Color.clear;
            if (m == 0) return Color.white;
            if (m == 1) return ((x + y + Fi(f)) % 3 == 0) ? new Color(0.65f, 1f, 0.88f) : new Color(0.18f, 0.95f, 0.68f);
            return new Color(0.08f, 0.55f, 0.42f);
        });

        // 12. Twin Sonic Wind Crescent Scimitar (32x32, 6 frames)
        private static Sprite GetWindCrescentSprite() => GetOrBuildSprite("WindCrescent", 32, (x, y, s, f) =>
        {
            float dx = x - 9f, dy = y - 15.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            bool mainBlade = x >= 10 && r >= 10.5f && r <= 14.5f;
            bool innerEcho = x >= 7 && r >= 6.5f && r <= 8.5f && ((y + Fi(f)) % 2 == 0);
            if (!mainBlade && !innerEcho) return Color.clear;
            if (r >= 12.2f && r <= 13.4f) return Color.white;
            return new Color(0.52f, 0.96f, 1f);
        });

        // 13. Iridescent Multi-Ring Water Prison Sphere (32x32, 6 frames)
        private static Sprite GetBubblePrisonSprite() => GetOrBuildSprite("BubblePrison", 32, (x, y, s, f) =>
        {
            float dx = x - 15.5f, dy = y - 15.5f;
            float wobble = Mathf.Sin(Mathf.Atan2(dy, dx) * 4f + f) * 0.6f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) + wobble;
            if (r > 14.2f) return Color.clear;

            // Curved window highlight in upper-left & cyan rim in lower-right
            bool spec = (x >= 8 && x <= 13 && y >= 19 && y <= 24 && r >= 7f && r <= 11.5f);
            if (spec) return Color.white;
            if (r >= 12.2f) return new Color(0.55f, 0.94f, 1f, 0.95f);
            if (r >= 10.6f) return new Color(0.22f, 0.75f, 0.98f, 0.85f);
            return new Color(0.18f, 0.62f, 0.95f, 0.30f);
        });

        // 14. Curling Hokusai Tsunami Wave Crest with White Foam Claws (36x32, 6 frames)
        private static Sprite GetWaveCrestSprite() => GetOrBuildSprite("WaveCrest", 36, (x, y, s, f) =>
        {
            if (x < 3 || x > 32 || y < 3 || y > 28) return Color.clear;
            float curve = 6f + (y * 0.65f) + Mathf.Sin((y + f * 2) * 0.35f) * 3.0f;
            if (x < curve - 6f || x > curve + 7f) return Color.clear;

            // White foam crest along leading edge & top curl
            if (x >= curve + 3f || (y >= 22 && x >= curve - 1f)) return Color.white;
            if (x >= curve) return new Color(0.52f, 0.92f, 1f);
            if (x >= curve - 3f) return new Color(0.18f, 0.66f, 0.96f);
            return new Color(0.10f, 0.38f, 0.78f);
        });

        // 15. Faceted 5-Pointed Celestial Swift Star (28x28, 6 frames)
        private static Sprite GetGoldStarSprite() => GetOrBuildSprite("GoldStar", 28, (x, y, s, f) =>
        {
            float dx = x - 13.5f, dy = y - 13.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + f * 0.2f;
            float starRadius = 6.5f + Mathf.Cos(a * 5f) * 5.2f;
            if (r > starRadius) return Color.clear;
            if (r < 3.2f) return Color.white;
            bool lightFacet = Mathf.Sin(a * 5f) > 0f;
            return lightFacet ? new Color(1f, 0.94f, 0.36f) : new Color(0.95f, 0.68f, 0.14f);
        });

        // 16. Hexagonal Prismatic Light Screen Barrier (36x32, 6 frames)
        private static Sprite GetCrystalWallSprite() => GetOrBuildSprite("CrystalWall", 36, (x, y, s, f) =>
        {
            int dx = Mathf.Abs(x - 18), dy = Mathf.Abs(y - 16);
            if (dx > 14 || dy > 9 || (dx + dy > 19)) return Color.clear;
            bool border = (dx == 14 || dy == 9 || dx + dy == 19);
            bool sheen = Mathf.Abs((x + y) - (16 + f * 4)) <= 2;
            if (border || sheen) return Color.white;
            return ((x / 4 + y / 4) % 2 == 0)
                ? new Color(1f, 0.58f, 0.95f, 0.88f)
                : new Color(0.75f, 0.38f, 0.92f, 0.85f);
        });

        // 17. Spiral Galaxy & Black-Hole Accretion Supernova (40x40, 6 frames)
        private static Sprite GetSupernovaOrbSprite() => GetOrBuildSprite("Supernova", 40, (x, y, s, f) =>
        {
            float dx = x - 19.5f, dy = y - 19.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 18.2f) return Color.clear;
            if (r < 4.5f) return new Color(0.08f, 0.02f, 0.16f); // Black hole event horizon
            if (r < 6.2f) return Color.white;                    // Photon ring

            float a = Mathf.Atan2(dy, dx) + r * 0.28f - f * 0.55f;
            float arm = Mathf.Sin(a * 3f);
            if (arm > 0.25f)
                return r < 11f ? new Color(1f, 0.48f, 0.95f) : new Color(0.42f, 0.95f, 1f);
            return r < 13f ? new Color(0.45f, 0.16f, 0.68f, 0.85f) : Color.clear;
        });

        // 18. Twin Katana Leaf-Blade Cross Slash (+) (36x36, 6 frames)
        private static Sprite GetCrossSlashSprite() => GetOrBuildSprite("CrossSlash", 36, (x, y, s, f) =>
        {
            int dx = Mathf.Abs(x - 18), dy = Mathf.Abs(y - 18);
            float taperX = Mathf.Max(0.5f, 3.2f * (1f - dy / 16f));
            float taperY = Mathf.Max(0.5f, 3.2f * (1f - dx / 16f));
            bool inVert = dy <= 15 && dx <= taperX;
            bool inHoriz = dx <= 15 && dy <= taperY;
            if (!inVert && !inHoriz) return Color.clear;
            if (dx == 0 || dy == 0) return Color.white;
            if (dx == 1 || dy == 1) return new Color(0.65f, 1f, 0.68f);
            return new Color(0.20f, 0.88f, 0.36f);
        });

        // 19. Supersonic Armored Acorn Bullet Seed (24x24, 6 frames)
        private static Sprite GetBulletSeedSprite() => GetOrBuildSprite("BulletSeed", 24, (x, y, s, f) =>
        {
            float dx = (x - 11.5f) / 7.5f, dy = (y - 11.5f) / 5.5f;
            float d = dx * dx + dy * dy;
            if (d > 1f) return Color.clear;
            if (x < 9) return ((x + y) % 2 == 0) ? new Color(0.48f, 0.30f, 0.14f) : new Color(0.34f, 0.20f, 0.08f);
            if (d < 0.35f) return Color.white;
            return d < 0.72f ? new Color(1f, 0.94f, 0.35f) : new Color(0.52f, 0.88f, 0.22f);
        });

        // 20. Gnarled Subterranean Briar Root Eruption (32x36, 6 frames)
        private static Sprite GetThornRootSprite() => GetOrBuildSprite("ThornRoot", 32, (x, y, s, f) =>
        {
            int wave = Mathf.RoundToInt(Mathf.Sin((y + f) * 0.35f) * 2.5f);
            int halfW = Mathf.Max(1, (30 - y) / 4);
            int dx = Mathf.Abs(x - (16 + wave));
            bool thorn = (y % 6 == 2) && dx >= halfW && dx <= halfW + 3;
            if (thorn) return new Color(0.85f, 0.98f, 0.45f);
            if (dx > halfW || y < 2 || y > 30) return Color.clear;
            if (y < 6) return new Color(0.45f, 0.30f, 0.16f); // Earth rubble at base
            if (dx == 0) return new Color(0.52f, 0.88f, 0.28f);
            return dx < halfW - 1 ? new Color(0.34f, 0.64f, 0.18f) : new Color(0.20f, 0.42f, 0.12f);
        });

        // 21. Blazing Phoenix Wing Feather with Barbed Vanes (28x28, 6 frames)
        private static Sprite GetFeatherSprite() => GetOrBuildSprite("FireFeather", 28, (x, y, s, f) =>
        {
            float dx = (x - 13.5f) / 10.5f;
            float dy = (y - 13.5f - Mathf.Sin(dx * 2f) * 1.5f) / (4.2f * (1f - Mathf.Abs(dx) * 0.5f));
            float d = dx * dx + dy * dy;
            if (d > 1f) return Color.clear;
            if (Mathf.Abs(dy) < 0.2f) return Color.white; // Central quill
            if ((x + Fi(f)) % 3 == 0 && d > 0.65f) return new Color(1f, 0.92f, 0.25f);
            return x > 15 ? new Color(1f, 0.78f, 0.18f) : new Color(0.96f, 0.26f, 0.12f);
        });

        // 22. Splattering Earthen Mud Boulder with Pebbles (28x28, 6 frames)
        private static Sprite GetMudGlobSprite() => GetOrBuildSprite("MudGlob", 28, (x, y, s, f) =>
        {
            float dx = x - 13.5f, dy = y - 13.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Sin(Mathf.Atan2(dy, dx) * 5f + f) * 1.2f;
            if (r > 10.8f) return Color.clear;
            if ((x % 6 == 0) && (y % 5 == 0) && r < 7.5f) return new Color(0.75f, 0.62f, 0.45f);
            if (r < 5.5f) return new Color(0.58f, 0.40f, 0.22f);
            if (r < 8.8f) return new Color(0.42f, 0.28f, 0.14f);
            return new Color(0.26f, 0.16f, 0.08f);
        });

        // 23. Jagged Tectonic Earthquake Chasm with Glowing Vents (36x32, 6 frames)
        private static Sprite GetFissureCrackSprite() => GetOrBuildSprite("Fissure", 36, (x, y, s, f) =>
        {
            int zig = 18 + (((y / 3) % 2 == 0) ? (y % 3) - 1 : 2 - (y % 3)) * 2;
            int d = Mathf.Abs(x - zig);
            if (y < 2 || y > 30 || d > 5) return Color.clear;
            if (d == 0) return (Fi(f) % 2 == 0) ? new Color(0.42f, 0.90f, 1f) : new Color(0.10f, 0.06f, 0.04f);
            if (d <= 2) return new Color(0.16f, 0.10f, 0.06f);
            return new Color(0.52f, 0.36f, 0.20f);
        });

        // 24. Lucario Cobalt/Cyan Aura Gauntlet Fist (32x32, 6 frames)
        private static Sprite GetAuraFistSprite() => GetOrBuildSprite("AuraFist", 32, (x, y, s, f) =>
        {
            float dx = (x - 19f) / 8.5f, dy = (y - 15.5f) / 7.5f;
            float fistD = dx * dx + dy * dy;
            bool spike = (x >= 25 && x <= 29 && Mathf.Abs(y - 16) <= (29 - x));
            if (spike) return Color.white;

            int auraWave = Mathf.RoundToInt(Mathf.Cos((y + f * 2) * 0.55f) * 3f);
            bool trail = x >= 3 + auraWave && x <= 16 && Mathf.Abs(y - 16) <= 6;
            if (fistD > 1f && !trail) return Color.clear;
            if (fistD < 0.38f) return Color.white;
            if (fistD < 0.72f) return new Color(0.42f, 0.92f, 1f);
            return new Color(0.16f, 0.52f, 0.95f);
        });

        // 25. Two-Knobbled Cyan Bone Rush Staff with Aura Rings (36x36, 6 frames)
        private static Sprite GetBoneStaffSprite() => GetOrBuildSprite("BoneStaff", 36, (x, y, s, f) =>
        {
            bool shaft = (y >= 15 && y <= 20 && x >= 6 && x <= 29);
            float knobL1 = Vector2.Distance(new Vector2(x, y), new Vector2(6f, 14f));
            float knobL2 = Vector2.Distance(new Vector2(x, y), new Vector2(6f, 21f));
            float knobR1 = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 14f));
            float knobR2 = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 21f));
            bool knob = knobL1 <= 3.8f || knobL2 <= 3.8f || knobR1 <= 3.8f || knobR2 <= 3.8f;

            if (!shaft && !knob) return Color.clear;
            bool ring = ((x + Fi(f) * 2) % 6 == 0) && shaft;
            if (ring || y == 17 || y == 18) return Color.white;
            return new Color(0.32f, 0.90f, 1f);
        });

        // 26. Faceted 6-Pointed Diamond & Snowflake Ice Crystal (28x28, 6 frames)
        private static Sprite GetIceCrystalSprite() => GetOrBuildSprite("IceCrystal", 28, (x, y, s, f) =>
        {
            int dx = Mathf.Abs(x - 14), dy = Mathf.Abs(y - 14);
            bool diamond = (dx * 2 + dy <= 18) && (dx + dy * 2 <= 18);
            bool cross = (dx <= 1 && dy <= 11) || (dy <= 1 && dx <= 11) || (Mathf.Abs(dx - dy) <= 1 && dx <= 8);
            if (!diamond && !cross) return Color.clear;
            if (dx + dy <= 4 || (Fi(f) % 2 == 0 && (dx == 0 || dy == 0))) return Color.white;
            if (dx + dy <= 8) return new Color(0.68f, 0.96f, 1f);
            return new Color(0.28f, 0.76f, 0.98f);
        });

        // 27. Triple Glacier Spire Cluster Erupting from Ground (32x36, 6 frames)
        private static Sprite GetIceSpikePillarSprite() => GetOrBuildSprite("IceSpike", 32, (x, y, s, f) =>
        {
            if (y < 2 || y > 33) return Color.clear;
            // Central tall spire + left/right angled side shards
            int wCenter = Mathf.Max(1, (33 - y) / 3);
            bool inCenter = Mathf.Abs(x - 16) <= wCenter;

            int wSide = Mathf.Max(0, (24 - y) / 3);
            bool inLeft = y <= 24 && Mathf.Abs(x - (9 - y / 5)) <= wSide;
            bool inRight = y <= 24 && Mathf.Abs(x - (23 + y / 5)) <= wSide;

            if (!inCenter && !inLeft && !inRight) return Color.clear;
            bool sheen = ((x + y + Fi(f) * 2) % 7 == 0);
            if (sheen || x == 16) return Color.white;
            return (x < 16) ? new Color(0.65f, 0.95f, 1f) : new Color(0.26f, 0.68f, 0.95f);
        });

        // 28. Ornate Hexagonal Snowflake Magic Circle (44x44, 6 frames)
        private static Sprite GetSnowGlobeFieldSprite() => GetOrBuildSprite("SnowGlobe", 44, (x, y, s, f) =>
        {
            float dx = x - 21.5f, dy = y - 21.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 20.5f) return Color.clear;
            float a = Mathf.Atan2(dy, dx) + f * 0.1f;
            bool outerRing = Mathf.Abs(r - 19.2f) < 1.1f || Mathf.Abs(r - 12.0f) < 0.85f;
            bool hexSpoke = Mathf.Abs(Mathf.Sin(a * 3f)) < 0.14f;
            if (outerRing || hexSpoke) return Color.white;
            return new Color(0.38f, 0.86f, 1f, 0.28f);
        }, false);

        // 29. Serrated Anime Impact Shockwave Ring (32x32, 6 frames)
        private static Sprite GetShockwaveRingSprite() => GetOrBuildSprite("ShockRing", 32, (x, y, s, f) =>
        {
            float dx = x - 15.5f, dy = y - 15.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float teeth = Mathf.Sin(Mathf.Atan2(dy, dx) * 12f + f) * 1.1f;
            if (r + teeth > 14.5f || r + teeth < 10.5f) return Color.clear;
            return r + teeth > 12.5f ? Color.white : new Color(1f, 1f, 1f, 0.8f);
        }, false);

        // 30. Faceted Diamond Spark Particle (10x10, 6 frames)
        private static Sprite GetSparkParticleSprite() => GetOrBuildSprite("SparkParticle", 10, (x, y, s, f) =>
        {
            int dx = Mathf.Abs(x - 5), dy = Mathf.Abs(y - 5);
            if (dx + dy > 4) return Color.clear;
            return (dx + dy <= 1) ? Color.white : new Color(1f, 1f, 1f, 0.92f);
        });

        // 31. Cracked Elemental Ground Crater Decal (28x28, 6 frames)
        private static Sprite GetGroundDecalSprite() => GetOrBuildSprite("GroundDecal", 28, (x, y, s, f) =>
        {
            float dx = (x - 13.5f) / 11.5f, dy = (y - 13.5f) / 6.8f;
            float d = dx * dx + dy * dy;
            if (d > 1f) return Color.clear;
            return d > 0.72f ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 1f, 1f, 0.78f);
        }, false);

        // 32. Translucent Hexagonal Glacier Ice Cage (32x36, 6 frames)
        private static Sprite GetIceCageSprite() => GetOrBuildSprite("IceCage", 32, (x, y, s, f) =>
        {
            int maxW = Mathf.Max(3, (34 - y / 2) / 2);
            int dx = Mathf.Abs(x - 16);
            if (dx > maxW || y < 2 || y > 33) return Color.clear;
            bool border = dx == maxW || y == 2 || y == 33 || ((x + y + Fi(f)) % 7 == 0);
            return border ? new Color(0.90f, 0.99f, 1f, 0.92f) : new Color(0.38f, 0.86f, 1f, 0.42f);
        });

        // 33. Dialga Chrono Clockwork Ring (44x44, 6 frames)
        private static Sprite GetClockRingSprite() => GetOrBuildSprite("ClockRing", 44, (x, y, s, f) =>
        {
            float dx = x - 21.5f, dy = y - 21.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 20.5f) return Color.clear;
            float a = Mathf.Atan2(dy, dx);
            bool ring = Mathf.Abs(r - 19f) < 1.1f || Mathf.Abs(r - 13f) < 0.85f;
            bool tick = r > 15f && r < 19f && Mathf.Abs(Mathf.Sin(a * 6f)) < 0.14f;
            float handA = f * 0.52f;
            float handDot = Mathf.Abs(dx * Mathf.Sin(handA) - dy * Mathf.Cos(handA));
            bool hand = r < 16f && handDot < 0.9f;
            if (ring || tick || hand) return Color.white;
            return new Color(0.28f, 0.85f, 1f, 0.25f);
        }, false);

        // 34. Giratina / Darkrai Distortion Void Portal (40x40, 6 frames)
        private static Sprite GetVoidPortalSprite() => GetOrBuildSprite("VoidPortal", 40, (x, y, s, f) =>
        {
            float dx = x - 19.5f, dy = y - 19.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 18.5f) return Color.clear;
            float a = Mathf.Atan2(dy, dx) + r * 0.25f - f * 0.6f;
            if (r < 6f) return new Color(0.04f, 0.02f, 0.08f);
            bool rim = Mathf.Sin(a * 4f) > 0.1f;
            if (r > 15.5f) return rim ? new Color(0.85f, 0.18f, 0.35f) : Color.clear;
            return rim ? new Color(0.55f, 0.16f, 0.88f, 0.88f) : new Color(0.14f, 0.04f, 0.24f, 0.85f);
        }, false);

        // 35. Groudon Precipice Tectonic Stone Blade (32x36, 6 frames)
        private static Sprite GetTectonicBladeSprite() => GetOrBuildSprite("TectonicBlade", 32, (x, y, s, f) =>
        {
            if (y < 2 || y > 34) return Color.clear;
            int halfW = Mathf.Max(1, (34 - y) / 3);
            int dx = Mathf.Abs(x - 16);
            if (dx > halfW) return Color.clear;
            if (y < 7) return new Color(1f, 0.45f, 0.12f); // Magma crack at base
            if (dx == 0 || (Fi(f) % 2 == 0 && y > 26)) return new Color(1f, 0.85f, 0.35f);
            return x < 16 ? new Color(0.88f, 0.28f, 0.16f) : new Color(0.45f, 0.14f, 0.10f);
        });

        // 36. Jirachi / Kyogre / Arceus / Sylveon Cosmic Star Orb (32x32, 6 frames)
        private static Sprite GetDoomStarSprite() => GetOrBuildSprite("DoomStar", 32, (x, y, s, f) =>
        {
            float dx = x - 15.5f, dy = y - 15.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + f * 0.28f;
            float flare = 7.5f + Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 4f) * 7.5f;
            if (r > flare) return Color.clear;
            if (r < 4.5f) return Color.white;
            return r < 8.5f ? new Color(1f, 0.95f, 0.48f) : new Color(0.65f, 0.92f, 1f);
        });

        // 37. Decidueye / Dragapult Phantom Spirit Arrow (32x24, 6 frames)
        private static Sprite GetShadowArrowSprite() => GetOrBuildSprite("ShadowArrow", 32, (x, y, s, f) =>
        {
            bool shaft = (y >= 14 && y <= 17 && x >= 5 && x <= 23);
            bool head = (x >= 20 && x <= 29 && Mathf.Abs(y - 16) <= (29 - x));
            bool fletch = (x >= 3 && x <= 8 && Mathf.Abs(y - 16) >= 2 && Mathf.Abs(y - 16) <= (9 - x));
            if (!shaft && !head && !fletch) return Color.clear;
            if (head && x >= 25) return Color.white;
            return (x + Fi(f)) % 2 == 0 ? new Color(0.55f, 0.98f, 0.68f) : new Color(0.32f, 0.72f, 0.92f);
        });

        // 38. Venusaur / Sylveon Cherry Blossom Petal Vortex (28x28, 6 frames)
        private static Sprite GetPetalBlossomSprite() => GetOrBuildSprite("PetalBlossom", 28, (x, y, s, f) =>
        {
            float dx = x - 13.5f, dy = y - 13.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) + f * 0.3f;
            float petalR = 6.5f + Mathf.Cos(a * 5f) * 4.5f;
            if (r > petalR) return Color.clear;
            if (r < 3.2f) return new Color(1f, 0.95f, 0.42f);
            return r < 6.5f ? Color.white : new Color(1f, 0.52f, 0.82f);
        });
    }

    public class PMDAnimatedPixelVfx : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private List<Sprite> _frames;
        private float _baseScale = 1f;
        private float _duration = 0.3f;
        private float _elapsed;
        private bool _popScale = true;
        private Color _initialColor = Color.white;

        public void Initialize(SpriteRenderer sr, float scale, float duration, bool popScale = true, List<Sprite> frames = null)
        {
            _sr = sr;
            _frames = frames;
            _baseScale = scale;
            _duration = Mathf.Max(0.05f, duration);
            _popScale = popScale;
            if (_sr != null)
                _initialColor = _sr.color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            if (_sr != null && _frames != null && _frames.Count > 1)
            {
                // Play the full 20-frame strip across the effect's lifetime (at least 20 FPS, at most 60 FPS);
                // long-lived effects (pools, domains) loop the strip.
                float fps = Mathf.Clamp(_frames.Count / Mathf.Max(0.05f, _duration * 0.9f), 20f, 60f);
                int idx = Mathf.FloorToInt(_elapsed * fps) % _frames.Count;
                _sr.sprite = _frames[idx];
            }

            // Beautify: 1-frame white hit flash on spawn (only for popping skill sprites)
            if (_sr != null && _popScale && t <= 0.65f)
            {
                _sr.color = _elapsed < 0.034f
                    ? new Color(1f, 1f, 1f, _initialColor.a)
                    : _initialColor;
            }

            if (_popScale)
            {
                float s = t < 0.2f
                    ? Mathf.Lerp(0.45f, 1.18f, t / 0.2f)
                    : Mathf.Lerp(1.18f, 0.92f, (t - 0.2f) / 0.8f);
                transform.localScale = Vector3.one * (_baseScale * s);
            }

            if (_sr != null && t > 0.65f)
            {
                // Stepped pixel fade (3 levels + dither blink) instead of a smooth alpha gradient
                float k = (t - 0.65f) / 0.35f;
                float step = k < 0.34f ? 0.85f : (k < 0.67f ? 0.55f : 0.25f);
                bool blink = k > 0.67f && (Mathf.FloorToInt(_elapsed * 30f) & 1) == 1;
                Color c = _initialColor;
                c.a = blink ? 0f : _initialColor.a * step;
                _sr.color = c;
            }
        }
    }

    public class PMDExpandingRingVfx : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _startScale;
        private float _endScale;
        private float _duration;
        private float _elapsed;
        private Color _baseCol;

        public void Initialize(SpriteRenderer sr, float startScale, float endScale, float duration)
        {
            _sr = sr;
            _startScale = startScale;
            _endScale = endScale;
            _duration = Mathf.Max(0.05f, duration);
            _baseCol = sr != null ? sr.color : Color.white;
            transform.localScale = Vector3.one * _startScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float ease = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, ease);
            if (_sr != null)
            {
                Color c = _baseCol;
                c.a = (1f - t) * 0.9f;
                _sr.color = c;
            }
        }
    }

    public class PMDPhysicsParticleVfx : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Vector2 _vel;
        private float _zHeight;
        private float _zVel;
        private float _duration;
        private float _elapsed;
        private Color _baseCol;

        public void Initialize(SpriteRenderer sr, Vector2 velocity, float jumpVel, float duration)
        {
            _sr = sr;
            _vel = velocity;
            _zVel = jumpVel;
            _duration = Mathf.Max(0.1f, duration);
            _baseCol = sr != null ? sr.color : Color.white;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;
            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            _zVel -= 14f * dt;
            _zHeight += _zVel * dt;
            if (_zHeight < 0f)
            {
                _zHeight = 0f;
                _zVel = Mathf.Abs(_zVel) * 0.45f;
                _vel *= 0.7f;
            }

            transform.position += new Vector3(_vel.x * dt, (_vel.y + _zVel * 0.35f) * dt, 0f);
            transform.Rotate(0f, 0f, 360f * dt);

            if (_sr != null)
            {
                float t = _elapsed / _duration;
                Color c = _baseCol;
                c.a = 1f - t * t;
                _sr.color = c;
            }
        }
    }
}
