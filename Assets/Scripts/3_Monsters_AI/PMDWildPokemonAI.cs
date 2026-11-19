using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDWildState
    {
        Idle,
        Wander,
        Sleep,
        Chase,
        FollowLeader,
        Dazed,
        Fainted
    }

    public enum PMDStatusAilment
    {
        None = 0,
        Paralyzed = 1,
        Burned = 2,
        Frozen = 3,
        Poisoned = 4,
        SoakedSlow = 5,
        PsychicStun = 6
    }

    /// <summary>
    /// AI for Wild Pokémon, Region Bosses and Recruited Followers.
    /// - Có Cấp độ (Level) theo vùng, HP/Sát thương tăng theo cấp.
    /// - Nhận sát thương theo bảng hệ khắc chế (Siêu hiệu quả / Không hiệu quả lắm).
    /// - Hạ gục → thưởng EXP, Poké, có thể rơi vật phẩm, cập nhật nhiệm vụ.
    /// - Đồng đội có thể bị thương / gục ngã và tự hồi sinh sau vài giây.
    /// </summary>
    [RequireComponent(typeof(PMDSpriteAnimator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PMDWildPokemonAI : MonoBehaviour
    {
        public static readonly List<PMDWildPokemonAI> ActiveMons = new List<PMDWildPokemonAI>();

        [SerializeField] private float moveSpeed = 2.3f;
        [SerializeField] private float maxHp = 80f;
        [SerializeField] private float currentHp = 80f;
        [SerializeField] private bool isAggressive;
        [SerializeField] private string homeBiomeName = "Treasure Town";
        [SerializeField] private int level = 1;
        [SerializeField] private bool isBoss;

        private PMDSpriteAnimator _animator;
        private Rigidbody2D _rb;
        private PMDWildState _state = PMDWildState.Idle;
        private Vector2 _homePosition;
        private Vector2 _wanderTarget;
        private float _stateTimer;
        private float _skillCooldown;
        private float _faintRespawnTimer;
        private float _dazeTimer;
        private float _dazeMaxDuration = 14f;
        private int _captureAttemptsRemaining;
        private bool _isBeingCaptured;
        private bool _wasRecruited;
        private bool _bossRoared;
        private PMDStatusAilment _activeAilment = PMDStatusAilment.None;
        private float _ailmentTimer;
        private float _ailmentTickTimer;
        private float _markedMultiplier = 1f;
        private float _markedTimer;
        private float _timeBombStoredDamage;
        private float _timeBombTimer;

        private PMDPlayerController _leader;
        private int _partySlotIndex;
        private PMDSkillData[] _skills;
        private readonly float[] _botSlotCooldowns = new float[4];
        private float _dodgeTimer;
        private float _dodgeCooldown;
        private Vector2 _dodgeDir;
        private int _strafeSign = 1;
        private float _strafeFlipTimer;
        private float _botShieldAmount;

        public string PokemonName => Animator != null && Animator.CharacterData != null ? Animator.CharacterData.pokemonName : name;
        public PMDStatusAilment ActiveAilment => _ailmentTimer > 0f ? _activeAilment : PMDStatusAilment.None;
        public string ActiveAilmentTag => ActiveAilment switch
        {
            PMDStatusAilment.Paralyzed => "⚡TÊ LIỆT",
            PMDStatusAilment.Burned => "🔥BỎNG",
            PMDStatusAilment.Frozen => "❄️ĐÓNG BĂNG",
            PMDStatusAilment.Poisoned => "☠️TRÚNG ĐỘC",
            PMDStatusAilment.SoakedSlow => "💧CHẬM",
            PMDStatusAilment.PsychicStun => "🔮CHOÁNG",
            _ => string.Empty
        };
        public string HomeBiomeName
        {
            get => homeBiomeName;
            set => homeBiomeName = value;
        }
        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public int Level => level;
        public bool IsBoss => isBoss;
        public bool IsDazed => _state == PMDWildState.Dazed;
        public bool IsFainted => _state == PMDWildState.Fainted || _state == PMDWildState.Dazed;
        public bool IsBeingCaptured => _isBeingCaptured;
        public float DazeTimeRatio => _dazeMaxDuration > 0f ? Mathf.Clamp01(_dazeTimer / _dazeMaxDuration) : 0f;
        public float DazeTimeRemaining => Mathf.Max(0f, _dazeTimer);
        public int CaptureAttemptsRemaining => _captureAttemptsRemaining;
        public bool CanBeCaptured => _state == PMDWildState.Dazed && !_isBeingCaptured && !isBoss && !_wasRecruited && _captureAttemptsRemaining > 0;
        public bool IsRecruited => _state == PMDWildState.FollowLeader || (_wasRecruited && _state == PMDWildState.Fainted);
        public bool IsAggressive
        {
            get => isAggressive;
            set => isAggressive = value;
        }
        public PMDWildState CurrentState => _state;
        public PMDSpriteAnimator Animator => _animator != null ? _animator : (_animator = GetComponent<PMDSpriteAnimator>());

        public static System.Action<PMDWildPokemonAI> OnMonsterDefeated;

        private void Awake()
        {
            _animator = GetComponent<PMDSpriteAnimator>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _homePosition = transform.position;
            ChooseNextAmbientState();
        }

        private void OnEnable()
        {
            if (!ActiveMons.Contains(this))
                ActiveMons.Add(this);
        }

        private void OnDisable()
        {
            ActiveMons.Remove(this);
        }

        public void InitializeWild(PMDCharacterData data, Vector2 spawnPos, string biomeName, bool aggressive)
        {
            InitializeWild(data, spawnPos, biomeName, aggressive, 1, false);
        }

        public void InitializeWild(PMDCharacterData data, Vector2 spawnPos, string biomeName, bool aggressive, int monLevel, bool boss)
        {
            if (Animator != null)
            {
                Animator.CharacterData = data;
            }
            transform.position = spawnPos;
            _homePosition = spawnPos;
            homeBiomeName = biomeName;
            isAggressive = aggressive || boss;
            isBoss = boss;
            level = Mathf.Max(1, monLevel);
            float baseHp = isAggressive ? 105f : 75f;
            maxHp = Mathf.Round(baseHp * (1f + (level - 1) * 0.12f) * (boss ? 5f : 1f));
            currentHp = maxHp;
            moveSpeed = boss ? 2.6f : 2.3f;
            _skills = PMDSkillDatabase.GetSkillsForPokemon(data != null ? data.pokemonName : "Pikachu");
            ChooseNextAmbientState();
        }

        private void EnsureSkillsLoaded()
        {
            if (_skills == null || _skills.Length == 0)
            {
                _skills = PMDSkillDatabase.GetSkillsForPokemon(PokemonName);
            }
        }

        private void Update()
        {
            EnsureSkillsLoaded();

            if (_skillCooldown > 0f)
                _skillCooldown -= Time.deltaTime;

            if (_state == PMDWildState.Dazed)
            {
                _rb.linearVelocity = Vector2.zero;
                if (!_isBeingCaptured)
                {
                    _dazeTimer -= Time.deltaTime;
                    if (_dazeTimer <= 0f)
                    {
                        DismissToRespawn("Hết ngất ngư - bỏ chạy!");
                    }
                }
                return;
            }

            if (_state == PMDWildState.Fainted)
            {
                _rb.linearVelocity = Vector2.zero;
                _faintRespawnTimer -= Time.deltaTime;
                if (_faintRespawnTimer <= 0f)
                {
                    if (_wasRecruited)
                    {
                        currentHp = maxHp * 0.5f;
                        _state = PMDWildState.FollowLeader;
                        SetMonsterVisible(true, true);
                        if (_leader != null)
                            transform.position = (Vector2)_leader.transform.position + Random.insideUnitCircle * 1.2f;
                        PMDVfxPlayer.SpawnFloatingText(transform.position, "Đã hồi phục!", new Color(0.5f, 1f, 0.6f));
                    }
                    else
                    {
                        currentHp = maxHp;
                        transform.position = _homePosition;
                        isAggressive = isAggressive || Random.value < 0.5f;
                        SetMonsterVisible(true, true);
                        ChooseNextAmbientState();
                    }
                }
                return;
            }

            if (Animator != null && Animator.IsPlayingOneShot)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (_state == PMDWildState.FollowLeader)
            {
                UpdateFollowLeader();
                return;
            }

            // Process active elemental status effects (Burn DoT, Poison DoT, Freeze lock, Psychic Stun, Paralysis)
            if (_markedTimer > 0f)
                _markedTimer -= Time.deltaTime;

            if (_timeBombTimer > 0f)
            {
                _timeBombTimer -= Time.deltaTime;
                if (_timeBombTimer <= 0f)
                {
                    DetonateTimeBomb();
                }
            }

            if (_ailmentTimer > 0f)
            {
                _ailmentTimer -= Time.deltaTime;
                _ailmentTickTimer -= Time.deltaTime;
                if (_ailmentTickTimer <= 0f)
                {
                    _ailmentTickTimer = 0.55f;
                    TickStatusAilmentEffect();
                }

                if (_activeAilment == PMDStatusAilment.Frozen || _activeAilment == PMDStatusAilment.PsychicStun)
                {
                    _rb.linearVelocity = Vector2.zero;
                    return;
                }
            }

            if (_dodgeCooldown > 0f)
                _dodgeCooldown -= Time.deltaTime;
            for (int i = 0; i < _botSlotCooldowns.Length; i++)
                if (_botSlotCooldowns[i] > 0f)
                    _botSlotCooldowns[i] -= Time.deltaTime;

            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= Time.deltaTime;
                _rb.linearVelocity = _dodgeDir * (moveSpeed * 3.1f);
                Animator.Play("Walk", true);
                return;
            }

            // Aggressive wild Pokémon scan for Player
            if (isAggressive && PMDPlayerController.Instance != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, PMDPlayerController.Instance.transform.position);
                float aggroRange = isBoss ? 9.5f : 6.8f;
                if (distToPlayer < aggroRange && Vector2.Distance(transform.position, _homePosition) < (isBoss ? 12f : 15f))
                {
                    if (isBoss && !_bossRoared)
                    {
                        _bossRoared = true;
                        PMDAudio.Play(PMDSfx.BossRoar);
                        PMDGameState.Toast($"⚠ TRÙM {PokemonName.ToUpper()} (Cấp {level}) xuất hiện!", new Color(1f, 0.4f, 0.6f));
                    }
                    _state = PMDWildState.Chase;
                }
                else if (_state == PMDWildState.Chase)
                {
                    ChooseNextAmbientState();
                }
            }

            switch (_state)
            {
                case PMDWildState.Idle:
                    _rb.linearVelocity = Vector2.zero;
                    Animator.Play("Idle", true);
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f) ChooseNextAmbientState();
                    break;

                case PMDWildState.Sleep:
                    _rb.linearVelocity = Vector2.zero;
                    Animator.Play("Sleep", true);
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f) ChooseNextAmbientState();
                    break;

                case PMDWildState.Wander:
                    Vector2 toTarget = _wanderTarget - (Vector2)transform.position;
                    if (toTarget.magnitude < 0.25f || _stateTimer <= 0f)
                    {
                        ChooseNextAmbientState();
                    }
                    else
                    {
                        _stateTimer -= Time.deltaTime;
                        Vector2 dir = toTarget.normalized;
                        _rb.linearVelocity = dir * moveSpeed;
                        Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dir, Animator.CurrentDirection);
                        Animator.Play("Walk", true);
                    }
                    break;

                case PMDWildState.Chase:
                    UpdateHostileChaseAndCastSkills();
                    break;
            }
        }

        private void UpdateHostileChaseAndCastSkills()
        {
            if (PMDPlayerController.Instance == null)
            {
                ChooseNextAmbientState();
                return;
            }

            Vector2 toPlayer = (Vector2)PMDPlayerController.Instance.transform.position - (Vector2)transform.position;
            float dist = toPlayer.magnitude;
            Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.down;
            Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dir, Animator.CurrentDirection);

            // 1. Smart Evasion: sidestep-dash when the player winds up an attack nearby
            if (_dodgeCooldown <= 0f && dist <= 4.6f && PMDPlayerController.Instance.Animator != null && PMDPlayerController.Instance.Animator.IsPlayingOneShot)
            {
                if (Random.value < (isBoss ? 0.55f : 0.35f))
                {
                    TryBotDodge(dir, false);
                    return;
                }
            }

            // 2. Rotate & cast ALL 4 skills of this Pokémon using per-slot cooldowns!
            if (_skillCooldown <= 0f && _skills != null && _skills.Length > 0 && !Animator.IsPlayingOneShot)
            {
                int chosenSlot = PickBestBotSkillSlot(dist);
                if (chosenSlot >= 0)
                {
                    ExecuteBotSkill(_skills[chosenSlot], chosenSlot, dir, dist);
                    return;
                }
            }

            // 3. Tactical Movement: approach, strafe sideways around the player, or kite back if too close
            float speedMult = (ActiveAilment == PMDStatusAilment.Paralyzed || ActiveAilment == PMDStatusAilment.SoakedSlow) ? 0.38f : 1.0f;
            _strafeFlipTimer -= Time.deltaTime;
            if (_strafeFlipTimer <= 0f)
            {
                _strafeFlipTimer = Random.Range(1.0f, 1.8f);
                _strafeSign = Random.value < 0.5f ? 1 : -1;
            }

            Vector2 perp = new Vector2(-dir.y, dir.x) * _strafeSign;
            Vector2 moveVec;
            if (dist > 3.6f)
            {
                // Zigzag approach so bots aren't walking in a predictable straight line
                moveVec = (dir * 0.85f + perp * 0.45f).normalized;
            }
            else if (dist < 1.25f)
            {
                // Backstep kite & strafe
                moveVec = (-dir * 0.65f + perp * 0.75f).normalized;
            }
            else
            {
                // Orbit/strafe around player while waiting for next skill cooldown
                moveVec = (perp * 0.85f + dir * 0.2f).normalized;
            }

            _rb.linearVelocity = moveVec * (moveSpeed * 1.35f * speedMult);
            Animator.Play("Walk", true);
        }

        private int PickBestBotSkillSlot(float dist)
        {
            // Slot 3: Ultimate / Signature (Bosses or <65% HP or periodic big attack)
            if (_skills.Length > 3 && _botSlotCooldowns[3] <= 0f && dist <= 5.2f && (isBoss || currentHp < maxHp * 0.72f || Random.value < 0.35f))
                return 3;

            // Slot 2: Tactical Dash / AoE / Shield / Buff
            if (_skills.Length > 2 && _botSlotCooldowns[2] <= 0f && dist <= 5.4f)
                return 2;

            // Slot 1: Ranged Projectile / Beam (mid-long range)
            if (_skills.Length > 1 && _botSlotCooldowns[1] <= 0f && dist >= 1.6f && dist <= 6.8f)
                return 1;

            // Slot 0: Melee / Close Burst
            if (_skills.Length > 0 && _botSlotCooldowns[0] <= 0f && dist <= (isBoss ? 2.5f : 2.1f))
                return 0;

            return -1;
        }

        private void ExecuteBotSkill(PMDSkillData skill, int slotIndex, Vector2 dir, float dist)
        {
            if (skill == null) return;

            float cdScale = isBoss ? 1.15f : 1.45f;
            _botSlotCooldowns[slotIndex] = Mathf.Max(1.1f, skill.cooldown * cdScale);
            _skillCooldown = isBoss ? 0.65f : 0.90f; // Brief global pacing between consecutive moves
            _rb.linearVelocity = Vector2.zero;

            string vfx = ResolveWildVfxSheet(skill);
            string tag = slotIndex == 3 ? $"!! {skill.skillName}" : $"! {skill.skillName}";
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, slotIndex == 3 ? 0.75f : 0.48f, 0f), tag, skill.elementColor, slotIndex == 3 ? 1.05f : 0.8f);

            // Slot 1: Ranged Projectile with slight predictive lead
            if (slotIndex == 1 || skill.kind == PMDSkillKind.Projectile)
            {
                Animator.PlayOneShot(!string.IsNullOrEmpty(skill.animName) ? skill.animName : "Shoot", "Idle", () =>
                {
                    if (PMDPlayerController.Instance != null && !IsFainted)
                    {
                        var pRb = PMDPlayerController.Instance.GetComponent<Rigidbody2D>();
                        Vector2 targetPos = (Vector2)PMDPlayerController.Instance.transform.position + (pRb != null ? pRb.linearVelocity * 0.16f : Vector2.zero);
                        Vector2 aim = (targetPos - (Vector2)transform.position).normalized;
                        PMDProjectile.SpawnProjectile(skill, transform.position, aim, false, transform);
                        PMDPixelVfxSystem.SpawnElementalBurst(transform.position, skill.elementColor, skill.moveType, 5, 2.2f);
                    }
                });
                return;
            }

            // Slot 2: Support/Shield/Dash/AoE depending on Archetype
            if (slotIndex == 2)
            {
                if (skill.archetype == PMDArchetypeMechanic.RegenCleanseBurst ||
                    skill.archetype == PMDArchetypeMechanic.ShieldBarrierPush ||
                    skill.archetype == PMDArchetypeMechanic.ParryReflectStance ||
                    skill.archetype == PMDArchetypeMechanic.ElementalTransform)
                {
                    Animator.PlayOneShot("Strike", "Idle", () =>
                    {
                        if (IsFainted) return;
                        _botShieldAmount = Mathf.Max(_botShieldAmount, maxHp * 0.18f);
                        currentHp = Mathf.Min(maxHp, currentHp + maxHp * 0.10f);
                        PMDVfxPlayer.SpawnVfx(vfx, transform.position, 1.45f, 26f, skill.elementColor, transform);
                        PMDPixelVfxSystem.SpawnElementalBurst(transform.position, skill.elementColor, skill.moveType, 8, 2.6f);
                        DamagePlayerTeamInRadius(transform.position, 2.4f, skill.damage * 0.4f, skill.elementColor, skill.moveType);
                    });
                    return;
                }

                if (skill.archetype == PMDArchetypeMechanic.InvulnDashSlash ||
                    skill.archetype == PMDArchetypeMechanic.BlinkBackstab ||
                    skill.archetype == PMDArchetypeMechanic.AerialLeapSlam ||
                    skill.kind == PMDSkillKind.LinearShockwaveDash)
                {
                    // Rapid dash strike toward the player
                    _dodgeDir = dir;
                    _dodgeTimer = 0.22f;
                    _dodgeCooldown = 1.6f;
                    Animator.PlayOneShot("Attack", "Idle", () =>
                    {
                        if (IsFainted) return;
                        PMDVfxPlayer.SpawnVfx(vfx, transform.position + (Vector3)(dir * 0.7f), 1.4f, 26f, skill.elementColor);
                        PMDPixelVfxSystem.SpawnElementalBurst(transform.position, skill.elementColor, skill.moveType, 8, 3.0f);
                        PMDAudio.Play(PMDSfx.Dash, 0.75f);
                        DamagePlayerTeamInRadius(transform.position, 2.2f, skill.damage * 0.52f, skill.elementColor, skill.moveType);
                    });
                    return;
                }
            }

            // Slot 3: Ultimate with telegraph ring -> AoE burst
            if (slotIndex == 3)
            {
                PMDVfxPlayer.SpawnVfx("Circle_Red_Out.None", transform.position, 1.9f, 24f, skill.elementColor);
                Animator.PlayOneShot("Hop", "Idle", () =>
                {
                    if (IsFainted) return;
                    PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
                    if (!string.IsNullOrEmpty(skill.ringVfxSheet))
                        PMDVfxPlayer.SpawnVfx(skill.ringVfxSheet, transform.position, 2.2f, 24f);
                    PMDVfxPlayer.SpawnVfx(vfx, transform.position, 1.85f, 24f, skill.elementColor);
                    PMDPixelVfxSystem.SpawnElementalBurst(transform.position, skill.elementColor, skill.moveType, 12, 3.6f);
                    DamagePlayerTeamInRadius(transform.position, 3.4f, skill.damage * 0.45f, skill.elementColor, skill.moveType);
                });
                return;
            }

            // Slot 0 (or default AoE): Lunge + strike
            _rb.linearVelocity = dir * (moveSpeed * 1.8f);
            Animator.PlayOneShot(!string.IsNullOrEmpty(skill.animName) ? skill.animName : "Attack", "Idle", () =>
            {
                if (IsFainted) return;
                PMDVfxPlayer.SpawnVfx(vfx, transform.position + (Vector3)(dir * 0.8f), 1.3f, 26f, skill.elementColor);
                PMDPixelVfxSystem.SpawnElementalBurst(transform.position + (Vector3)(dir * 0.6f), skill.elementColor, skill.moveType, 6, 2.5f);
                PMDAudio.Play(PMDSfx.Melee, 0.7f);
                DamagePlayerTeamInRadius(transform.position + (Vector3)(dir * 0.5f), isBoss ? 2.6f : 2.15f, skill.damage * 0.45f, skill.elementColor, skill.moveType);
            });
        }

        private void TryBotDodge(Vector2 threatDir, bool reactiveOnHit)
        {
            if (_dodgeCooldown > 0f || IsFainted)
                return;

            _dodgeCooldown = isBoss ? 2.1f : 3.0f;
            _dodgeTimer = 0.22f;
            Vector2 perp = new Vector2(-threatDir.y, threatDir.x) * (Random.value < 0.5f ? 1f : -1f);
            _dodgeDir = reactiveOnHit
                ? (perp * 0.75f - threatDir * 0.65f).normalized
                : perp.normalized;

            PMDVfxPlayer.SpawnVfx("Circle_Blue_Out.None", transform.position, 0.9f, 28f);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.45f, 0f), "⚡ NÉ!", new Color(0.55f, 0.95f, 1f), 0.7f);
        }

        private static string ResolveWildVfxSheet(PMDSkillData skill)
        {
            if (skill == null) return "Explosion.None";
            if (!string.IsNullOrEmpty(skill.vfxSheet)) return skill.vfxSheet;
            if (!string.IsNullOrEmpty(skill.skillName))
            {
                var sb = new System.Text.StringBuilder(skill.skillName.Length);
                foreach (char c in skill.skillName)
                {
                    if (char.IsLetterOrDigit(c)) sb.Append(c);
                    else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
                }
                string safe = sb.ToString().Trim('_');
                if (!string.IsNullOrEmpty(safe))
                    return "VFX_Move_" + safe;
            }
            return "Explosion.None";
        }

        private void DamagePlayerTeamInRadius(Vector2 center, float radius, float baseDamage, Color elementColor, string moveType = null)
        {
            var player = PMDPlayerController.Instance;
            if (player != null && Vector2.Distance(center, player.transform.position) <= radius)
            {
                player.TakeDamageFrom(baseDamage, elementColor, level, isBoss, moveType);
            }

            if (player != null)
            {
                var party = player.RecruitedParty;
                for (int i = 0; i < party.Count; i++)
                {
                    var mon = party[i];
                    if (mon != null && !mon.IsFainted && Vector2.Distance(center, mon.transform.position) <= radius)
                        mon.TakeDamageAsFollower(baseDamage * (0.8f + level * 0.07f) * (isBoss ? 1.5f : 1f));
                }
            }
        }

        private void UpdateFollowLeader()
        {
            if (_leader == null)
                return;

            // Followers automatically assist the player against nearby hostile monsters!
            if (_skillCooldown <= 0f)
            {
                PMDWildPokemonAI hostileTarget = FindNearestHostileMonster(4.8f);
                if (hostileTarget != null && _skills != null && _skills.Length > 1)
                {
                    float distToHostile = Vector2.Distance(transform.position, hostileTarget.transform.position);
                    Vector2 dirToHostile = ((Vector2)(hostileTarget.transform.position - transform.position)).normalized;
                    Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dirToHostile, Animator.CurrentDirection);

                    if (distToHostile <= 1.8f)
                    {
                        PMDSkillData melee = _skills[0];
                        _skillCooldown = 1.8f;
                        PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.4f, 0f), melee.skillName, melee.elementColor, 0.75f);
                        Animator.PlayOneShot(melee.animName, "Idle", () =>
                        {
                            if (hostileTarget != null && !hostileTarget.IsFainted)
                            {
                                PMDVfxPlayer.SpawnVfx(melee.vfxSheet, hostileTarget.transform.position, 1.2f, 28f);
                                hostileTarget.TakeDamage(melee.damage * 0.65f, dirToHostile, melee.elementColor);
                            }
                        });
                        return;
                    }
                    else
                    {
                        PMDSkillData projSkill = _skills[1];
                        _skillCooldown = 2.4f;
                        PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.4f, 0f), projSkill.skillName, projSkill.elementColor, 0.75f);
                        Animator.PlayOneShot(projSkill.animName, "Idle", () =>
                        {
                            if (hostileTarget != null && !hostileTarget.IsFainted)
                            {
                                PMDProjectile.SpawnProjectile(projSkill, transform.position, dirToHostile, true, transform);
                            }
                        });
                        return;
                    }
                }
            }

            float followDist = 1.35f + (_partySlotIndex * 1.0f);
            Vector2 toLeader = (Vector2)_leader.transform.position - (Vector2)transform.position;
            float dist = toLeader.magnitude;

            if (dist > 14f)
            {
                transform.position = (Vector2)_leader.transform.position - _leader.Animator.CurrentDirection.ToVector2() * followDist;
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (dist > followDist)
            {
                Vector2 dir = toLeader.normalized;
                float speed = dist > followDist + 2f ? 6.2f : 4.2f;
                _rb.linearVelocity = dir * speed;
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dir, Animator.CurrentDirection);
                Animator.Play("Walk", true);
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                Animator.Play("Idle", true);
            }
        }

        private PMDWildPokemonAI FindNearestHostileMonster(float maxRange)
        {
            PMDWildPokemonAI best = null;
            float bestDist = maxRange;

            for (int i = 0; i < ActiveMons.Count; i++)
            {
                var other = ActiveMons[i];
                if (other == null || other == this || other.IsFainted || other.IsRecruited || !other.IsAggressive)
                    continue;

                float d = Vector2.Distance(transform.position, other.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }
            return best;
        }

        /// <summary>
        /// Neutral-element damage from the player team.
        /// </summary>
        public void TakeDamage(float dmg, Vector2 knockbackDir)
        {
            TakeDamage(dmg, knockbackDir, new Color(0.92f, 0.88f, 0.78f));
        }

        /// <summary>
        /// Damage from the player team, applying level scaling and the type chart.
        /// </summary>
        public void TakeDamage(float dmg, Vector2 knockbackDir, Color elementColor, string moveType = null)
        {
            if (_state == PMDWildState.Fainted || _state == PMDWildState.Dazed || IsRecruited)
                return;

            PMDElement atk = !string.IsNullOrEmpty(moveType)
                ? PMDTypeChart.FromTypeName(moveType)
                : PMDTypeChart.FromColor(elementColor);
            float typeMult = PMDTypeChart.GetMultiplier(atk, PMDTypeChart.GetSpeciesTypes(PokemonName));
            float markMult = _markedTimer > 0f ? _markedMultiplier : 1f;
            float finalDmg = dmg * PMDGameState.PlayerDamageMultiplier * typeMult * markMult / (1f + (level - 1) * 0.05f);
            finalDmg = Mathf.Max(1f, Mathf.Round(finalDmg));

            if (_botShieldAmount > 0f)
            {
                float absorbed = Mathf.Min(_botShieldAmount, finalDmg);
                _botShieldAmount -= absorbed;
                finalDmg -= absorbed;
                if (absorbed > 0f)
                    PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.4f, 0f), $"🛡️ -{absorbed:0}", new Color(0.55f, 0.92f, 1f), 0.7f);
                if (finalDmg <= 0.1f)
                    return;
            }

            if (_timeBombTimer > 0f)
            {
                _timeBombStoredDamage += finalDmg * 0.85f;
            }

            currentHp = Mathf.Max(0f, currentHp - finalDmg);
            transform.position += (Vector3)(knockbackDir * (isBoss ? 0.08f : 0.28f));
            if (currentHp > 0f && _dodgeCooldown <= 0f && Random.value < (isBoss ? 0.45f : 0.30f))
            {
                TryBotDodge(knockbackDir.sqrMagnitude > 0.01f ? -knockbackDir.normalized : Vector2.right, true);
            }
            PMDHitFlash.Flash(gameObject, elementColor, 0.14f);
            PMDBespokeSkillEngine.SpawnHitImpactJuice(transform.position, elementColor, atk);
            TryInflictElementalAilment(atk);

            if (typeMult > 1.01f || isBoss)
            {
                PMDCameraFollow.Shake(0.18f, isBoss ? 0.22f : 0.14f);
            }

            Color numCol = typeMult > 1.01f ? new Color(1f, 0.35f, 0.25f) : (typeMult < 0.99f ? new Color(0.7f, 0.7f, 0.75f) : new Color(1f, 0.85f, 0.25f));
            PMDVfxPlayer.SpawnFloatingText(transform.position, $"-{finalDmg:0}", numCol);
            if (typeMult > 1.01f)
            {
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.35f, 0f), "Siêu hiệu quả!", new Color(1f, 0.45f, 0.25f), 0.85f);
                PMDAudio.Play(PMDSfx.SuperEffective, 0.8f);
            }
            else if (typeMult < 0.99f)
            {
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.35f, 0f), "Không hiệu quả lắm...", new Color(0.7f, 0.75f, 0.85f), 0.85f);
                PMDAudio.Play(PMDSfx.NotVeryEffective, 0.8f);
            }
            else
            {
                PMDAudio.Play(PMDSfx.Hit, 0.7f);
            }

            if (currentHp <= 0f)
            {
                Faint();
            }
            else
            {
                isAggressive = true;
                _state = PMDWildState.Chase;
                if (!isBoss || Random.value < 0.3f)
                    Animator.PlayOneShot("Hurt", "Idle");
            }
        }

        private void Faint()
        {
            _rb.linearVelocity = Vector2.zero;
            PMDAudio.Play(PMDSfx.Faint);

            int mapZone = 0;
            var mapGen = PMDOpenWorldMapGenerator.Instance;
            if (mapGen != null)
                mapZone = mapGen.ActiveZoneIndex;

            if (PMDGameState.Instance != null)
                PMDGameState.Instance.RegisterKill(PokemonName, isBoss, mapZone, level);

            // Item drops
            if (mapGen != null)
            {
                if (isBoss)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        string rare = PMDGameState.RareLootTable[Random.Range(0, PMDGameState.RareLootTable.Length)];
                        mapGen.SpawnDroppedItem(rare, (Vector2)transform.position + Random.insideUnitCircle * 1.4f);
                    }
                }
                else if (Random.value < 0.32f)
                {
                    string loot = PMDGameState.FieldLootTable[Random.Range(0, PMDGameState.FieldLootTable.Length)];
                    mapGen.SpawnDroppedItem(loot, (Vector2)transform.position + Random.insideUnitCircle * 0.6f);
                }
            }

            OnMonsterDefeated?.Invoke(this);

            if (isBoss)
            {
                _state = PMDWildState.Fainted;
                _faintRespawnTimer = 99999f;
                Animator.Play("Sleep", true);
                PMDVfxPlayer.SpawnFloatingText(transform.position, "TRÙM ĐÃ BỊ HẠ!", new Color(1f, 0.5f, 0.2f), 2f);
                Destroy(gameObject, 2.5f);
                return;
            }

            EnterDazedState();
        }

        private void EnterDazedState()
        {
            _ailmentTimer = 0f;
            _activeAilment = PMDStatusAilment.None;
            _state = PMDWildState.Dazed;
            _dazeMaxDuration = 14f;
            _dazeTimer = _dazeMaxDuration;
            _captureAttemptsRemaining = 2;
            _isBeingCaptured = false;
            _rb.linearVelocity = Vector2.zero;
            SetMonsterVisible(true, true);
            Animator.Play("Sleep", true);
            PMDCaptureBall.AttachDizzyStars(this);
            PMDVfxPlayer.SpawnFloatingText(transform.position, "★ NGẤT NGƯ! [E] Ném Cầu", new Color(1f, 0.85f, 0.25f), 1.4f);
        }

        public bool TryBeginCapture()
        {
            if (!CanBeCaptured)
                return false;

            _isBeingCaptured = true;
            _rb.linearVelocity = Vector2.zero;
            return true;
        }

        public void SetCaptureVisualHidden(bool hidden)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = !hidden;
        }

        public void EndCaptureAttempt(bool caught, float catchChance)
        {
            _isBeingCaptured = false;

            if (caught)
            {
                _captureAttemptsRemaining = 0;
                return;
            }

            _captureAttemptsRemaining = Mathf.Max(0, _captureAttemptsRemaining - 1);
            if (_captureAttemptsRemaining > 0)
            {
                Animator.Play("Sleep", true);
                PMDVfxPlayer.SpawnFloatingText(
                    transform.position,
                    $"Trượt rồi! ({catchChance * 100f:0}%) • Còn {_captureAttemptsRemaining} lượt",
                    new Color(1f, 0.52f, 0.42f),
                    1.25f);
            }
            else
            {
                DismissToRespawn($"Thoát mất rồi! ({catchChance * 100f:0}%)");
            }
        }

        public void DismissToRespawn(string message = null)
        {
            _isBeingCaptured = false;
            _captureAttemptsRemaining = 0;
            _dazeTimer = 0f;
            _state = PMDWildState.Fainted;
            _faintRespawnTimer = 20f;
            _rb.linearVelocity = Vector2.zero;
            SetMonsterVisible(false, true);

            if (!string.IsNullOrEmpty(message))
                PMDVfxPlayer.SpawnFloatingText(transform.position, message, new Color(0.95f, 0.65f, 0.4f), 1.15f);
        }

        private void SetMonsterVisible(bool visible, bool toggleCollider)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = visible;

            if (toggleCollider)
            {
                var col = GetComponent<Collider2D>();
                if (col != null)
                    col.enabled = visible;
            }
        }

        public void ApplyVulnerabilityMark(float mult, float duration)
        {
            if (IsFainted || IsRecruited) return;
            _markedMultiplier = Mathf.Max(1.1f, mult);
            _markedTimer = Mathf.Max(_markedTimer, duration);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), $"🎯 ĐÁNH DẤU (+{(mult - 1f) * 100f:0}% Dmg)", new Color(1f, 0.72f, 0.25f), 0.95f);
        }

        public bool HasVulnerabilityMark => _markedTimer > 0f;

        public float ConsumeVulnerabilityMark()
        {
            if (_markedTimer <= 0f) return 0f;
            _markedTimer = 0f;
            return _markedMultiplier;
        }

        public void ApplyTimeBomb(float duration)
        {
            if (IsFainted || IsRecruited) return;
            _timeBombTimer = Mathf.Max(_timeBombTimer, duration);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), "⏱️ BOM THỜI GIAN!", new Color(0.42f, 0.92f, 1f), 1.0f);
        }

        private void DetonateTimeBomb()
        {
            if (IsFainted || IsRecruited)
            {
                _timeBombStoredDamage = 0f;
                return;
            }

            float burst = Mathf.Max(18f, Mathf.Round(_timeBombStoredDamage));
            _timeBombStoredDamage = 0f;
            PMDAudio.Play(PMDSfx.Ultimate, 0.85f);
            PMDVfxPlayer.SpawnVfx("Explosion.None", transform.position, 1.6f, 26f);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), $"⏱️ NỔ TRỄ -{burst:0}!", new Color(0.45f, 0.95f, 1f), 1.2f);
            TakeDamage(burst, Vector2.zero, new Color(0.3f, 0.78f, 1f));
        }

        public void ApplyStatusAilment(PMDStatusAilment ailment, float duration)
        {
            if (ailment == PMDStatusAilment.None || IsFainted || IsRecruited)
                return;

            bool isNew = (_activeAilment != ailment || _ailmentTimer <= 0.1f);
            _activeAilment = ailment;
            _ailmentTimer = Mathf.Max(_ailmentTimer, isBoss ? duration * 0.6f : duration);
            if (isNew)
            {
                _ailmentTickTimer = 0.35f;
                PMDBespokeSkillEngine.SpawnStatusAppliedBurst(transform, ailment, _ailmentTimer);
            }
        }

        private void TryInflictElementalAilment(PMDElement atk)
        {
            if (currentHp <= 0f || Random.value > 0.55f)
                return;

            switch (atk)
            {
                case PMDElement.Electric:
                    ApplyStatusAilment(PMDStatusAilment.Paralyzed, 2.8f);
                    break;
                case PMDElement.Fire:
                    ApplyStatusAilment(PMDStatusAilment.Burned, 3.0f);
                    break;
                case PMDElement.Ice:
                    ApplyStatusAilment(PMDStatusAilment.Frozen, 1.65f);
                    break;
                case PMDElement.Grass:
                case PMDElement.Poison:
                case PMDElement.Bug:
                    ApplyStatusAilment(PMDStatusAilment.Poisoned, 3.2f);
                    break;
                case PMDElement.Water:
                case PMDElement.Ground:
                case PMDElement.Rock:
                    ApplyStatusAilment(PMDStatusAilment.SoakedSlow, 3.0f);
                    break;
                case PMDElement.Psychic:
                case PMDElement.Fighting:
                case PMDElement.Ghost:
                case PMDElement.Dark:
                case PMDElement.Fairy:
                    ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.35f);
                    break;
            }
        }

        private void TickStatusAilmentEffect()
        {
            if (IsFainted || IsRecruited)
                return;

            PMDBespokeSkillEngine.SpawnStatusTickJuice(transform.position, _activeAilment);

            if (_activeAilment == PMDStatusAilment.Burned)
            {
                float dot = Mathf.Max(2f, Mathf.Round(maxHp * (isBoss ? 0.025f : 0.055f)));
                currentHp = Mathf.Max(0f, currentHp - dot);
                PMDHitFlash.Flash(gameObject, new Color(1f, 0.45f, 0.12f), 0.10f);
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.25f, 0f), $"-{dot:0} 🔥Bỏng", new Color(1f, 0.50f, 0.18f), 0.7f);
                if (currentHp <= 0f)
                    Faint();
            }
            else if (_activeAilment == PMDStatusAilment.Poisoned)
            {
                float dot = Mathf.Max(2f, Mathf.Round(maxHp * (isBoss ? 0.025f : 0.06f)));
                currentHp = Mathf.Max(0f, currentHp - dot);
                PMDHitFlash.Flash(gameObject, new Color(0.75f, 0.28f, 0.95f), 0.10f);
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.25f, 0f), $"-{dot:0} ☠️Độc", new Color(0.82f, 0.42f, 1f), 0.7f);
                if (PMDPlayerController.Instance != null)
                    PMDPlayerController.Instance.Heal(2f);
                if (currentHp <= 0f)
                    Faint();
            }
            else if (_activeAilment == PMDStatusAilment.Paralyzed)
            {
                _rb.linearVelocity = Vector2.zero;
                transform.position += new Vector3(Random.Range(-0.05f, 0.05f), 0f, 0f);
            }
        }

        /// <summary>
        /// Damage received while being a recruited follower.
        /// </summary>
        public void TakeDamageAsFollower(float dmg)
        {
            if (_state != PMDWildState.FollowLeader)
                return;

            dmg = Mathf.Max(1f, Mathf.Round(dmg * PMDGameState.PlayerDefenseMultiplier));
            currentHp = Mathf.Max(0f, currentHp - dmg);
            PMDHitFlash.Flash(gameObject, new Color(1f, 0.45f, 0.45f), 0.12f);
            PMDVfxPlayer.SpawnFloatingText(transform.position, $"-{dmg:0}", new Color(1f, 0.55f, 0.55f));
            if (currentHp <= 0f)
            {
                _state = PMDWildState.Fainted;
                _faintRespawnTimer = 15f;
                _rb.linearVelocity = Vector2.zero;
                Animator.Play("Sleep", true);
                PMDVfxPlayer.SpawnFloatingText(transform.position, "Đồng đội gục ngã! (hồi sau 15s)", new Color(1f, 0.6f, 0.4f), 1.4f);
                PMDAudio.Play(PMDSfx.Faint, 0.7f);
            }
            else
            {
                Animator.PlayOneShot("Hurt", "Idle");
            }
        }

        public void HealFull()
        {
            currentHp = maxHp;
            if (_state == PMDWildState.Fainted && _wasRecruited)
            {
                _state = PMDWildState.FollowLeader;
                Animator.Play("Idle", true, true);
            }
        }

        public void SetPartySlot(int slot)
        {
            _partySlotIndex = slot;
        }

        public void SyncFollowerLevel(int playerLevel)
        {
            level = Mathf.Max(level, playerLevel);
            float newMax = Mathf.Round(90f * (1f + (level - 1) * 0.12f));
            currentHp += Mathf.Max(0f, newMax - maxHp);
            maxHp = Mathf.Max(maxHp, newMax);
        }

        public void RecruitToParty(PMDPlayerController leader, int slotIndex)
        {
            _leader = leader;
            _partySlotIndex = slotIndex;
            isAggressive = false;
            _wasRecruited = true;
            _isBeingCaptured = false;
            _captureAttemptsRemaining = 0;
            _dazeTimer = 0f;
            SetMonsterVisible(true, true);
            SyncFollowerLevel(PMDGameState.CurrentLevel);
            currentHp = maxHp;
            _state = PMDWildState.FollowLeader;
            Animator.PlayOneShot("Hop", "Idle");
        }

        /// <summary>
        /// Swaps this recruited companion into a newly chosen Pokémon sent out from the Trainer's Poké Ball to hunt.
        /// </summary>
        public void SwapCompanionSpecies(PMDCharacterData newSpecies, Vector2 summonPos)
        {
            if (newSpecies == null)
                return;

            if (Animator != null)
            {
                Animator.CharacterData = newSpecies;
            }
            gameObject.name = $"Partner_{newSpecies.pokemonName}";
            _skills = PMDSkillDatabase.GetSkillsForPokemon(newSpecies.pokemonName);
            _wasRecruited = true;
            _isBeingCaptured = false;
            _captureAttemptsRemaining = 0;
            _dazeTimer = 0f;
            SetMonsterVisible(true, true);
            SyncFollowerLevel(PMDGameState.CurrentLevel);
            currentHp = maxHp;
            _state = PMDWildState.FollowLeader;
            transform.position = summonPos;
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;

            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", summonPos, 1.35f, 26f);
            PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", summonPos + new Vector2(0f, 0.2f), 1.25f, 24f);
            Animator.PlayOneShot("Hop", "Idle");
        }

        /// <summary>
        /// Called when the Human Trainer commands this companion Pokémon to attack toward the mouse cursor.
        /// </summary>
        public void ExecuteTrainerCommand(Vector2 aimDir, int slotIndex, bool isPrimaryHunter = false)
        {
            if (_state != PMDWildState.FollowLeader || IsFainted)
                return;

            EnsureSkillsLoaded();
            if (_skills == null || _skills.Length == 0)
                return;

            Vector2 dir = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector2.down;
            if (isPrimaryHunter && _leader != null && Vector2.Distance(transform.position, _leader.transform.position) > 2.4f)
            {
                transform.position = (Vector2)_leader.transform.position + dir * 0.95f;
            }

            Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dir, Animator.CurrentDirection);

            PMDSkillData cmdSkill = _skills[Mathf.Clamp(slotIndex, 0, _skills.Length - 1)];
            _skillCooldown = Mathf.Min(_skillCooldown, 0.8f);

            if (slotIndex == 3)
            {
                currentHp = Mathf.Min(maxHp, currentHp + maxHp * 0.30f);
                PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", transform.position, 1.2f, 24f, null, transform);
            }

            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.42f, 0f), $"⚡ {PokemonName}: {cmdSkill.skillName}!", cmdSkill.elementColor, 0.85f);
            Animator.PlayOneShot(cmdSkill.animName, "Idle", () =>
            {
                if (this == null || IsFainted)
                    return;

                PMDSkillComboRunner.ExecuteSkillPattern(this, transform, cmdSkill, dir, true);
            });
        }

        private void ChooseNextAmbientState()
        {
            float roll = Random.value;
            if (roll < 0.42f)
            {
                _state = PMDWildState.Idle;
                _stateTimer = Random.Range(1.8f, 4.0f);
            }
            else if (roll < 0.85f || isBoss)
            {
                _state = PMDWildState.Wander;
                _wanderTarget = _homePosition + Random.insideUnitCircle * (isBoss ? 2.0f : 4.5f);
                _stateTimer = Random.Range(2.5f, 5.0f);
            }
            else
            {
                _state = PMDWildState.Sleep;
                _stateTimer = Random.Range(4.0f, 7.0f);
            }
        }
    }
}
