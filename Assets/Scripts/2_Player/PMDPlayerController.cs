using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace POKE.PMDCollab.OpenWorld
{
    [RequireComponent(typeof(PMDSpriteAnimator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PMDPlayerController : MonoBehaviour
    {
        public static PMDPlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runMultiplier = 1.65f;

        [Header("Combat & Health")]
        [SerializeField] private float maxHp = 150f;
        [SerializeField] private float currentHp = 150f;

        private PMDSpriteAnimator _animator;
        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _isRunning;
        private float _recruitCooldownTimer;
        private Vector2 _dashVelocity;
        private float _dashTimer;
        private float _footstepTimer;
        private float _invulnTimer;
        private float _shieldAmount;
        private float _shieldTimer;
        private float _parryTimer;
        private float _domainTimer;
        private float _buffSpeedMult = 1f;
        private float _buffTimer;
        private PMDOpenWorldMapGenerator _cachedMapGen;

        private readonly List<PMDWildPokemonAI> _recruitedParty = new List<PMDWildPokemonAI>();
        private List<PMDCharacterData> _availablePlayableCharacters = new List<PMDCharacterData>();
        private int _currentCharacterIndex;
        private PMDCharacterData _trainerCharacterData;
        private PMDCharacterData _selectedPokeballPokemon;
        private readonly Dictionary<string, float> _pokeballHpBySpecies = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
        private bool _isThrowingPokeball;
        private float _trainerWarningCooldown;
        private GameObject _trainerFollowerGo;
        private PMDSpriteAnimator _trainerFollowerAnim;
        private SpriteRenderer _trainerFollowerSr;
        private bool _isAutoTrainEnabled;
        private bool _hasAutoAim;
        private Vector2 _autoAimDir = Vector2.down;
        private float _autoSkillDelay;

        public bool IsAutoTrainEnabled => _isAutoTrainEnabled;

        private PMDSkillData[] _currentSkills = new PMDSkillData[4];
        private readonly float[] _skillCooldownTimers = new float[4];

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public bool IsPokemonOut => !IsTrainerMode;
        public PMDCharacterData SelectedPokeballPokemon => _selectedPokeballPokemon;
        public float DisplayedPokemonHp => IsPokemonOut ? currentHp : GetStoredPokemonHp(ActiveHuntingSpeciesName);
        public float DisplayedPokemonMaxHp => maxHp;
        public IReadOnlyList<PMDWildPokemonAI> RecruitedParty => _recruitedParty;
        public PMDSpriteAnimator Animator => _animator != null ? _animator : (_animator = GetComponent<PMDSpriteAnimator>());
        public PMDSkillData[] CurrentSkills => _currentSkills;
        public string SpeciesName => Animator != null && Animator.CharacterData != null ? Animator.CharacterData.pokemonName : "Trainer";
        public bool IsTrainerMode => string.Equals(SpeciesName, "Trainer", System.StringComparison.OrdinalIgnoreCase);

        public PMDWildPokemonAI ActiveHuntingPokemon
        {
            get
            {
                CleanupParty();
                return _recruitedParty.Count > 0 ? _recruitedParty[0] : null;
            }
        }

        public string ActiveHuntingSpeciesName
        {
            get
            {
                if (!IsTrainerMode)
                    return SpeciesName;
                if (_selectedPokeballPokemon != null)
                    return _selectedPokeballPokemon.pokemonName;
                var hunter = ActiveHuntingPokemon;
                if (hunter != null)
                    return hunter.PokemonName;
                return "Pikachu";
            }
        }

        public float GetStoredPokemonHp(string speciesName)
        {
            if (!string.IsNullOrEmpty(speciesName) && _pokeballHpBySpecies.TryGetValue(speciesName, out float hp))
                return Mathf.Clamp(hp, 0f, maxHp);
            return maxHp;
        }

        public float GetSkillCooldownRemaining(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _skillCooldownTimers.Length)
                return 0f;
            return Mathf.Max(0f, _skillCooldownTimers[slotIndex]);
        }

        private void Awake()
        {
            Instance = this;
            _animator = GetComponent<PMDSpriteAnimator>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            RefreshSkillSet();
            SyncStatsWithLevel(true);
        }

        public void SetAvailableCharacters(List<PMDCharacterData> characters)
        {
            _availablePlayableCharacters = characters ?? new List<PMDCharacterData>();
            _trainerCharacterData = null;
            PMDCharacterData fallbackPoke = null;
            for (int i = 0; i < _availablePlayableCharacters.Count; i++)
            {
                var c = _availablePlayableCharacters[i];
                if (c == null) continue;
                if (c.pokemonId == "0000" && _trainerCharacterData == null)
                    _trainerCharacterData = c;
                else if (c.pokemonId != "0000")
                {
                    if (fallbackPoke == null || c.pokemonId == "0025")
                        fallbackPoke = c;
                }
            }
            if (_selectedPokeballPokemon == null)
                _selectedPokeballPokemon = fallbackPoke;
            RefreshSkillSet();
        }

        public void SetActivePokeballSpecies(PMDCharacterData speciesData)
        {
            if (speciesData == null || speciesData.pokemonId == "0000")
                return;
            _selectedPokeballPokemon = speciesData;
            if (!_pokeballHpBySpecies.ContainsKey(speciesData.pokemonName))
                _pokeballHpBySpecies[speciesData.pokemonName] = maxHp;
            RefreshSkillSet();
        }

        public void RefreshSkillSet()
        {
            _currentSkills = PMDSkillDatabase.GetSkillsForPokemon(ActiveHuntingSpeciesName);
        }

        public void SyncStatsWithLevel(bool fullHeal)
        {
            float oldMax = maxHp;
            maxHp = PMDGameState.PlayerMaxHp;
            if (fullHeal)
                currentHp = maxHp;
            else
                currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(0f, maxHp - oldMax));
        }

        public void SetCurrentHp(float hp)
        {
            currentHp = Mathf.Clamp(hp, 1f, maxHp);
        }

        private void Update()
        {
            for (int i = 0; i < _skillCooldownTimers.Length; i++)
            {
                if (_skillCooldownTimers[i] > 0f)
                    _skillCooldownTimers[i] -= Time.deltaTime;
            }

            if (_recruitCooldownTimer > 0f)
                _recruitCooldownTimer -= Time.deltaTime;
            if (_trainerWarningCooldown > 0f)
                _trainerWarningCooldown -= Time.deltaTime;
            if (_invulnTimer > 0f)
                _invulnTimer -= Time.deltaTime;
            if (_shieldTimer > 0f)
            {
                _shieldTimer -= Time.deltaTime;
                if (_shieldTimer <= 0f) _shieldAmount = 0f;
            }
            if (_parryTimer > 0f)
                _parryTimer -= Time.deltaTime;
            if (_domainTimer > 0f)
                _domainTimer -= Time.deltaTime;
            if (_buffTimer > 0f)
            {
                _buffTimer -= Time.deltaTime;
                if (_buffTimer <= 0f) _buffSpeedMult = 1f;
            }

            if (PMDGameState.IsInputBlocked)
            {
                _moveInput = Vector2.zero;
                _isRunning = false;
                return;
            }

            ReadInput();
            UpdateAnimation();
            UpdateFootsteps();
            UpdateTrainerFollower();
            CheckNearbyMapItemPickup();
        }

        private void FixedUpdate()
        {
            if (_dashTimer > 0f)
            {
                _dashTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _dashVelocity;
                return;
            }

            if (Animator != null && Animator.IsPlayingOneShot)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float speed = walkSpeed * (_isRunning ? runMultiplier : 1f) * (_buffTimer > 0f ? _buffSpeedMult : 1f);
            _rb.linearVelocity = _moveInput * speed;
        }

        private void ReadInput()
        {
            Vector2 input = Vector2.zero;
            bool runPressed = false;
            bool skill1Pressed = false; // [J] or [Space] - Melee
            bool skill2Pressed = false; // [K] - Ranged Projectile
            bool skill3Pressed = false; // [L] - Linear Shockwave Dash
            bool skill4Pressed = false; // [U] - Ultimate + Heal
            bool recruitPressed = false;
            bool swapCharPressed = false;
            bool interactPressed = false;
            bool quickHealPressed = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;

                runPressed = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                skill1Pressed = kb.jKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame;
                skill2Pressed = kb.kKey.wasPressedThisFrame || kb.digit2Key.wasPressedThisFrame;
                skill3Pressed = kb.lKey.wasPressedThisFrame || kb.digit3Key.wasPressedThisFrame;
                skill4Pressed = kb.uKey.wasPressedThisFrame || kb.digit4Key.wasPressedThisFrame || kb.hKey.wasPressedThisFrame;
                recruitPressed = kb.eKey.wasPressedThisFrame;
                swapCharPressed = kb.tabKey.wasPressedThisFrame;
                interactPressed = kb.fKey.wasPressedThisFrame;
                quickHealPressed = kb.qKey.wasPressedThisFrame;
                if (kb.rKey.wasPressedThisFrame)
                    TogglePokeballControl();
                if (kb.tKey.wasPressedThisFrame)
                    ToggleAutoTrain();
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && GUIUtility.hotControl == 0)
                    skill1Pressed = true;
                if (Mouse.current.rightButton.wasPressedThisFrame && GUIUtility.hotControl == 0)
                    skill2Pressed = true;
            }

            _moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
            _isRunning = runPressed;
            _hasAutoAim = false;

            if (_isAutoTrainEnabled && input.sqrMagnitude < 0.01f && !Animator.IsPlayingOneShot)
            {
                UpdateAutoTrain();
            }

            if (Animator != null && !Animator.IsPlayingOneShot)
            {
                if (_moveInput.sqrMagnitude > 0.01f)
                {
                    // Khi đang chạy (trên 4 chân), hướng mặt theo đúng chiều di chuyển để không bị chạy giật lùi (moonwalk)
                    Animator.CurrentDirection = PMDDirectionUtility.FromVector2(_moveInput, Animator.CurrentDirection);
                }
                else
                {
                    // Khi đứng yên (trên 2 chân), xoay người nhìn theo con trỏ chuột
                    Vector2 mouseDir = GetMouseAimDirection();
                    if (mouseDir.sqrMagnitude > 0.01f)
                    {
                        Animator.CurrentDirection = PMDDirectionUtility.FromVector2(mouseDir, Animator.CurrentDirection);
                    }
                }
            }

            if (interactPressed)
            {
                var it = PMDInteractable.FindNearest(transform.position);
                if (it != null)
                {
                    it.Interact(this);
                    return;
                }
            }

            if (skill1Pressed) TryCastSkillSlot(0);
            else if (skill2Pressed) TryCastSkillSlot(1);
            else if (skill3Pressed) TryCastSkillSlot(2);
            else if (skill4Pressed) TryCastSkillSlot(3);

            if (recruitPressed && _recruitCooldownTimer <= 0f)
            {
                TryRecruitNearbyPokemon();
            }

            if (swapCharPressed)
            {
                CyclePlayableCharacter();
            }

            if (quickHealPressed)
            {
                QuickUseHealingItem();
            }
        }

        private void UpdateAnimation()
        {
            if (Animator == null || Animator.IsPlayingOneShot)
            {
                if (Animator != null)
                    Animator.PlaybackSpeed = 1f;
                return;
            }

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Animator.PlaybackSpeed = _isRunning ? 1.35f : 1f;
                Animator.Play("Walk", true);
            }
            else
            {
                Animator.PlaybackSpeed = 1f;
                Animator.Play("Idle", true);
            }
        }

        private void UpdateFootsteps()
        {
            if (_moveInput.sqrMagnitude < 0.01f)
            {
                _footstepTimer = 0f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _isRunning ? 0.22f : 0.33f;
                PMDAudio.Play(PMDSfx.Footstep, 0.6f);

                // Electric cheek/tail spark trail when Pikachu gallops on 4 legs!
                if (_isRunning && string.Equals(SpeciesName, "Pikachu", System.StringComparison.OrdinalIgnoreCase))
                {
                    PMDVfxPlayer.SpawnVfx("Spark.None", transform.position + new Vector3(0f, 0.15f, 0f), 0.55f, 32f);
                }
            }
        }

        public bool TryGetMouseWorldPosition(out Vector2 mouseWorld)
        {
            mouseWorld = transform.position;
            if (Mouse.current == null)
                return false;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y) || float.IsInfinity(screenPos.x) || float.IsInfinity(screenPos.y))
                return false;

            float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
            mouseWorld = new Vector2(world.x, world.y);
            return true;
        }

        public Vector2 GetMouseAimDirection()
        {
            if (_isAutoTrainEnabled && _hasAutoAim && _autoAimDir.sqrMagnitude > 0.001f)
                return _autoAimDir;

            if (TryGetMouseWorldPosition(out Vector2 mouseWorld))
            {
                Vector2 toMouse = mouseWorld - (Vector2)transform.position;
                if (toMouse.sqrMagnitude > 0.0025f)
                    return toMouse.normalized;
            }

            if (_moveInput.sqrMagnitude > 0.01f)
                return _moveInput.normalized;

            return Animator != null ? Animator.CurrentDirection.ToVector2() : Vector2.down;
        }

        public void TryCastSkillSlot(int slotIndex)
        {
            if (PMDGameState.IsInputBlocked)
                return;

            if (_currentSkills == null || slotIndex < 0 || slotIndex >= _currentSkills.Length)
                return;

            if (_skillCooldownTimers[slotIndex] > 0f)
                return;

            PMDSkillData skill = _currentSkills[slotIndex];
            if (skill == null)
                return;

            float cdrMult = _domainTimer > 0f ? 0.5f : 1f;
            _skillCooldownTimers[slotIndex] = skill.cooldown * cdrMult;
            Vector2 aimDir = GetMouseAimDirection();
            if (Animator != null)
            {
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(aimDir, Animator.CurrentDirection);
            }

            // When controlling the Human Trainer, Pokémon is still inside the Pokéball!
            if (IsTrainerMode)
            {
                if (_trainerWarningCooldown <= 0f)
                {
                    _trainerWarningCooldown = 0.8f;
                    PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.45f, 0f), "🔴 Bấm [R] để ném Pokéball tung Pokémon!", new Color(1f, 0.85f, 0.3f), 1.0f);
                }
                return;
            }

            // Controlling the active summoned Pokémon directly!
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.4f, 0f), $"★ {skill.skillName}!", skill.elementColor, 0.85f);
            string animToPlay = !string.IsNullOrEmpty(skill.animName) ? skill.animName : "Attack";
            Animator.PlayOneShot(animToPlay, "Idle", () => ExecuteSkillEffect(skill, aimDir));
        }

        public void ResetAllCooldowns()
        {
            for (int i = 0; i < _skillCooldownTimers.Length; i++)
                _skillCooldownTimers[i] = 0f;
        }

        private void ExecuteSkillEffect(PMDSkillData skill, Vector2 facingDir)
        {
            Vector2 aimDir = facingDir.sqrMagnitude > 0.001f ? facingDir.normalized : GetMouseAimDirection();
            if (Animator != null)
            {
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(aimDir, Animator.CurrentDirection);
            }

            if (skill.pattern == PMDSkillPattern.HighSpeedRamDash)
            {
                _invulnTimer = 0.35f;
            }

            PMDSkillComboRunner.ExecuteSkillPattern(this, transform, skill, aimDir, true);
        }

        private Vector2 FindBestTargetDirection(Vector2 fallbackFacing, float maxRange)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            float bestScore = float.MaxValue;
            Vector2 bestDir = fallbackFacing;

            for (int i = 0; i < allWild.Count; i++)
            {
                var wild = allWild[i];
                if (wild == null || wild.IsFainted || wild.IsRecruited)
                    continue;

                Vector2 toWild = wild.transform.position - transform.position;
                float dist = toWild.magnitude;
                if (dist > maxRange || dist < 0.1f)
                    continue;

                Vector2 dir = toWild / dist;
                float dot = Vector2.Dot(fallbackFacing, dir);
                if (dot >= 0.35f && dist < bestScore)
                {
                    bestScore = dist;
                    bestDir = dir;
                }
            }

            return bestDir;
        }

        private void CheckNearbyMapItemPickup()
        {
            if (_cachedMapGen == null)
                _cachedMapGen = PMDOpenWorldMapGenerator.Instance != null
                    ? PMDOpenWorldMapGenerator.Instance
                    : Object.FindFirstObjectByType<PMDOpenWorldMapGenerator>();

            var mapGen = _cachedMapGen;
            if (mapGen == null || mapGen.SpawnedItems == null)
                return;

            for (int i = mapGen.SpawnedItems.Count - 1; i >= 0; i--)
            {
                var item = mapGen.SpawnedItems[i];
                if (item == null || item.IsCollected)
                    continue;

                if (Vector2.Distance(transform.position, item.transform.position) <= 0.85f)
                {
                    item.Collect(this);
                }
            }
        }

        public void Heal(float amount)
        {
            float before = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", transform.position, 1.8f, 24f, null, transform);
            PMDVfxPlayer.SpawnFloatingText(transform.position, $"+{currentHp - before:0} HP", new Color(0.35f, 1f, 0.45f));
        }

        private void QuickUseHealingItem()
        {
            var gs = PMDGameState.Instance;
            if (gs == null)
                return;

            if (currentHp >= maxHp - 0.5f)
            {
                gs.PushToast("HP đã đầy!", new Color(0.8f, 0.9f, 1f));
                return;
            }

            string item = gs.FindBestHealingItem(maxHp - currentHp);
            if (item == null)
            {
                gs.PushToast("Không còn vật phẩm hồi máu! (Mua ở Bưu Điện - Vùng 1)", new Color(1f, 0.5f, 0.4f));
                PMDAudio.Play(PMDSfx.Error);
                return;
            }

            string name = PMDGameState.GetItemInfo(item).displayName;
            if (gs.UseItem(item))
                gs.PushToast($"Đã dùng {name}", new Color(0.5f, 1f, 0.6f));
        }

        // =========================================================================================
        // CAPTURE & RECRUITMENT (Đánh 0 HP cho ngất ngư, bấm [E] ném cầu hoặc dùng Apricorn)
        // =========================================================================================
        private PMDWildPokemonAI FindClosestWild(float radius, bool dazedOnly)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            PMDWildPokemonAI closest = null;
            float bestDist = radius;

            for (int i = 0; i < allWild.Count; i++)
            {
                var wild = allWild[i];
                if (wild == null || wild.IsRecruited || wild.IsBoss)
                    continue;

                if (dazedOnly)
                {
                    if (!wild.CanBeCaptured)
                        continue;
                }
                else if (wild.IsFainted)
                {
                    continue;
                }

                float d = Vector2.Distance(transform.position, wild.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = wild;
                }
            }
            return closest;
        }

        private bool IsPartyFull()
        {
            CleanupParty();
            return _recruitedParty.Count >= PMDGameState.MaxPartySize - 1;
        }

        private void CleanupParty()
        {
            for (int i = _recruitedParty.Count - 1; i >= 0; i--)
                if (_recruitedParty[i] == null)
                    _recruitedParty.RemoveAt(i);
        }

        private void TryRecruitNearbyPokemon()
        {
            _recruitCooldownTimer = 0.45f;
            var dazed = FindClosestWild(5.5f, true);
            if (dazed != null)
            {
                ThrowBallAtTarget(dazed, false);
                return;
            }

            var livingWild = FindClosestWild(4.8f, false);
            if (livingWild != null)
            {
                PMDVfxPlayer.SpawnFloatingText(livingWild.transform.position, "Chưa ngất ngư!", new Color(1f, 0.65f, 0.35f), 1.0f);
                PMDGameState.Toast($"Hãy đánh {livingWild.PokemonName} hết máu (0 HP) cho ngất ngư rồi bấm [E] ném cầu!", new Color(1f, 0.72f, 0.42f));
                PMDAudio.Play(PMDSfx.Error);
            }
        }

        public bool TryRecruitWithApricorn()
        {
            var dazed = FindClosestWild(6.5f, true);
            if (dazed == null)
            {
                PMDGameState.Toast("Hãy đánh Pokémon hoang dã về 0 HP (ngất ngư) gần bạn rồi dùng Apricorn để tăng tỉ lệ bắt!", new Color(1f, 0.7f, 0.4f));
                return false;
            }

            return ThrowBallAtTarget(dazed, true);
        }

        private bool ThrowBallAtTarget(PMDWildPokemonAI target, bool isApricornBoost)
        {
            if (target == null)
                return false;

            Vector2 aimDir = ((Vector2)(target.transform.position - transform.position)).normalized;
            if (aimDir.sqrMagnitude > 0.001f && Animator != null)
            {
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(aimDir, Animator.CurrentDirection);
                Animator.PlayOneShot("Attack", "Idle");
            }

            return PMDCaptureBall.SpawnCaptureThrow(this, target, isApricornBoost);
        }

        public void CompleteCapturedPokemon(PMDWildPokemonAI mon, float catchChance)
        {
            if (mon == null)
                return;

            if (!IsPartyFull())
            {
                AddRecruit(mon, catchChance);
                return;
            }

            int bonusCoins = 30 + mon.Level * 5;
            int bonusExp = 20 + mon.Level * 4;
            mon.DismissToRespawn();
            PMDAudio.Play(PMDSfx.Recruit);
            PMDVfxPlayer.SpawnFloatingText(mon.transform.position, $"★ ĐÃ BẮT VỀ KHO! (+{bonusCoins} Poké)", new Color(0.45f, 0.95f, 1f), 1.35f);

            if (PMDGameState.Instance != null)
            {
                PMDGameState.Instance.AddCoins(bonusCoins);
                PMDGameState.Instance.AddExp(bonusExp);
                PMDGameState.Instance.RegisterRecruit();
                PMDGameState.Toast($"Đội đã đầy ({PMDGameState.MaxPartySize - 1}/3) — đã gửi {mon.PokemonName} về Kho Hội! (+{bonusCoins} Poké, +{bonusExp} EXP)", new Color(0.45f, 0.95f, 1f));
            }
        }

        private void AddRecruit(PMDWildPokemonAI mon, float catchChance)
        {
            mon.RecruitToParty(this, _recruitedParty.Count + 1);
            _recruitedParty.Add(mon);
            if (!_pokeballHpBySpecies.ContainsKey(mon.PokemonName))
                _pokeballHpBySpecies[mon.PokemonName] = maxHp;

            // Keep recruited Pokémon safely inside its Pokéball so only 1 Pokémon is ever out on the field!
            mon.gameObject.SetActive(false);

            RefreshSkillSet();
            PMDVfxPlayer.SpawnFloatingText(mon.transform.position, $"★ THU VÀO BÓNG! ({catchChance * 100f:0}%)", new Color(0.35f, 1f, 0.6f), 1.35f);
            PMDAudio.Play(PMDSfx.Recruit);
            PMDGameState.Toast($"★ Đã bắt {mon.PokemonName} vào Pokéball! (Bấm vào tên trong Đội để ném ra)", new Color(0.45f, 1f, 0.65f));
            if (PMDGameState.Instance != null)
                PMDGameState.Instance.RegisterRecruit();

            var mgr = PMDOpenWorldManager.Instance;
            if (mgr != null)
                mgr.AdoptCompanion(mon);
        }

        public void RegisterExistingCompanion(PMDWildPokemonAI mon)
        {
            if (mon != null && !_recruitedParty.Contains(mon))
            {
                _recruitedParty.Add(mon);
                RefreshSkillSet();
            }
        }

        public void ReleaseCompanion(PMDWildPokemonAI mon)
        {
            if (mon == null)
                return;
            _recruitedParty.Remove(mon);
            PMDVfxPlayer.SpawnVfx("Circle_Blue_Out.None", mon.transform.position, 1.2f, 26f);
            Destroy(mon.gameObject);
            for (int i = 0; i < _recruitedParty.Count; i++)
                if (_recruitedParty[i] != null)
                    _recruitedParty[i].SetPartySlot(i + 1);
            RefreshSkillSet();
        }

        public void ClearParty()
        {
            foreach (var mon in _recruitedParty)
                if (mon != null)
                    Destroy(mon.gameObject);
            _recruitedParty.Clear();
        }

        // =========================================================================================
        // HUNTING POKÉMON SELECTION, SWAPPING & EVOLUTION
        // =========================================================================================
        public void PromotePartyMemberToLeadHunter(int partyIndex)
        {
            CleanupParty();
            if (partyIndex < 0 || partyIndex >= _recruitedParty.Count)
                return;

            var chosen = _recruitedParty[partyIndex];
            if (chosen == null) return;

            _recruitedParty.RemoveAt(partyIndex);
            _recruitedParty.Insert(0, chosen);

            for (int i = 0; i < _recruitedParty.Count; i++)
            {
                if (_recruitedParty[i] != null)
                {
                    _recruitedParty[i].SetPartySlot(i + 1);
                    _recruitedParty[i].gameObject.SetActive(false);
                }
            }

            PMDCharacterData charData = null;
            for (int i = 0; i < _availablePlayableCharacters.Count; i++)
            {
                if (_availablePlayableCharacters[i] != null &&
                    string.Equals(_availablePlayableCharacters[i].pokemonName, chosen.PokemonName, System.StringComparison.OrdinalIgnoreCase))
                {
                    charData = _availablePlayableCharacters[i];
                    break;
                }
            }

            if (charData != null)
                SummonOrSwapHuntingPokemon(charData);
        }

        public void ToggleAutoTrain()
        {
            _isAutoTrainEnabled = !_isAutoTrainEnabled;
            _autoSkillDelay = 0f;
            if (_isAutoTrainEnabled)
            {
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), "🤖 AUTO TRAIN: BẬT!", new Color(0.45f, 1f, 0.65f), 1.15f);
                PMDGameState.Toast("🤖 Đã BẬT Auto Train! Pokémon sẽ tự tìm quái, tự đánh & tự tung chiêu!", new Color(0.45f, 1f, 0.65f));
                if (IsTrainerMode && !_isThrowingPokeball)
                    TogglePokeballControl();
            }
            else
            {
                _hasAutoAim = false;
                _moveInput = Vector2.zero;
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), "🤖 AUTO TRAIN: TẮT", new Color(1f, 0.72f, 0.42f), 1.0f);
                PMDGameState.Toast("🤖 Đã TẮT Auto Train — quay lại điều khiển thủ công.", new Color(1f, 0.75f, 0.45f));
            }
        }

        private void UpdateAutoTrain()
        {
            if (IsTrainerMode)
                return;

            if (_autoSkillDelay > 0f)
                _autoSkillDelay -= Time.deltaTime;

            PMDWildPokemonAI target = FindNearestWildTarget(18f);
            if (target == null)
            {
                _moveInput = Vector2.zero;
                _isRunning = false;
                return;
            }

            Vector2 toTarget = (Vector2)(target.transform.position - transform.position);
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.down;
            _hasAutoAim = true;
            _autoAimDir = dir;

            if (Animator != null)
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(dir, Animator.CurrentDirection);

            // Approach target smoothly until in optimal skill range
            if (dist > 1.85f)
            {
                _moveInput = dir;
                _isRunning = dist > 3.2f;
            }
            else
            {
                _moveInput = Vector2.zero;
                _isRunning = false;
            }

            if (_autoSkillDelay > 0f || _currentSkills == null)
                return;

            // Cast the Pokémon's own 4 signature moves automatically off cooldown!
            int chosenSlot = -1;
            if (_currentSkills.Length > 3 && _skillCooldownTimers[3] <= 0f && (dist <= 5.4f || currentHp < maxHp * 0.65f))
                chosenSlot = 3;
            else if (_currentSkills.Length > 2 && _skillCooldownTimers[2] <= 0f && dist <= 5.5f)
                chosenSlot = 2;
            else if (_currentSkills.Length > 1 && _skillCooldownTimers[1] <= 0f && dist <= 7.2f)
                chosenSlot = 1;
            else if (_currentSkills.Length > 0 && _skillCooldownTimers[0] <= 0f && dist <= Mathf.Max(2.6f, _currentSkills[0] != null ? _currentSkills[0].range : 2.6f))
                chosenSlot = 0;

            if (chosenSlot >= 0)
            {
                _autoSkillDelay = 0.28f;
                TryCastSkillSlot(chosenSlot);
            }
        }

        private PMDWildPokemonAI FindNearestWildTarget(float maxRange)
        {
            PMDWildPokemonAI best = null;
            float bestDist = maxRange;
            var allWild = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited || w.IsDazed || !w.gameObject.activeInHierarchy)
                    continue;
                float d = Vector2.Distance(transform.position, w.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = w;
                }
            }
            return best;
        }

        public void TogglePokeballControl()
        {
            if (_isThrowingPokeball)
                return;

            if (IsTrainerMode)
            {
                if (_selectedPokeballPokemon == null)
                    SetActivePokeballSpecies(FindDefaultPokemonData());
                if (_selectedPokeballPokemon != null)
                    StartCoroutine(ThrowPokeballOutRoutine(_selectedPokeballPokemon));
            }
            else
            {
                RecallPokemonToBall(false);
            }
        }

        private PMDCharacterData FindDefaultPokemonData()
        {
            if (_availablePlayableCharacters == null) return null;
            for (int i = 0; i < _availablePlayableCharacters.Count; i++)
            {
                var c = _availablePlayableCharacters[i];
                if (c != null && c.pokemonId != "0000")
                    return c;
            }
            return null;
        }

        public void RecallPokemonToBall(bool fainted = false)
        {
            if (IsTrainerMode)
                return;

            string prevSpecies = SpeciesName;
            _pokeballHpBySpecies[prevSpecies] = fainted ? 0f : Mathf.Clamp(currentHp, 1f, maxHp);

            // Ensure no companion GameObjects remain visible on the field
            for (int i = 0; i < _recruitedParty.Count; i++)
                if (_recruitedParty[i] != null)
                    _recruitedParty[i].gameObject.SetActive(false);

            PMDAudio.Play(PMDSfx.BallThrow);
            PMDVfxPlayer.SpawnVfx("Circle_Red_Out.None", transform.position, 1.55f, 26f);

            // Switch control back to the Human Trainer right where the Trainer follower is standing
            if (_trainerFollowerGo != null && _trainerFollowerGo.activeSelf)
            {
                transform.position = _trainerFollowerGo.transform.position;
                _trainerFollowerGo.SetActive(false);
            }

            if (_trainerCharacterData != null)
                Animator.CharacterData = _trainerCharacterData;

            RefreshSkillSet();
            _rb.linearVelocity = Vector2.zero;

            if (fainted)
            {
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.5f, 0f), $"🔴 {prevSpecies} kiệt sức! Thu vào Pokéball!", new Color(1f, 0.42f, 0.38f), 1.4f);
                PMDGameState.Toast($"🔴 {prevSpecies} đã kiệt sức và tự thu vào Pokéball! Hãy hồi máu hoặc chọn Pokémon khác.", new Color(1f, 0.52f, 0.42f));
            }
            else
            {
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.45f, 0f), $"🔴 Thu hồi {prevSpecies} vào bóng!", new Color(1f, 0.65f, 0.45f), 1.1f);
            }
        }

        private System.Collections.IEnumerator ThrowPokeballOutRoutine(PMDCharacterData speciesData)
        {
            if (speciesData == null || speciesData.pokemonId == "0000")
                yield break;

            float storedHp = GetStoredPokemonHp(speciesData.pokemonName);
            if (storedHp <= 0f)
            {
                // Auto-revive inside Pokéball with 35% HP if player has no other way, or prompt heal
                storedHp = Mathf.Round(maxHp * 0.35f);
                _pokeballHpBySpecies[speciesData.pokemonName] = storedHp;
                PMDGameState.Toast($"💊 Hồi nhẹ {speciesData.pokemonName} ({storedHp:0} HP) để ra trận!", new Color(0.55f, 0.95f, 0.65f));
            }

            _isThrowingPokeball = true;
            _selectedPokeballPokemon = speciesData;
            Vector2 aimDir = GetMouseAimDirection();
            if (Animator != null)
            {
                Animator.CurrentDirection = PMDDirectionUtility.FromVector2(aimDir, Animator.CurrentDirection);
                Animator.PlayOneShot("Attack", "Idle");
            }

            PMDAudio.Play(PMDSfx.BallThrow);
            Vector3 startPos = transform.position + new Vector3(0f, 0.35f, 0f);
            Vector3 targetPos = transform.position + (Vector3)(aimDir * 1.15f);
            PMDVfxPlayer.SpawnFloatingText(startPos, $"🔴 Ra trận đi, {speciesData.pokemonName}!", new Color(1f, 0.92f, 0.38f), 1.15f);

            // Brief Pokéball arc VFX
            var ballVfx = PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", startPos, 0.7f, 28f);
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.18f);
                if (ballVfx != null)
                    ballVfx.transform.position = Vector3.Lerp(startPos, targetPos, u) + new Vector3(0f, Mathf.Sin(u * Mathf.PI) * 0.45f, 0f);
                yield return null;
            }

            // Keep the Human Trainer visibly on the field following behind the controlled Pokémon!
            Vector3 trainerStandPos = IsTrainerMode
                ? transform.position
                : (_trainerFollowerGo != null && _trainerFollowerGo.activeSelf ? _trainerFollowerGo.transform.position : transform.position - (Vector3)(aimDir * 1.15f));

            // Switch player control directly into the chosen 1 Pokémon!
            for (int i = 0; i < _recruitedParty.Count; i++)
                if (_recruitedParty[i] != null)
                    _recruitedParty[i].gameObject.SetActive(false);

            transform.position = targetPos;
            Animator.CharacterData = speciesData;
            ShowTrainerFollower(trainerStandPos, aimDir);
            currentHp = Mathf.Clamp(storedHp, 1f, maxHp);
            _invulnTimer = Mathf.Max(_invulnTimer, 0.45f);
            RefreshSkillSet();
            ResetAllCooldowns();

            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", targetPos, 1.65f, 26f);
            PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", targetPos, 1.5f, 26f);
            PMDGameState.Toast($"★ Đang điều khiển trực tiếp: {speciesData.pokemonName.ToUpper()}! (Bấm [R] để thu vào bóng)", new Color(0.45f, 0.95f, 1f));
            _isThrowingPokeball = false;
        }

        private void ShowTrainerFollower(Vector3 pos, Vector2 facingDir)
        {
            if (_trainerCharacterData == null)
                return;

            if (_trainerFollowerGo == null)
            {
                _trainerFollowerGo = new GameObject("Human_Trainer_Follower");
                _trainerFollowerSr = _trainerFollowerGo.AddComponent<SpriteRenderer>();
                _trainerFollowerAnim = _trainerFollowerGo.AddComponent<PMDSpriteAnimator>();
                _trainerFollowerAnim.CharacterData = _trainerCharacterData;
            }

            _trainerFollowerGo.transform.position = pos;
            _trainerFollowerGo.SetActive(true);
            if (_trainerFollowerAnim != null)
            {
                _trainerFollowerAnim.CurrentDirection = PMDDirectionUtility.FromVector2(facingDir, PMDDirection.Down);
                _trainerFollowerAnim.Play("Idle", true);
            }
        }

        private void UpdateTrainerFollower()
        {
            if (_trainerFollowerGo == null)
                return;

            if (IsTrainerMode)
            {
                if (_trainerFollowerGo.activeSelf)
                    _trainerFollowerGo.SetActive(false);
                return;
            }

            if (!_trainerFollowerGo.activeSelf && _trainerCharacterData != null)
            {
                ShowTrainerFollower(transform.position + new Vector3(-1.3f, 0f, 0f), Vector2.right);
            }

            Vector3 target = transform.position;
            Vector3 cur = _trainerFollowerGo.transform.position;
            Vector2 toPoke = (Vector2)(target - cur);
            float dist = toPoke.magnitude;

            if (dist > 8.5f)
            {
                _trainerFollowerGo.transform.position = target - (Vector3)(toPoke.normalized * 1.5f);
            }
            else if (dist > 1.55f)
            {
                Vector2 dir = toPoke / dist;
                float followSpeed = walkSpeed * (_isRunning ? runMultiplier : 1.05f);
                _trainerFollowerGo.transform.position += (Vector3)(dir * (followSpeed * Time.deltaTime));
                if (_trainerFollowerAnim != null)
                {
                    _trainerFollowerAnim.CurrentDirection = PMDDirectionUtility.FromVector2(dir, _trainerFollowerAnim.CurrentDirection);
                    _trainerFollowerAnim.PlaybackSpeed = _isRunning ? 1.3f : 1f;
                    _trainerFollowerAnim.Play("Walk", true);
                }
            }
            else if (_trainerFollowerAnim != null)
            {
                if (dist > 0.05f)
                    _trainerFollowerAnim.CurrentDirection = PMDDirectionUtility.FromVector2(toPoke / dist, _trainerFollowerAnim.CurrentDirection);
                _trainerFollowerAnim.PlaybackSpeed = 1f;
                _trainerFollowerAnim.Play("Idle", true);
            }

            if (_trainerFollowerSr != null)
                _trainerFollowerSr.sortingOrder = Mathf.RoundToInt(-_trainerFollowerGo.transform.position.y * 100f);
        }

        public void SummonOrSwapHuntingPokemon(PMDCharacterData speciesData)
        {
            if (speciesData == null || _isThrowingPokeball)
                return;

            // If user picked Trainer itself, recall active Pokémon into its Pokéball
            if (speciesData.pokemonId == "0000")
            {
                RecallPokemonToBall(false);
                return;
            }

            // Save current Pokémon's HP if one is already out, then swap to the new 1 Pokémon
            if (!IsTrainerMode)
            {
                _pokeballHpBySpecies[SpeciesName] = Mathf.Clamp(currentHp, 1f, maxHp);
            }

            _selectedPokeballPokemon = speciesData;
            if (!_pokeballHpBySpecies.ContainsKey(speciesData.pokemonName))
                _pokeballHpBySpecies[speciesData.pokemonName] = maxHp;

            StartCoroutine(ThrowPokeballOutRoutine(speciesData));
        }

        public void CyclePlayableCharacter()
        {
            if (_availablePlayableCharacters == null || _availablePlayableCharacters.Count == 0)
                return;

            // Cycle through Pokémon species (skipping index 0 which is the Human Trainer)
            int startIdx = Mathf.Max(1, (_currentCharacterIndex + 1) % _availablePlayableCharacters.Count);
            for (int step = 0; step < _availablePlayableCharacters.Count; step++)
            {
                int idx = (startIdx + step) % _availablePlayableCharacters.Count;
                if (_availablePlayableCharacters[idx] != null && _availablePlayableCharacters[idx].pokemonId != "0000")
                {
                    _currentCharacterIndex = idx;
                    SummonOrSwapHuntingPokemon(_availablePlayableCharacters[idx]);
                    return;
                }
            }
        }

        public void SelectCharacterByIndex(int index)
        {
            if (_availablePlayableCharacters == null || index < 0 || index >= _availablePlayableCharacters.Count)
                return;

            _currentCharacterIndex = index;
            SummonOrSwapHuntingPokemon(_availablePlayableCharacters[index]);
        }

        public void SelectCharacterByName(string speciesName)
        {
            if (_availablePlayableCharacters == null)
                return;
            for (int i = 0; i < _availablePlayableCharacters.Count; i++)
            {
                if (string.Equals(_availablePlayableCharacters[i].pokemonName, speciesName, System.StringComparison.OrdinalIgnoreCase))
                {
                    _currentCharacterIndex = i;
                    SummonOrSwapHuntingPokemon(_availablePlayableCharacters[i]);
                    return;
                }
            }
        }

        public void EvolveInto(PMDCharacterData data)
        {
            if (data == null)
                return;

            int idx = _availablePlayableCharacters.IndexOf(data);
            if (idx >= 0)
                _currentCharacterIndex = idx;

            if (IsTrainerMode && ActiveHuntingPokemon != null)
            {
                ActiveHuntingPokemon.SwapCompanionSpecies(data, ActiveHuntingPokemon.transform.position);
            }
            else
            {
                Animator.CharacterData = data;
            }

            RefreshSkillSet();
            currentHp = maxHp;
            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", transform.position, 2.2f, 22f);
            PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", transform.position, 2.4f, 22f, null, transform);
            PMDVfxPlayer.SpawnFloatingText(transform.position, $"★ TIẾN HÓA: {data.pokemonName}!", new Color(1f, 0.92f, 0.4f), 2f);
        }

        // =========================================================================================
        // DAMAGE, LEVEL UP & GAME OVER
        // =========================================================================================
        public void OnLevelUp()
        {
            SyncStatsWithLevel(true);
            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", transform.position, 1.8f, 24f);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.5f, 0f), $"LÊN CẤP {PMDGameState.CurrentLevel}!", new Color(1f, 0.9f, 0.3f), 1.6f);
            foreach (var mon in _recruitedParty)
                if (mon != null)
                    mon.SyncFollowerLevel(PMDGameState.CurrentLevel);
        }

        /// <summary>
        /// Damage from a wild monster's skill (applies attacker level, type effectiveness and player defense).
        /// </summary>
        public void TakeDamageFrom(float baseDamage, Color elementColor, int attackerLevel, bool isBoss, string moveType = null)
        {
            PMDElement atk = !string.IsNullOrEmpty(moveType)
                ? PMDTypeChart.FromTypeName(moveType)
                : PMDTypeChart.FromColor(elementColor);
            float typeMult = PMDTypeChart.GetMultiplier(atk, PMDTypeChart.GetSpeciesTypes(SpeciesName));
            float lvlMult = 0.8f + attackerLevel * 0.07f;
            float dmg = baseDamage * lvlMult * typeMult * (isBoss ? 1.5f : 1f);
            if (typeMult > 1.01f)
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.35f, 0f), "Siêu hiệu quả!", new Color(1f, 0.4f, 0.3f), 0.8f);
            TakeDamage(dmg);
        }

        public void GrantInvulnerability(float duration)
        {
            _invulnTimer = Mathf.Max(_invulnTimer, duration);
        }

        public void GrantShield(float amount, float duration)
        {
            _shieldAmount = Mathf.Max(_shieldAmount, amount);
            _shieldTimer = Mathf.Max(_shieldTimer, duration);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), $"🛡️ GIÁP +{amount:0}", new Color(0.45f, 0.92f, 1f), 1.0f);
        }

        public void GrantParryWindow(float duration)
        {
            _parryTimer = Mathf.Max(_parryTimer, duration);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), "⚔️ THẾ PHẢN ĐÒN!", new Color(1f, 0.92f, 0.35f), 0.9f);
        }

        public void ActivateDomain(float duration)
        {
            _domainTimer = Mathf.Max(_domainTimer, duration);
            PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.65f, 0f), "🌌 LÃNH ĐỊA (-50% HỒI CHIÊU)", new Color(0.85f, 0.55f, 1f), 1.35f);
        }

        public void GrantSpeedBuff(float speedMult, float duration)
        {
            _buffSpeedMult = Mathf.Max(_buffSpeedMult, speedMult);
            _buffTimer = Mathf.Max(_buffTimer, duration);
        }

        public void SpendHpForPower(float pct)
        {
            float cost = Mathf.Round(currentHp * Mathf.Clamp(pct, 0.05f, 0.30f));
            if (currentHp - cost >= 5f)
            {
                currentHp -= cost;
                PMDVfxPlayer.SpawnFloatingText(transform.position, $"-{cost:0} HP HIẾN TẾ", new Color(1f, 0.35f, 0.45f), 0.95f);
            }
            ResetAllCooldowns();
        }

        public void TakeDamage(float amount)
        {
            if (IsTrainerMode)
            {
                // Human Trainer has no HP bar; wild hits just prompt releasing a Pokémon from its Pokéball!
                if (_trainerWarningCooldown <= 0f)
                {
                    _trainerWarningCooldown = 1.25f;
                    PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.45f, 0f), "🔴 Tung Pokémon [R] để đỡ đòn!", new Color(1f, 0.85f, 0.35f), 0.95f);
                }
                return;
            }

            if (_invulnTimer > 0f || (PMDGameState.Instance != null && PMDGameState.Instance.IsGameOver))
                return;

            if (_parryTimer > 0f)
            {
                _parryTimer = 0f;
                PMDAudio.Play(PMDSfx.SuperEffective);
                PMDCameraFollow.Shake(0.25f, 0.22f);
                PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", transform.position, 1.8f, 26f);
                PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.55f, 0f), "🛡️ PHẢN ĐÒN HOÀN HẢO!", new Color(1f, 0.95f, 0.35f), 1.15f);
                var mons = PMDWildPokemonAI.ActiveMons;
                for (int i = mons.Count - 1; i >= 0; i--)
                {
                    var w = mons[i];
                    if (w != null && !w.IsFainted && !w.IsRecruited && Vector2.Distance(transform.position, w.transform.position) <= 3.6f)
                    {
                        w.ApplyStatusAilment(PMDStatusAilment.PsychicStun, 1.5f);
                        w.TakeDamage(Mathf.Max(35f, amount * 2.2f), ((Vector2)(w.transform.position - transform.position)).normalized * 2f, new Color(1f, 0.88f, 0.22f));
                    }
                }
                return;
            }

            amount = Mathf.Max(1f, Mathf.Round(amount * PMDGameState.PlayerDefenseMultiplier));
            if (_shieldTimer > 0f && _shieldAmount > 0f)
            {
                float absorbed = Mathf.Min(_shieldAmount, amount);
                _shieldAmount -= absorbed;
                amount -= absorbed;
                if (absorbed > 0f)
                    PMDVfxPlayer.SpawnFloatingText(transform.position + new Vector3(0f, 0.4f, 0f), $"🛡️ -{absorbed:0}", new Color(0.5f, 0.92f, 1f), 0.7f);
                if (amount <= 0.1f)
                    return;
            }
            currentHp = Mathf.Max(0f, currentHp - amount);
            PMDHitFlash.Flash(gameObject, new Color(1f, 0.35f, 0.35f), 0.14f);
            PMDCameraFollow.Shake(0.18f, 0.22f);
            PMDVfxPlayer.SpawnFloatingText(transform.position, $"-{amount:0}", new Color(1f, 0.35f, 0.35f));
            PMDAudio.Play(PMDSfx.PlayerHurt);
            Animator.PlayOneShot("Hurt", "Idle");

            if (currentHp <= 0f)
            {
                HandleDefeat();
            }
        }

        private void HandleDefeat()
        {
            var gs = PMDGameState.Instance;
            if (gs != null && gs.RemoveItem("Seed_White.None"))
            {
                currentHp = Mathf.Round(maxHp * 0.5f);
                _invulnTimer = 2.0f;
                PMDVfxPlayer.SpawnVfx("Circle_Green_Out.None", transform.position, 1.8f, 24f);
                PMDVfxPlayer.SpawnFloatingText(transform.position, "Hạt Hồi Sinh kích hoạt!", new Color(0.5f, 1f, 0.6f), 1.6f);
                PMDAudio.Play(PMDSfx.Heal);
                gs.PushToast("🌱 Hạt Hồi Sinh đã cứu bạn! (Hồi 50% HP)", new Color(0.5f, 1f, 0.6f));
                return;
            }

            if (gs == null)
            {
                currentHp = maxHp;
                return;
            }

            // Pokémon fainted -> automatically recall into its Pokéball and return control to the Human Trainer!
            RecallPokemonToBall(true);
        }

        public void ReviveAfterGameOver()
        {
            currentHp = maxHp;
            _invulnTimer = 2.5f;
            ResetAllCooldowns();
            Animator.Play("Idle", true, true);
            foreach (var mon in _recruitedParty)
                if (mon != null)
                    mon.HealFull();
        }
    }
}
