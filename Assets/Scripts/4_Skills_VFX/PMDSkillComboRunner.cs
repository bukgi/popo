using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Executes 14 distinct canonical Pokémon skill patterns (Chain Lightning, Multi-Slash Combos,
    /// Continuous Piercing Laser Beams, High-Speed Ram Dashes, Gravity-Pulling Whirlpools/Vortices,
    /// Sky-Fall Thunderbolts/Blizzards, Wide Tsunami Tidal Waves, and Multi-Ring Volcanic Novas).
    /// </summary>
    public static class PMDSkillComboRunner
    {
        public static void ExecuteSkillPattern(
            MonoBehaviour runner,
            Transform caster,
            PMDSkillData skill,
            Vector2 aimDir,
            bool isPlayerTeam)
        {
            if (runner == null || caster == null || skill == null)
                return;

            Vector2 dir = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector2.down;

            if (isPlayerTeam && PMDBespokeSkillEngine.TryExecuteUniqueMove(runner, caster, skill, dir))
                return;

            if (skill.healPercent > 0f && isPlayerTeam && PMDPlayerController.Instance != null)
            {
                float healAmt = Mathf.Round(PMDPlayerController.Instance.MaxHp * skill.healPercent);
                PMDPlayerController.Instance.Heal(healAmt);
            }

            switch (skill.pattern)
            {
                case PMDSkillPattern.SingleStrike:
                    runner.StartCoroutine(RunSingleStrike(caster, skill, dir));
                    break;

                case PMDSkillPattern.MultiSlashCombo:
                    runner.StartCoroutine(RunMultiSlashCombo(caster, skill, dir));
                    break;

                case PMDSkillPattern.ChainLightningArc:
                    runner.StartCoroutine(RunChainLightningArc(caster, skill, dir));
                    break;

                case PMDSkillPattern.StraightVolley:
                case PMDSkillPattern.HomingProjectile:
                case PMDSkillPattern.BoomerangVolley:
                case PMDSkillPattern.SpiralOrbitalShot:
                case PMDSkillPattern.ClusterBombLob:
                    PMDAudio.Play(PMDSfx.Projectile);
                    PMDProjectile.SpawnProjectileVolley(skill, caster.position, dir, isPlayerTeam, caster);
                    break;

                case PMDSkillPattern.ContinuousBeamLaser:
                    runner.StartCoroutine(RunContinuousBeamLaser(caster, skill, dir));
                    break;

                case PMDSkillPattern.HighSpeedRamDash:
                    runner.StartCoroutine(RunHighSpeedRamDash(caster, skill, dir));
                    break;

                case PMDSkillPattern.VortexPullWhirlpool:
                    runner.StartCoroutine(RunVortexPullWhirlpool(caster, skill, dir));
                    break;

                case PMDSkillPattern.TargetedSkyStrike:
                    runner.StartCoroutine(RunTargetedSkyStrike(caster, skill, dir));
                    break;

                case PMDSkillPattern.SweepingTidalWave:
                    runner.StartCoroutine(RunSweepingTidalWave(caster, skill, dir));
                    break;

                case PMDSkillPattern.ExpandingNovaStorm:
                    runner.StartCoroutine(RunExpandingNovaStorm(caster, skill));
                    break;

                default:
                    runner.StartCoroutine(RunLinearShockwave(runner, skill, caster.position, dir, isPlayerTeam));
                    break;
            }
        }

        // 1. Heavy Double-Impact Strike (Fire Fang, Ice Fang, Rock Smash, Force Palm, Zen Headbutt)
        private static IEnumerator RunSingleStrike(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Melee);
            Vector3 strikePos = caster.position + (Vector3)(dir * 1.15f);
            PMDVfxPlayer.SpawnVfx(skill.vfxSheet, strikePos, skill.vfxScale, 28f);
            DamageEnemiesInRadius(strikePos, skill.range * 0.85f, skill.damage * 0.65f, dir * 1.3f, skill);

            yield return new WaitForSeconds(0.11f);
            if (caster == null) yield break;

            Vector3 secondPos = caster.position + (Vector3)(dir * 1.55f);
            string secondSheet = !string.IsNullOrEmpty(skill.hitVfxSheet) ? skill.hitVfxSheet : skill.vfxSheet;
            PMDVfxPlayer.SpawnVfx(secondSheet, secondPos, skill.vfxScale * 1.2f, 28f);
            DamageEnemiesInRadius(secondPos, skill.range, skill.damage * 0.55f, dir * 1.8f, skill);
        }

        // 2. 3-Hit Crescent Slash / Whip Combo (Vine Whip, Dragon Claw, Aqua Tail, Leaf Blade, Slash)
        private static IEnumerator RunMultiSlashCombo(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            float[] angles = { -38f, 38f, 0f };
            for (int i = 0; i < angles.Length; i++)
            {
                if (caster == null) yield break;
                PMDAudio.Play(PMDSfx.Melee, 0.85f);

                Vector2 swingDir = PMDProjectile.RotateVector2(dir, angles[i]);
                float dist = (i == 2) ? 1.45f : 1.15f;
                Vector3 pos = caster.position + (Vector3)(swingDir * dist);

                string sheet = (i == 2 && !string.IsNullOrEmpty(skill.secondaryVfxSheet)) ? skill.secondaryVfxSheet : skill.vfxSheet;
                PMDVfxPlayer.SpawnVfx(sheet, pos, skill.vfxScale * (i == 2 ? 1.25f : 1.0f), 30f);
                DamageEnemiesInRadius(pos, skill.range * 0.8f, skill.damage * 0.45f, swingDir, skill);

                yield return new WaitForSeconds(0.075f);
            }
        }

        // 3. Jumping Chain Lightning / Drain Vine (Thunder Shock, Giga Drain)
        private static IEnumerator RunChainLightningArc(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Projectile);
            Vector3 currentPos = caster.position + (Vector3)(dir * 1.1f);
            PMDVfxPlayer.SpawnVfx(skill.vfxSheet, currentPos, skill.vfxScale, 30f);

            var visited = new HashSet<PMDWildPokemonAI>();
            const int maxJumps = 4;

            for (int jump = 0; jump < maxJumps; jump++)
            {
                PMDWildPokemonAI nextTarget = FindNearestUnvisitedWild(currentPos, 3.6f, visited);
                if (nextTarget == null)
                {
                    if (jump == 0)
                        DamageEnemiesInRadius(currentPos, skill.range, skill.damage, dir, skill);
                    yield break;
                }

                visited.Add(nextTarget);
                Vector3 targetPos = nextTarget.transform.position;

                // Spawn connecting arc VFX between previous point and new target
                Vector3 mid = Vector3.Lerp(currentPos, targetPos, 0.5f);
                if (!string.IsNullOrEmpty(skill.secondaryVfxSheet))
                    PMDVfxPlayer.SpawnVfx(skill.secondaryVfxSheet, mid, skill.vfxScale * 0.9f, 32f);

                PMDVfxPlayer.SpawnVfx(skill.vfxSheet, targetPos, skill.vfxScale * 1.15f, 30f);
                float jumpDmg = skill.damage * Mathf.Pow(0.85f, jump);
                nextTarget.TakeDamage(jumpDmg, ((Vector2)(targetPos - currentPos)).normalized, skill.elementColor);

                if (skill.lifestealPercent > 0f && PMDPlayerController.Instance != null)
                {
                    PMDPlayerController.Instance.Heal(Mathf.Max(3f, jumpDmg * skill.lifestealPercent));
                }

                currentPos = targetPos;
                yield return new WaitForSeconds(0.08f);
            }
        }

        // 4. Continuous Piercing Laser Beam (Solar Beam, Flamethrower, Ice Beam)
        private static IEnumerator RunContinuousBeamLaser(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
            PMDCameraFollow.Shake(0.22f, 0.18f);

            const int ticks = 3;
            int nodes = Mathf.Max(6, skill.waveSteps);
            float stepDist = Mathf.Max(0.85f, skill.range / nodes);

            for (int t = 0; t < ticks; t++)
            {
                if (caster == null) yield break;
                Vector3 start = caster.position;

                if (t == 0 && !string.IsNullOrEmpty(skill.ringVfxSheet))
                    PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, start, 1.3f, 28f);

                for (int n = 1; n <= nodes; n++)
                {
                    Vector3 beamNode = start + (Vector3)(dir * (n * stepDist));
                    string sheet = (n % 2 == 0 && !string.IsNullOrEmpty(skill.secondaryVfxSheet)) ? skill.secondaryVfxSheet : skill.vfxSheet;
                    PMDVfxPlayer.SpawnVfx(sheet, beamNode, skill.vfxScale * 1.05f, 30f);
                    DamageEnemiesInRadius(beamNode, 1.15f, skill.damage * 0.42f, dir * 0.4f, skill);
                }

                yield return new WaitForSeconds(0.09f);
            }
        }

        // 5. Full-Body High-Speed Elemental Ram Dash (Volt Tackle, Flare Blitz, Aqua Jet, Double-Edge, Flame Charge, Extreme Speed)
        private static IEnumerator RunHighSpeedRamDash(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Dash);
            PMDCameraFollow.Shake(0.18f, 0.16f);

            var mapGen = PMDOpenWorldMapGenerator.Instance;
            int steps = Mathf.Max(5, skill.waveSteps);
            float stepMove = Mathf.Clamp(skill.range / steps, 0.55f, 0.95f);

            for (int s = 0; s < steps; s++)
            {
                if (caster == null) yield break;

                Vector2 nextPos = (Vector2)caster.position + dir * stepMove;
                if (mapGen == null || mapGen.IsWorldPointWalkable(nextPos))
                {
                    caster.position = nextPos;
                }

                PMDVfxPlayer.SpawnVfx(skill.vfxSheet, caster.position, skill.vfxScale, 30f);
                if (!string.IsNullOrEmpty(skill.secondaryVfxSheet) && s % 2 == 0)
                    PMDVfxPlayer.SpawnVfx(skill.secondaryVfxSheet, caster.position, skill.vfxScale * 0.85f, 30f);

                DamageEnemiesInRadius(caster.position, 1.35f, skill.damage * 0.38f, dir * 1.4f, skill);
                yield return new WaitForSeconds(0.035f);
            }

            if (caster != null && !string.IsNullOrEmpty(skill.hitVfxSheet))
            {
                PMDVfxPlayer.SpawnVfx(skill.hitVfxSheet, caster.position + (Vector3)(dir * 0.6f), skill.vfxScale * 1.35f, 26f);
                DamageEnemiesInRadius(caster.position + (Vector3)(dir * 0.6f), 1.65f, skill.damage * 0.45f, dir * 1.8f, skill);
            }
        }

        // 6. Gravity-Pulling Whirlpool / Fire Spin / Psychic Vortex (Fire Spin, Whirlpool, Psychic, Leaf Storm)
        private static IEnumerator RunVortexPullWhirlpool(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
            Vector3 vortexCenter = caster.position + (Vector3)(dir * Mathf.Min(3.2f, skill.range * 0.55f));
            float pullRadius = Mathf.Max(3.2f, skill.range * 0.75f);

            const int pulses = 6;
            for (int p = 0; p < pulses; p++)
            {
                if (!string.IsNullOrEmpty(skill.ringVfxSheet) && p % 2 == 0)
                    PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, vortexCenter, skill.vfxScale * 1.35f, 26f);

                PMDVfxPlayer.SpawnVfx(skill.vfxSheet, vortexCenter, skill.vfxScale * 1.25f, 28f);

                // Orbiting satellite sparks around the vortex
                for (int k = 0; k < 3; k++)
                {
                    float ang = (p * 55f + k * 120f) * Mathf.Deg2Rad;
                    Vector3 rim = vortexCenter + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * 1.35f;
                    string satSheet = !string.IsNullOrEmpty(skill.secondaryVfxSheet) ? skill.secondaryVfxSheet : skill.vfxSheet;
                    PMDVfxPlayer.SpawnVfx(satSheet, rim, skill.vfxScale * 0.85f, 30f);
                }

                // Pull enemies inward toward vortexCenter and deal vortex tick damage
                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var wild = allWild[i];
                    if (wild == null || wild.IsFainted || wild.IsRecruited)
                        continue;

                    float d = Vector2.Distance(vortexCenter, wild.transform.position);
                    if (d <= pullRadius)
                    {
                        Vector2 toCenter = ((Vector2)(vortexCenter - wild.transform.position)).normalized;
                        wild.transform.position = Vector2.MoveTowards(wild.transform.position, vortexCenter, 0.55f);
                        wild.TakeDamage(skill.damage * 0.24f, toCenter * 0.2f, skill.elementColor);
                    }
                }

                yield return new WaitForSeconds(0.16f);
            }
        }

        // 7. Targeted Sky-Fall Strikes (Thunderbolt, Psystrike, Blizzard)
        private static IEnumerator RunTargetedSkyStrike(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.35f, 0.26f);

            Vector3 targetZone = caster.position + (Vector3)(dir * 2.8f);
            const int strikes = 7;

            for (int s = 0; s < strikes; s++)
            {
                Vector3 strikePoint = targetZone + (Vector3)(Random.insideUnitCircle * 1.85f);
                if (s == 0)
                    strikePoint = targetZone;

                // Descending sky trace
                Vector3 skyTop = strikePoint + new Vector3(0f, 2.2f, 0f);
                if (!string.IsNullOrEmpty(skill.secondaryVfxSheet))
                    PMDVfxPlayer.SpawnVfx(skill.secondaryVfxSheet, Vector3.Lerp(skyTop, strikePoint, 0.5f), skill.vfxScale * 0.95f, 32f);

                PMDVfxPlayer.SpawnVfx(skill.vfxSheet, strikePoint, skill.vfxScale * 1.35f, 26f);
                if (!string.IsNullOrEmpty(skill.ringVfxSheet) && s % 2 == 0)
                    PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, strikePoint, 1.15f, 28f);

                DamageEnemiesInRadius(strikePoint, 1.65f, skill.damage * 0.30f, dir * 0.5f, skill);
                yield return new WaitForSeconds(0.085f);
            }
        }

        // 8. Wide Horizontal Tsunami / Muddy Tidal Wave (Hydro Pump, Muddy Water)
        private static IEnumerator RunSweepingTidalWave(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.25f, 0.20f);

            Vector3 origin = caster.position;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            const int rows = 5;

            for (int r = 1; r <= rows; r++)
            {
                Vector3 rowCenter = origin + (Vector3)(dir * (r * 1.05f));
                for (int w = -2; w <= 2; w++)
                {
                    Vector3 cell = rowCenter + (Vector3)(perp * (w * 0.75f));
                    string sheet = (Mathf.Abs(w) == 2 && !string.IsNullOrEmpty(skill.secondaryVfxSheet)) ? skill.secondaryVfxSheet : skill.vfxSheet;
                    PMDVfxPlayer.SpawnVfx(sheet, cell, skill.vfxScale * 1.05f, 28f);
                    DamageEnemiesInRadius(cell, 0.95f, skill.damage * 0.32f, dir * 1.5f, skill);
                }
                yield return new WaitForSeconds(0.075f);
            }
        }

        // 9. 3-Ring Expanding Volcanic / Elemental Nova (Blast Burn, Last Resort, Overheat, Close Combat)
        private static IEnumerator RunExpandingNovaStorm(Transform caster, PMDSkillData skill)
        {
            PMDAudio.Play(PMDSfx.Ultimate);
            PMDCameraFollow.Shake(0.40f, 0.30f);

            Vector3 center = caster.position;
            float[] radii = { 1.35f, 2.55f, 3.75f };
            int[] counts = { 6, 8, 10 };

            for (int ring = 0; ring < radii.Length; ring++)
            {
                if (caster != null)
                    center = caster.position;

                if (!string.IsNullOrEmpty(skill.ringVfxSheet))
                    PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, center, skill.vfxScale * (1.1f + ring * 0.35f), 26f);

                int cnt = counts[ring];
                float rad = radii[ring];
                for (int i = 0; i < cnt; i++)
                {
                    float ang = (i * (360f / cnt) + ring * 22f) * Mathf.Deg2Rad;
                    Vector2 outDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    Vector3 pos = center + (Vector3)(outDir * rad);

                    string sheet = (ring % 2 == 1 && !string.IsNullOrEmpty(skill.secondaryVfxSheet)) ? skill.secondaryVfxSheet : skill.vfxSheet;
                    PMDVfxPlayer.SpawnVfx(sheet, pos, skill.vfxScale * 1.15f, 26f);
                    DamageEnemiesInRadius(pos, 1.35f, skill.damage * 0.36f, outDir * 1.2f, skill);
                }

                yield return new WaitForSeconds(0.14f);
            }
        }

        private static void DamageEnemiesInRadius(Vector3 center, float radius, float dmg, Vector2 knockDir, PMDSkillData skill)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = allWild.Count - 1; i >= 0; i--)
            {
                var wild = allWild[i];
                if (wild == null || wild.IsFainted || wild.IsRecruited)
                    continue;

                if (Vector2.Distance(center, wild.transform.position) <= radius)
                {
                    wild.TakeDamage(Mathf.Max(1f, dmg), knockDir, skill.elementColor);
                    if (!string.IsNullOrEmpty(skill.hitVfxSheet))
                    {
                        PMDVfxPlayer.SpawnVfx(skill.hitVfxSheet, wild.transform.position + new Vector3(0f, 0.2f, 0f), 1.2f, 28f);
                    }
                    if (skill.lifestealPercent > 0f && PMDPlayerController.Instance != null)
                    {
                        PMDPlayerController.Instance.Heal(Mathf.Max(2f, dmg * skill.lifestealPercent));
                    }
                }
            }
        }

        private static PMDWildPokemonAI FindNearestUnvisitedWild(Vector3 fromPos, float maxRadius, HashSet<PMDWildPokemonAI> visited)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            PMDWildPokemonAI best = null;
            float bestDist = maxRadius;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited || visited.Contains(w))
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

        public static IEnumerator RunLinearShockwave(
            MonoBehaviour runner,
            PMDSkillData skill,
            Vector3 origin,
            Vector2 dir,
            bool isPlayerTeam)
        {
            Vector2 normDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.down;
            int steps = Mathf.Max(3, skill.waveSteps);
            float stepDist = Mathf.Max(0.9f, skill.range / steps);

            for (int step = 1; step <= steps; step++)
            {
                Vector3 waveCenter = origin + (Vector3)(normDir * (step * stepDist));
                PMDVfxPlayer.SpawnVfx(skill.vfxSheet, waveCenter, skill.vfxScale, 28f);
                if (isPlayerTeam)
                {
                    DamageEnemiesInRadius(waveCenter, 1.35f, skill.damage * 0.6f, normDir, skill);
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        public static IEnumerator RunUltimateOrbitalStorm(
            Transform caster,
            PMDSkillData skill)
        {
            if (caster != null)
                yield return RunExpandingNovaStorm(caster, skill);
        }
    }
}
