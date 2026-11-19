using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDArchetypeMechanic
    {
        None = 0,

        // Nhóm 1: Tấn công thuần túy
        SingleBurstPierce = 1,       // Đâm xuyên giáp 1 điểm sát thương cực đại
        ConeSweep3Hit = 2,           // Quét hình nón 3 nhịp trước mặt
        PiercingBeam9m = 3,          // Tia năng lượng xuyên thẳng 9m
        GroundDoTPool = 4,           // Để lại vùng dung nham/axit trên mặt đất
        ChainBounce4 = 5,            // Đạn nảy bật giữa 4 mục tiêu
        BoomerangReturn = 6,         // Đạn boomerang bay đi và quay lại

        // Nhóm 2: Dịch chuyển & Cơ động
        InvulnDashSlash = 7,         // Lướt bất tử xuyên địch kèm chém
        BlinkBackstab = 8,           // Dịch chuyển tức thời ra sau lưng kẻ địch
        AerialLeapSlam = 9,          // Nhảy vọt lên không né đòn rồi giáng xuống
        BackstepKiteVolley = 10,     // Nhảy lùi giữ khoảng cách + bắn quạt 3 tia

        // Nhóm 3: Ngưng đọng & Khống chế
        StasisFreezeField = 11,      // Đóng băng không gian & kẻ địch trong vùng
        KnockUpStunImpact = 12,      // Hất tung + làm choáng cứng

        // Nhóm 4: Hỗ trợ & Cường hóa
        MarkVulnerability = 13,      // Đánh dấu nhận thêm +40% sát thương
        RegenCleanseBurst = 14,      // Hồi phục dần + tăng tốc độ chạy

        // Nhóm 5: Phòng thủ & Phản đòn
        ShieldBarrierPush = 15,      // Tạo khiên hấp thụ + đẩy tường chắn đạn
        ParryReflectStance = 16,     // Tư thế phản đòn triệt tiêu 100% & làm choáng

        // Nhóm 6: Triệu hồi & Kiến tạo
        SummonAutoTurret = 17,       // Triệu hồi trụ/linh thú tự bắn 5 phát
        ProximityMineTrap = 18,      // Chôn 3 bẫy mìn cảm biến nổ choáng

        // Nhóm 7: Không gian & Trọng lực
        GravityBlackHole = 19,       // Hố đen hút gom mọi quái vào tâm
        WallSlamRepulse = 20,        // Sóng đẩy văng cực mạnh + choáng
        ShadowCloneExplode = 21,     // Tạo 2 phân thân kẹp đánh và phát nổ

        // Nhóm 8: Ký sinh & Hiến tế
        LifeDrainTether = 22,        // Dây liên kết hút máu 5 lần về hồi HP
        BloodSacrificeReset = 23,    // Hiến 12% HP để Reset toàn bộ hồi chiêu & nổ lớn

        // Nhóm 9: Thao túng Thời gian
        TimeBombDelay = 24,          // Gắn bom đếm ngược tích sát thương rồi kích nổ
        TimeLoopSnapback = 25,       // Đánh dấu & giật ngược quái về vị trí cũ sau 2s

        // Nhóm 10 & 11: Biến hình, Liên hoàn & Kết liễu
        ElementalTransform = 26,     // Hóa nguyên tố tăng tốc & xả bão quanh thân
        MarkDetonateChain = 27,      // Kích nổ ấn gây lan diện rộng
        ExecuteLowHp = 28,           // Kết liễu ngay quái <24% HP & Reset hồi chiêu

        // Nhóm 12: Tối thượng & Lãnh địa
        DomainExpansion = 29,        // Khai triển lãnh địa 6s (-50% Hồi chiêu + sét/mưa tự đánh)
        ScreenCataclysm = 30         // Tụ lực ngắn rồi giáng thiên tai toàn màn hình
    }

    public enum PMDVisualAssetKind
    {
        Auto = 0,
        Bolt,
        Electroweb,
        StormCloud,
        VineSeg,
        LeafBlade,
        SludgeGlob,
        SludgePool,
        SolarBeam,
        FireFist,
        FireOrb,
        ClawX,
        WindCrescent,
        BubblePrison,
        WaveCrest,
        GoldStar,
        CrystalWall,
        Supernova,
        CrossSlash,
        BulletSeed,
        ThornRoot,
        FireFeather,
        MudGlob,
        Fissure,
        AuraFist,
        BoneStaff,
        IceCrystal,
        IceSpike,
        SnowGlobe,
        ClockRing,
        VoidPortal,
        TectonicBlade,
        DoomStar,
        ShadowArrow,
        PetalBlossom
    }

    /// <summary>
    /// Executes all 30 Tactical Combat Archetypes spanning the 12 Skill Groups with Startup Anticipation
    /// (Squash & Stretch), Hitstop/Screen Shake, Status Ailments, Turrets, Mines, Time Bombs, and Domains.
    /// </summary>
    public static class PMDArchetypeCombatEngine
    {
        public static bool TryExecute(MonoBehaviour runner, Transform caster, PMDSkillData skill, Vector2 aimDir)
        {
            if (runner == null || caster == null || skill == null || skill.archetype == PMDArchetypeMechanic.None)
                return false;

            Vector2 dir = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector2.down;
            if (skill.healPercent > 0f && PMDPlayerController.Instance != null)
            {
                PMDPlayerController.Instance.Heal(Mathf.Round(PMDPlayerController.Instance.MaxHp * skill.healPercent));
            }

            runner.StartCoroutine(RunArchetypeRoutine(caster, skill, dir));
            return true;
        }

        private static IEnumerator RunArchetypeRoutine(Transform caster, PMDSkillData skill, Vector2 dir)
        {
            // Phase 1: Startup Anticipation (Squash & Stretch + Charge Particles)
            Vector3 origScale = caster.localScale;
            caster.localScale = new Vector3(origScale.x * 1.18f, origScale.y * 0.84f, origScale.z);
            PMDBespokeSkillEngine.SpawnSkillCastBurst(caster, skill, dir);
            yield return new WaitForSeconds(0.065f);
            if (caster == null) yield break;
            caster.localScale = new Vector3(origScale.x * 0.90f, origScale.y * 1.15f, origScale.z);
            yield return new WaitForSeconds(0.035f);
            if (caster != null)
                caster.localScale = origScale;

            PMDPixelVfxSystem.SpawnElementalBurst(caster.position, skill.elementColor, skill.moveType, 6, 2.5f);

            Sprite sp = ResolveSprite(skill);
            Color tint = skill.elementColor;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            switch (skill.archetype)
            {
                case PMDArchetypeMechanic.SingleBurstPierce:
                {
                    PMDAudio.Play(PMDSfx.Melee);
                    Vector3 strikePos = caster.position + (Vector3)(dir * 1.55f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, strikePos, skill.vfxScale * 1.35f, 0.34f, angle, 105, tint);
                    PMDCameraFollow.Shake(0.22f, 0.20f);
                    HitAndMark(strikePos, 1.85f, skill.damage * 1.15f, dir * 2.2f, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.ConeSweep3Hit:
                {
                    float[] offsets = { -35f, 0f, 35f };
                    for (int i = 0; i < 3; i++)
                    {
                        if (caster == null) yield break;
                        PMDAudio.Play(PMDSfx.Melee, 0.8f);
                        Vector2 d = PMDProjectile.RotateVector2(dir, offsets[i]);
                        float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        Vector3 p = caster.position + (Vector3)(d * 1.65f);
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, p, skill.vfxScale * 1.25f, 0.26f, rot, 105, tint);
                        HitAndMark(p, 1.65f, skill.damage * 0.48f, d * 1.4f, skill, 0f);
                        yield return new WaitForSeconds(0.065f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.PiercingBeam9m:
                {
                    PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
                    PMDCameraFollow.Shake(0.28f, 0.22f);
                    for (int wave = 0; wave < 3; wave++)
                    {
                        if (caster == null) yield break;
                        for (int seg = 1; seg <= 12; seg++)
                        {
                            Vector3 p = caster.position + (Vector3)(dir * (seg * 0.7f));
                            PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, p, skill.vfxScale * 1.15f, 0.14f, angle, 105, tint);
                            HitAndMark(p, 1.15f, skill.damage * 0.26f, dir * 0.5f, skill, 0f);
                        }
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.GroundDoTPool:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    Vector3 center = caster.position + (Vector3)(dir * 3.2f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, center, 2.6f, 2.8f, 0f, -15, tint);
                    for (int t = 0; t < 7; t++)
                    {
                        PMDBespokeSkillEngine.SpawnParticleBurst(center + (Vector3)(Random.insideUnitCircle * 1.2f), tint, 5, 2.5f);
                        HitAndMark(center, 2.3f, skill.damage * 0.28f, Vector2.zero, skill, 0f);
                        yield return new WaitForSeconds(0.36f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.ChainBounce4:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    Vector3 prev = caster.position;
                    var visited = new HashSet<PMDWildPokemonAI>();
                    for (int j = 0; j < 4; j++)
                    {
                        var next = PMDBespokeSkillEngine.FindNearestWildPublic(prev, 4.6f, visited);
                        Vector3 nextPos = next != null ? next.transform.position : prev + (Vector3)(dir * 2.0f);
                        for (int s = 1; s <= 4; s++)
                        {
                            Vector3 p = Vector3.Lerp(prev, nextPos, s / 4f);
                            PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, p, 1.2f, 0.22f, angle, 105, tint);
                        }
                        if (next != null)
                        {
                            visited.Add(next);
                            next.TakeDamage(skill.damage * Mathf.Pow(0.85f, j), dir, tint);
                            prev = nextPos;
                        }
                        else
                        {
                            HitAndMark(nextPos, 1.5f, skill.damage * 0.8f, dir, skill, 0f);
                            break;
                        }
                        yield return new WaitForSeconds(0.07f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.BoomerangReturn:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    float[] fan = { -22f, 0f, 22f };
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 d = PMDProjectile.RotateVector2(dir, fan[i]);
                        if (PMDPlayerController.Instance != null)
                            PMDPlayerController.Instance.StartCoroutine(RunBoomerang(caster, sp, d, skill, tint));
                    }
                    break;
                }

                case PMDArchetypeMechanic.InvulnDashSlash:
                {
                    PMDAudio.Play(PMDSfx.Dash);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.GrantInvulnerability(0.45f);
                    for (int i = 0; i < 6; i++)
                    {
                        if (caster == null) yield break;
                        PMDBespokeSkillEngine.MoveCasterSafePublic(caster, dir * 0.72f);
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 1.35f, 0.22f, angle, 105, tint);
                        HitAndMark(caster.position, 1.5f, skill.damage * 0.36f, dir * 1.4f, skill, 0f);
                        yield return new WaitForSeconds(0.035f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.BlinkBackstab:
                {
                    PMDAudio.Play(PMDSfx.Dash);
                    var target = PMDBespokeSkillEngine.FindNearestWildPublic(caster.position + (Vector3)(dir * 3.2f), 6.0f, null);
                    Vector3 dest = target != null ? target.transform.position + (Vector3)(dir * 0.65f) : caster.position + (Vector3)(dir * 3.8f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 1.6f, 0.3f, 0f, 95, tint);
                    caster.position = dest;
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, dest, 2.0f, 0.35f, angle, 110, tint);
                    HitAndMark(dest, 2.1f, skill.damage * 1.2f, -dir, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.AerialLeapSlam:
                {
                    PMDAudio.Play(PMDSfx.Dash);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.GrantInvulnerability(0.55f);
                    Vector3 start = caster.position;
                    Vector3 land = start + (Vector3)(dir * 3.6f);
                    float t = 0f;
                    while (t < 1f && caster != null)
                    {
                        t += Time.deltaTime * 3.0f;
                        Vector3 p = Vector3.Lerp(start, land, t);
                        p.y += Mathf.Sin(t * Mathf.PI) * 2.1f;
                        caster.position = p;
                        yield return null;
                    }
                    if (caster != null)
                    {
                        caster.position = land;
                        PMDCameraFollow.Shake(0.35f, 0.28f);
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, land, 2.1f, 0.45f, 0f, 105, tint);
                        HitAndMark(land, 2.5f, skill.damage * 1.1f, dir * 1.8f, skill, 0f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.BackstepKiteVolley:
                {
                    PMDAudio.Play(PMDSfx.Dash);
                    PMDBespokeSkillEngine.MoveCasterSafePublic(caster, -dir * 1.6f);
                    float[] angs = { -20f, 0f, 20f };
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 d = PMDProjectile.RotateVector2(dir, angs[i]);
                        float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        if (PMDPlayerController.Instance != null)
                            PMDPlayerController.Instance.StartCoroutine(
                                PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, caster.position, caster.position + (Vector3)(d * 6.5f), 0.25f, skill, d, rot, 1.35f, 0.48f, tint));
                    }
                    break;
                }

                case PMDArchetypeMechanic.StasisFreezeField:
                {
                    PMDAudio.Play(PMDSfx.Ultimate);
                    Vector3 zone = caster.position + (Vector3)(dir * 2.6f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, zone, 3.2f, 2.2f, 0f, -12, tint);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(zone, w.transform.position) <= 3.2f)
                        {
                            w.ApplyStatusAilment(PMDStatusAilment.Frozen, 2.2f);
                            w.TakeDamage(skill.damage, Vector2.zero, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.KnockUpStunImpact:
                {
                    PMDAudio.Play(PMDSfx.Melee);
                    PMDCameraFollow.Shake(0.28f, 0.22f);
                    Vector3 spot = caster.position + (Vector3)(dir * 1.8f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, spot, 1.9f, 0.4f, 0f, 110, tint);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(spot, w.transform.position) <= 2.3f)
                        {
                            w.ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.6f);
                            w.TakeDamage(skill.damage, Vector2.up * 1.5f, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.MarkVulnerability:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    Vector3 dest = caster.position + (Vector3)(dir * 4.5f);
                    yield return PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, caster.position, dest, 0.22f, skill, dir, angle, 1.45f, 0.85f, tint);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = 0; i < allWild.Count; i++)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(dest, w.transform.position) <= 2.6f)
                            w.ApplyVulnerabilityMark(1.40f, 5.0f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.RegenCleanseBurst:
                {
                    PMDAudio.Play(PMDSfx.Heal);
                    if (PMDPlayerController.Instance != null)
                    {
                        PMDPlayerController.Instance.Heal(Mathf.Round(PMDPlayerController.Instance.MaxHp * 0.28f));
                        PMDPlayerController.Instance.GrantSpeedBuff(1.38f, 4.0f);
                    }
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 2.4f, 0.5f, 0f, 105, tint);
                    HitAndMark(caster.position, 2.6f, skill.damage * 0.85f, Vector2.zero, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.ShieldBarrierPush:
                {
                    PMDAudio.Play(PMDSfx.Gate);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.GrantShield(65f, 5.0f);
                    Vector3 wall = caster.position + (Vector3)(dir * 1.4f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, wall, 2.2f, 0.7f, angle + 90f, 105, tint);
                    HitAndMark(wall, 2.2f, skill.damage, dir * 2.5f, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.ParryReflectStance:
                {
                    PMDAudio.Play(PMDSfx.MenuOpen);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.GrantParryWindow(1.5f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 2.0f, 1.2f, 0f, 110, tint);
                    HitAndMark(caster.position, 2.2f, skill.damage * 0.7f, dir * 1.5f, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.SummonAutoTurret:
                {
                    PMDAudio.Play(PMDSfx.Gate);
                    Vector3 turretPos = caster.position + (Vector3)(dir * 1.6f);
                    var turretGo = PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, turretPos, 1.75f, 4.2f, 0f, 95, tint);
                    for (int shot = 0; shot < 5; shot++)
                    {
                        var target = PMDBespokeSkillEngine.FindNearestWildPublic(turretPos, 6.2f, null);
                        if (target != null)
                        {
                            Vector2 shotDir = ((Vector2)(target.transform.position - turretPos)).normalized;
                            float rot = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;
                            if (PMDPlayerController.Instance != null)
                                PMDPlayerController.Instance.StartCoroutine(
                                    PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, turretPos, target.transform.position, 0.20f, skill, shotDir, rot, 1.15f, 0.35f, tint));
                        }
                        if (turretGo != null) turretGo.transform.Rotate(0f, 0f, 45f);
                        yield return new WaitForSeconds(0.75f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.ProximityMineTrap:
                {
                    PMDAudio.Play(PMDSfx.Pickup);
                    Vector3 baseSpot = caster.position + (Vector3)(dir * 2.2f);
                    var mines = new List<(Vector3 pos, GameObject go, bool detonated)>();
                    for (int m = -1; m <= 1; m++)
                    {
                        Vector2 side = PMDProjectile.RotateVector2(dir, m * 35f);
                        Vector3 mPos = caster.position + (Vector3)(side * 2.4f);
                        mines.Add((mPos, PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, mPos, 1.35f, 4.0f, 0f, -10, tint), false));
                    }

                    for (int tick = 0; tick < 20; tick++)
                    {
                        for (int m = 0; m < mines.Count; m++)
                        {
                            if (mines[m].detonated) continue;
                            var near = PMDBespokeSkillEngine.FindNearestWildPublic(mines[m].pos, 1.55f, null);
                            if (near != null || tick == 19)
                            {
                                if (mines[m].go != null) Object.Destroy(mines[m].go);
                                PMDVfxPlayer.SpawnVfx("Explosion.None", mines[m].pos, 1.4f, 28f);
                                HitAndMark(mines[m].pos, 1.9f, skill.damage * 0.55f, Vector2.zero, skill, 0f);
                                mines[m] = (mines[m].pos, null, true);
                            }
                        }
                        yield return new WaitForSeconds(0.18f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.GravityBlackHole:
                {
                    PMDAudio.Play(PMDSfx.Ultimate);
                    Vector3 center = caster.position + (Vector3)(dir * 3.0f);
                    var hole = PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, center, 2.8f, 2.2f, 0f, -10, tint);
                    for (int p = 0; p < 6; p++)
                    {
                        if (hole != null) hole.transform.Rotate(0f, 0f, 60f);
                        var allWild = PMDWildPokemonAI.ActiveMons;
                        for (int i = allWild.Count - 1; i >= 0; i--)
                        {
                            var w = allWild[i];
                            if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(center, w.transform.position) <= 3.8f)
                            {
                                w.transform.position = Vector2.MoveTowards(w.transform.position, center, 0.6f);
                                w.TakeDamage(skill.damage * 0.24f, Vector2.zero, tint);
                            }
                        }
                        yield return new WaitForSeconds(0.28f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.WallSlamRepulse:
                {
                    PMDAudio.Play(PMDSfx.Melee);
                    PMDCameraFollow.Shake(0.30f, 0.24f);
                    PMDBespokeSkillEngine.SpawnExpandingRing(caster.position, tint, 0.5f, 3.6f, 0.35f);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(caster.position, w.transform.position) <= 3.5f)
                        {
                            Vector2 outDir = ((Vector2)(w.transform.position - caster.position)).normalized;
                            w.ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.4f);
                            w.TakeDamage(skill.damage, outDir * 3.2f, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.ShadowCloneExplode:
                {
                    PMDAudio.Play(PMDSfx.Dash);
                    Vector3 apex = caster.position + (Vector3)(dir * 3.6f);
                    Vector2 perp = new Vector2(-dir.y, dir.x);
                    PMDPlayerController.Instance.StartCoroutine(
                        PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, caster.position + (Vector3)(perp * 1.4f), apex, 0.25f, skill, dir, angle, 1.45f, 0.5f, tint));
                    PMDPlayerController.Instance.StartCoroutine(
                        PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, caster.position - (Vector3)(perp * 1.4f), apex, 0.25f, skill, dir, angle, 1.45f, 0.5f, tint));
                    yield return new WaitForSeconds(0.25f);
                    PMDVfxPlayer.SpawnVfx("Explosion.None", apex, 1.5f, 28f);
                    HitAndMark(apex, 2.2f, skill.damage * 0.55f, dir, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.LifeDrainTether:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    var target = PMDBespokeSkillEngine.FindNearestWildPublic(caster.position + (Vector3)(dir * 2.5f), 5.5f, null);
                    if (target == null)
                    {
                        HitAndMark(caster.position + (Vector3)(dir * 2.0f), 2.0f, skill.damage, dir, skill, 0f);
                        yield break;
                    }
                    PMDPixelVfxSystem.SpawnPixelTether(PMDPlayerController.Instance, caster, target.transform, tint, Color.Lerp(tint, Color.white, 0.45f), 1.35f);
                    for (int p = 0; p < 5; p++)
                    {
                        if (target == null || target.IsFainted || caster == null) yield break;
                        float drainDmg = skill.damage * 0.28f;
                        target.TakeDamage(drainDmg, Vector2.zero, tint);
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, Vector3.Lerp(caster.position, target.transform.position, 0.5f), 1.25f, 0.22f, 0f, 105, tint);
                        if (PMDPlayerController.Instance != null)
                            PMDPlayerController.Instance.Heal(Mathf.Max(3f, drainDmg * 0.55f));
                        yield return new WaitForSeconds(0.26f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.BloodSacrificeReset:
                {
                    PMDAudio.Play(PMDSfx.Ultimate);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.SpendHpForPower(0.12f);
                    PMDCameraFollow.Shake(0.35f, 0.28f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 2.8f, 0.55f, 0f, 110, tint);
                    HitAndMark(caster.position, 3.6f, skill.damage * 1.45f, dir * 1.8f, skill, 0f);
                    break;
                }

                case PMDArchetypeMechanic.TimeBombDelay:
                {
                    PMDAudio.Play(PMDSfx.Projectile);
                    Vector3 spot = caster.position + (Vector3)(dir * 3.0f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, spot, 1.9f, 1.5f, 0f, 110, tint);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(spot, w.transform.position) <= 2.8f)
                        {
                            w.ApplyTimeBomb(2.2f);
                            w.TakeDamage(skill.damage * 0.65f, dir, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.TimeLoopSnapback:
                {
                    PMDAudio.Play(PMDSfx.Gate);
                    var snapped = new List<(PMDWildPokemonAI mon, Vector3 anchor)>();
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = 0; i < allWild.Count; i++)
                    {
                        var w = allWild[i];
                        if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(caster.position, w.transform.position) <= 4.5f)
                        {
                            snapped.Add((w, w.transform.position));
                            PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, w.transform.position, 1.4f, 1.9f, 0f, -5, tint);
                        }
                    }
                    yield return new WaitForSeconds(1.8f);
                    PMDAudio.Play(PMDSfx.Ultimate, 0.8f);
                    for (int i = 0; i < snapped.Count; i++)
                    {
                        var item = snapped[i];
                        if (item.mon != null && !item.mon.IsFainted)
                        {
                            item.mon.transform.position = item.anchor;
                            PMDVfxPlayer.SpawnFloatingText(item.anchor, "⏳ TUA NGƯỢC!", tint, 0.9f);
                            item.mon.TakeDamage(skill.damage, Vector2.zero, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.ElementalTransform:
                {
                    PMDAudio.Play(PMDSfx.LevelUp, 0.85f);
                    if (PMDPlayerController.Instance != null)
                    {
                        PMDPlayerController.Instance.GrantSpeedBuff(1.45f, 4.5f);
                        PMDPlayerController.Instance.GrantShield(45f, 4.5f);
                    }
                    for (int pulse = 0; pulse < 6; pulse++)
                    {
                        if (caster == null) yield break;
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, caster.position, 2.2f, 0.3f, pulse * 45f, 105, tint);
                        HitAndMark(caster.position, 2.8f, skill.damage * 0.30f, Vector2.zero, skill, 0f);
                        yield return new WaitForSeconds(0.45f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.MarkDetonateChain:
                {
                    PMDAudio.Play(PMDSfx.SuperEffective);
                    Vector3 spot = caster.position + (Vector3)(dir * 2.4f);
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w == null || w.IsFainted || w.IsRecruited) continue;
                        if (Vector2.Distance(spot, w.transform.position) <= 3.0f)
                        {
                            float bonus = w.HasVulnerabilityMark ? w.ConsumeVulnerabilityMark() * 1.45f : 1.0f;
                            if (!w.HasVulnerabilityMark) w.ApplyVulnerabilityMark(1.35f, 4.5f);
                            PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, w.transform.position, 1.75f, 0.35f, 0f, 110, tint);
                            w.TakeDamage(skill.damage * bonus, dir, tint);
                        }
                    }
                    break;
                }

                case PMDArchetypeMechanic.ExecuteLowHp:
                {
                    PMDAudio.Play(PMDSfx.SuperEffective);
                    Vector3 spot = caster.position + (Vector3)(dir * 1.9f);
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, spot, 2.1f, 0.35f, angle, 110, tint);
                    bool killed = false;
                    var allWild = PMDWildPokemonAI.ActiveMons;
                    for (int i = allWild.Count - 1; i >= 0; i--)
                    {
                        var w = allWild[i];
                        if (w == null || w.IsFainted || w.IsRecruited) continue;
                        if (Vector2.Distance(spot, w.transform.position) <= 2.4f)
                        {
                            bool low = (w.CurrentHp / Mathf.Max(1f, w.MaxHp)) <= 0.26f;
                            float dmg = low ? w.CurrentHp + 99f : skill.damage;
                            if (low) PMDVfxPlayer.SpawnFloatingText(w.transform.position, "💀 KẾT LIỄU!", new Color(1f, 0.22f, 0.25f), 1.2f);
                            w.TakeDamage(dmg, dir * 1.6f, tint);
                            if (w.IsFainted) killed = true;
                        }
                    }
                    if (killed && PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.ResetAllCooldowns();
                    break;
                }

                case PMDArchetypeMechanic.DomainExpansion:
                {
                    PMDAudio.Play(PMDSfx.Ultimate);
                    if (PMDPlayerController.Instance != null)
                        PMDPlayerController.Instance.ActivateDomain(6.0f);
                    Vector3 domainPos = caster.position;
                    PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, domainPos, 4.2f, 5.5f, 0f, -18, tint);
                    for (int tick = 0; tick < 10; tick++)
                    {
                        if (caster == null) yield break;
                        Vector3 strike = domainPos + (Vector3)(Random.insideUnitCircle * 3.6f);
                        PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, strike, 1.45f, 0.32f, tick * 35f, 105, tint);
                        HitAndMark(domainPos, 4.2f, skill.damage * 0.22f, Vector2.zero, skill, 0f);
                        yield return new WaitForSeconds(0.50f);
                    }
                    break;
                }

                case PMDArchetypeMechanic.ScreenCataclysm:
                {
                    PMDAudio.Play(PMDSfx.Ultimate);
                    PMDBespokeSkillEngine.SpawnExpandingRing(caster.position, tint, 0.4f, 4.5f, 0.45f);
                    yield return new WaitForSeconds(0.35f);
                    PMDCameraFollow.Shake(0.5f, 0.38f);
                    Vector3 center = caster != null ? caster.position : Vector3.zero;
                    for (int m = 0; m < 14; m++)
                    {
                        Vector3 drop = center + (Vector3)(Random.insideUnitCircle * 4.5f);
                        if (PMDPlayerController.Instance != null)
                            PMDPlayerController.Instance.StartCoroutine(
                                PMDBespokeSkillEngine.FlyAndHitSparkPublic(sp, drop + new Vector3(0f, 3.0f, 0f), drop, 0.22f, skill, Vector2.zero, 0f, 1.55f, 0.30f, tint));
                        yield return new WaitForSeconds(0.055f);
                    }
                    break;
                }
            }
        }

        private static IEnumerator RunBoomerang(Transform caster, Sprite sp, Vector2 dir, PMDSkillData skill, Color tint)
        {
            Vector3 start = caster != null ? caster.position : Vector3.zero;
            Vector3 apex = start + (Vector3)(dir * 5.2f);
            var go = PMDBespokeSkillEngine.SpawnPixelVisualPublic(sp, start, 1.4f, 1.25f, 0f, 105, tint);
            float t = 0f;
            while (t < 1f && go != null)
            {
                t += Time.deltaTime * 1.45f;
                Vector3 ret = caster != null ? caster.position : start;
                Vector3 pos = t < 0.5f ? Vector3.Lerp(start, apex, t * 2f) : Vector3.Lerp(apex, ret, (t - 0.5f) * 2f);
                go.transform.position = pos;
                go.transform.Rotate(0f, 0f, 920f * Time.deltaTime);
                HitAndMark(pos, 0.9f, skill.damage * 0.20f, dir, skill, 0f);
                yield return null;
            }
        }

        private static void HitAndMark(Vector3 center, float radius, float dmg, Vector2 knock, PMDSkillData skill, float markMult)
        {
            bool anyHit = false;
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = allWild.Count - 1; i >= 0; i--)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited) continue;
                if (Vector2.Distance(center, w.transform.position) <= radius)
                {
                    anyHit = true;
                    if (markMult > 1f) w.ApplyVulnerabilityMark(markMult, 4.5f);
                    PMDPixelVfxSystem.TriggerWhiteFlash(w.gameObject, 2);
                    PMDPixelVfxSystem.SpawnElementalBurst(w.transform.position, skill.elementColor, skill.moveType, 7, 3.0f);
                    w.TakeDamage(Mathf.Max(1f, dmg), knock, skill.elementColor, skill.moveType);
                }
            }
            if (anyHit)
            {
                if (dmg >= 45f)
                {
                    PMDPixelVfxSystem.SpawnAftermathDecal(center, skill.elementColor, skill.moveType, 1.4f);
                    PMDPixelVfxSystem.TriggerHitStop(PMDPlayerController.Instance, dmg >= 85f ? 3 : 2);
                }
            }
        }

        private static Sprite ResolveSprite(PMDSkillData skill)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.skillName))
            {
                var chars = new System.Text.StringBuilder(skill.skillName.Length);
                foreach (char c in skill.skillName)
                {
                    if (char.IsLetterOrDigit(c)) chars.Append(c);
                    else if (c == ' ' || c == '-' || c == '_') chars.Append('_');
                }
                string safeName = chars.ToString().Trim('_');
                if (!string.IsNullOrEmpty(safeName))
                {
                    var diskFrames = PMDRuntimeCharacterLoader.LoadVfxFrames("VFX_Move_" + safeName, 24f);
                    if (diskFrames != null && diskFrames.Count > 0 && diskFrames[0] != null)
                    {
                        PMDBespokeSkillEngine.RegisterAnimStripPublic(diskFrames[0].name, diskFrames);
                        return diskFrames[0];
                    }
                }
            }

            if (skill != null && skill.visualKind != PMDVisualAssetKind.Auto)
                return PMDBespokeSkillEngine.GetSpriteByVisualKey(skill.visualKind.ToString());

            return PMDBespokeSkillEngine.GetSpriteByVisualKey("DoomStar");
        }
    }
}
