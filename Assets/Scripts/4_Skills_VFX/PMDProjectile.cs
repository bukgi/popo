using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Animated skill projectile supporting straight volleys, homing lock-on orbs (Aura Sphere, Swift, Electro Ball),
    /// returning boomerang blades (Razor Leaf, Water Pulse), double-helix spiral shots (Dragon Pulse, Shadow Ball),
    /// and cluster-burst bombs (Seed Bomb, Energy Ball).
    /// </summary>
    public class PMDProjectile : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private List<Sprite> _frames;
        private Vector2 _direction;
        private Vector2 _forwardAxis;
        private float _speed;
        private float _damage;
        private float _maxDistance;
        private float _traveledDistance;
        private bool _firedByPlayerTeam;
        private bool _piercing;
        private float _lifestealPercent;
        private string _hitVfxSheet;
        private string _vfxSheet;
        private Color _elementColor;
        private float _animTimer;
        private Transform _owner;
        private PMDSkillPattern _pattern;
        private float _spiralPhaseSign = 1f;
        private bool _boomerangReturning;
        private bool _isClusterChild;
        private readonly HashSet<PMDWildPokemonAI> _hitTargets = new HashSet<PMDWildPokemonAI>();

        public static void SpawnProjectileVolley(
            PMDSkillData skill,
            Vector3 startPos,
            Vector2 direction,
            bool firedByPlayerTeam,
            Transform owner)
        {
            if (skill == null)
                return;

            int count = Mathf.Max(1, skill.projectileCount);
            Vector2 baseDir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;

            if (skill.pattern == PMDSkillPattern.SpiralOrbitalShot && count >= 2)
            {
                for (int i = 0; i < count; i++)
                {
                    var p = SpawnSingleProjectile(skill, startPos, baseDir, firedByPlayerTeam, owner);
                    if (p != null)
                        p._spiralPhaseSign = (i % 2 == 0) ? 1f : -1f;
                }
                return;
            }

            if (count == 1 || skill.spreadAngle <= 0.1f)
            {
                SpawnSingleProjectile(skill, startPos, baseDir, firedByPlayerTeam, owner);
                return;
            }

            float halfSpread = skill.spreadAngle * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float angleDeg = Mathf.Lerp(-halfSpread, halfSpread, t);
                Vector2 rotatedDir = RotateVector2(baseDir, angleDeg);
                SpawnSingleProjectile(skill, startPos, rotatedDir, firedByPlayerTeam, owner);
            }
        }

        public static PMDProjectile SpawnProjectile(
            PMDSkillData skill,
            Vector3 startPos,
            Vector2 direction,
            bool firedByPlayerTeam,
            Transform owner)
        {
            SpawnProjectileVolley(skill, startPos, direction, firedByPlayerTeam, owner);
            return null;
        }

        private static PMDProjectile SpawnSingleProjectile(
            PMDSkillData skill,
            Vector3 startPos,
            Vector2 direction,
            bool firedByPlayerTeam,
            Transform owner,
            bool isClusterChild = false)
        {
            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
            List<Sprite> frames = PMDRuntimeCharacterLoader.LoadVfxFrames(skill.vfxSheet, 24f);
            if (frames == null || frames.Count == 0)
            {
                frames = PMDRuntimeCharacterLoader.LoadVfxFrames("Spark.None", 24f);
            }

            var go = new GameObject($"Proj_{skill.skillName}");
            go.transform.position = startPos + (Vector3)(dir * 0.45f) + new Vector3(0f, 0.25f, 0f);
            float scale = isClusterChild ? Mathf.Max(0.85f, skill.vfxScale * 0.78f) : Mathf.Max(1.0f, skill.vfxScale);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            if (frames != null && frames.Count > 0)
            {
                sr.sprite = frames[0];
            }
            sr.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100f) + 90;

            var proj = go.AddComponent<PMDProjectile>();
            proj._renderer = sr;
            proj._frames = frames;
            proj._direction = dir;
            proj._forwardAxis = dir;
            proj._speed = isClusterChild ? 7.5f : Mathf.Max(4f, skill.projectileSpeed);
            proj._damage = isClusterChild ? skill.damage * 0.45f : skill.damage;
            proj._maxDistance = isClusterChild ? 2.4f : Mathf.Max(3f, skill.range);
            proj._firedByPlayerTeam = firedByPlayerTeam;
            proj._piercing = skill.piercing || skill.pattern == PMDSkillPattern.BoomerangVolley;
            proj._lifestealPercent = skill.lifestealPercent;
            proj._hitVfxSheet = skill.hitVfxSheet;
            proj._vfxSheet = skill.vfxSheet;
            proj._elementColor = skill.elementColor;
            proj._owner = owner;
            proj._pattern = isClusterChild ? PMDSkillPattern.StraightVolley : skill.pattern;
            proj._isClusterChild = isClusterChild;
            return proj;
        }

        public static Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y).normalized;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float step = _speed * dt;

            // Pattern-specific trajectory behavior
            if (_pattern == PMDSkillPattern.HomingProjectile && _firedByPlayerTeam)
            {
                PMDWildPokemonAI target = FindNearestLivingWild(transform.position, 6.5f);
                if (target != null)
                {
                    Vector2 toTarget = ((Vector2)(target.transform.position - transform.position)).normalized;
                    _direction = Vector2.Lerp(_direction, toTarget, dt * 8.5f).normalized;
                }
            }
            else if (_pattern == PMDSkillPattern.BoomerangVolley)
            {
                if (!_boomerangReturning && _traveledDistance >= _maxDistance * 0.55f)
                {
                    _boomerangReturning = true;
                    _hitTargets.Clear();
                }

                if (_boomerangReturning)
                {
                    if (_owner != null)
                    {
                        Vector2 toOwner = (Vector2)_owner.position - (Vector2)transform.position;
                        if (toOwner.magnitude <= 0.55f)
                        {
                            Destroy(gameObject);
                            return;
                        }
                        _direction = toOwner.normalized;
                    }
                    else
                    {
                        _direction = -_forwardAxis;
                    }
                }
                transform.Rotate(0f, 0f, 720f * dt);
            }
            else if (_pattern == PMDSkillPattern.SpiralOrbitalShot)
            {
                Vector2 perp = new Vector2(-_forwardAxis.y, _forwardAxis.x);
                float waveVel = Mathf.Cos(_traveledDistance * 3.8f) * 4.2f * _spiralPhaseSign;
                Vector2 moveVec = (_forwardAxis * _speed + perp * waveVel) * dt;
                transform.position += (Vector3)moveVec;
                _traveledDistance += step;
                UpdateRendererAndCheckEnd();
                return;
            }

            transform.position += (Vector3)(_direction * step);
            _traveledDistance += step;
            UpdateRendererAndCheckEnd();
        }

        private void UpdateRendererAndCheckEnd()
        {
            if (_renderer != null)
            {
                _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 90;
                if (_frames != null && _frames.Count > 0)
                {
                    _animTimer += Time.deltaTime * 24f;
                    int idx = Mathf.FloorToInt(_animTimer) % _frames.Count;
                    _renderer.sprite = _frames[idx];
                }
            }

            if (CheckCollision())
            {
                if (_pattern == PMDSkillPattern.ClusterBombLob && !_isClusterChild)
                {
                    DetonateClusterSubmunitions();
                }
                Destroy(gameObject);
                return;
            }

            if (_traveledDistance >= _maxDistance)
            {
                if (!string.IsNullOrEmpty(_hitVfxSheet))
                {
                    PMDVfxPlayer.SpawnVfx(_hitVfxSheet, transform.position, 1.0f, 28f);
                }
                if (_pattern == PMDSkillPattern.ClusterBombLob && !_isClusterChild)
                {
                    DetonateClusterSubmunitions();
                }
                Destroy(gameObject);
            }
        }

        private void DetonateClusterSubmunitions()
        {
            PMDAudio.Play(PMDSfx.Hit, 0.85f);
            PMDVfxPlayer.SpawnVfx("Explosion.None", transform.position, 1.3f, 28f);

            var childSkill = new PMDSkillData
            {
                skillName = "Cluster",
                vfxSheet = !string.IsNullOrEmpty(_hitVfxSheet) ? _hitVfxSheet : _vfxSheet,
                hitVfxSheet = _hitVfxSheet,
                damage = _damage,
                vfxScale = 1.0f,
                elementColor = _elementColor,
                pattern = PMDSkillPattern.StraightVolley
            };

            const int shards = 5;
            for (int i = 0; i < shards; i++)
            {
                float angle = i * (360f / shards);
                Vector2 shardDir = RotateVector2(Vector2.right, angle);
                SpawnSingleProjectile(childSkill, transform.position, shardDir, _firedByPlayerTeam, _owner, true);
            }
        }

        private static PMDWildPokemonAI FindNearestLivingWild(Vector3 pos, float maxRadius)
        {
            var allWild = PMDWildPokemonAI.ActiveMons;
            PMDWildPokemonAI best = null;
            float bestDist = maxRadius;
            for (int i = 0; i < allWild.Count; i++)
            {
                var w = allWild[i];
                if (w == null || w.IsFainted || w.IsRecruited)
                    continue;
                float d = Vector2.Distance(pos, w.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = w;
                }
            }
            return best;
        }

        private bool CheckCollision()
        {
            const float hitRadius = 0.78f;

            if (_firedByPlayerTeam)
            {
                var allWild = PMDWildPokemonAI.ActiveMons;
                for (int i = allWild.Count - 1; i >= 0; i--)
                {
                    var wild = allWild[i];
                    if (wild == null || wild.IsFainted || wild.IsRecruited || _hitTargets.Contains(wild))
                        continue;

                    if (Vector2.Distance(transform.position, wild.transform.position) <= hitRadius)
                    {
                        _hitTargets.Add(wild);
                        Vector2 knock = _direction;
                        wild.TakeDamage(_damage, knock, _elementColor);
                        if (!string.IsNullOrEmpty(_hitVfxSheet))
                        {
                            PMDVfxPlayer.SpawnVfx(_hitVfxSheet, wild.transform.position + new Vector3(0f, 0.25f, 0f), 1.35f, 28f);
                        }

                        if (_lifestealPercent > 0f && PMDPlayerController.Instance != null)
                        {
                            PMDPlayerController.Instance.Heal(Mathf.Max(2f, _damage * _lifestealPercent));
                        }

                        if (!_piercing)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var player = PMDPlayerController.Instance;
                if (player != null && Vector2.Distance(transform.position, player.transform.position) <= hitRadius)
                {
                    player.TakeDamage(_damage);
                    if (!string.IsNullOrEmpty(_hitVfxSheet))
                    {
                        PMDVfxPlayer.SpawnVfx(_hitVfxSheet, player.transform.position + new Vector3(0f, 0.25f, 0f), 1.3f, 28f);
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
