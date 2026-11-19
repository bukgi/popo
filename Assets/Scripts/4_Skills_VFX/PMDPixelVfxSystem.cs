using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDPixelParticleShape
    {
        StarGlint = 0,
        Ember = 1,
        ZigzagSpark = 2,
        Droplet = 3,
        IceDiamond = 4,
        PoisonBubble = 5,
        ShadowWisp = 6
    }

    /// <summary>
    /// Endesga-32 inspired 2D Pixel VFX & Game Feel system:
    /// - Point-filtered, 1px-outlined procedural pixel particles (no anti-aliasing)
    /// - Pixel-snapped movement & checkerboard dithered blink-out
    /// - Aftermath ground decals (scorch marks, frost patches, poison/shadow puddles)
    /// - Jagged pixel tether lines (for LifeDrainTether)
    /// - Safe non-stacking Hit-Stop & 1-2 frame White Hit Flash
    /// </summary>
    public static class PMDPixelVfxSystem
    {
        private const float PPU = 32f;
        private static readonly Color OutlineDark = new Color32(0x18, 0x14, 0x25, 0xFF);
        private static readonly Dictionary<PMDPixelParticleShape, Sprite> ParticleCache = new Dictionary<PMDPixelParticleShape, Sprite>();
        private static readonly Dictionary<string, Sprite> DecalCache = new Dictionary<string, Sprite>();
        private static Material _lineMat;
        private static bool _inHitStop;
        private static float _lastHitStopUnscaled = -10f;

        public static void SpawnElementalBurst(Vector3 pos, Color elementColor, string moveType, int count = 8, float speed = 3.2f)
        {
            PMDPixelParticleShape shape = ResolveShape(moveType);
            Sprite sp = GetOrBuildParticleSprite(shape);
            float gravity = shape == PMDPixelParticleShape.Ember || shape == PMDPixelParticleShape.ShadowWisp ? -1.8f
                          : (shape == PMDPixelParticleShape.Droplet || shape == PMDPixelParticleShape.IceDiamond ? 5.5f : 0.6f);

            for (int i = 0; i < count; i++)
            {
                float ang = (360f / Mathf.Max(1, count)) * i + Random.Range(-18f, 18f);
                float spd = speed * Random.Range(0.55f, 1.25f);
                Vector2 vel = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * spd;

                var go = new GameObject("PixelVfxMote");
                go.transform.position = SnapToPixel(pos + (Vector3)(Random.insideUnitCircle * 0.18f));
                float scale = Random.Range(0.9f, 1.45f);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.color = i % 3 == 0 ? Color.Lerp(elementColor, Color.white, 0.45f) : elementColor;
                sr.sortingOrder = 120;

                var mote = go.AddComponent<PixelMoteRunner>();
                mote.Init(sr, vel, gravity, Random.Range(0.24f, 0.42f));
            }
        }

        public static void SpawnAftermathDecal(Vector3 pos, Color elementColor, string moveType, float duration = 1.45f)
        {
            string kind = ResolveDecalKind(moveType);
            Sprite sp = GetOrBuildDecalSprite(kind);

            var go = new GameObject("PixelAftermathDecal");
            go.transform.position = SnapToPixel(pos);
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = kind == "Scorch" ? new Color(0.22f, 0.16f, 0.18f, 0.92f) : Color.Lerp(elementColor, OutlineDark, 0.25f);
            sr.sortingOrder = -12;

            var runner = go.AddComponent<PixelDecalRunner>();
            runner.Init(sr, duration);
        }

        public static void SpawnPixelTether(MonoBehaviour host, Transform source, Transform target, Color startCol, Color endCol, float duration = 1.2f)
        {
            if (host == null || source == null || target == null) return;

            var go = new GameObject("PixelTetherVfx");
            var lr = go.AddComponent<LineRenderer>();
            if (_lineMat == null)
                _lineMat = new Material(Shader.Find("Sprites/Default"));
            lr.material = _lineMat;
            lr.positionCount = 7;
            lr.startWidth = 0.095f;
            lr.endWidth = 0.065f;
            lr.sortingOrder = 115;
            lr.startColor = startCol;
            lr.endColor = endCol;

            var runner = go.AddComponent<PixelTetherRunner>();
            runner.Init(lr, source, target, duration);
        }

        // =========================================================================================
        // BEAUTIFY LAYER: Glow halos, impact stars, charge-in motes, afterimage trails
        // =========================================================================================
        private static Material _glowMat;
        private static bool _glowMatResolved;
        private static Sprite _glowSprite;
        private static Sprite _impactStarSprite;

        /// <summary>Additive material for halos (falls back to default sprite blending if unavailable).</summary>
        public static Material GetGlowMaterial()
        {
            if (_glowMatResolved) return _glowMat;
            _glowMatResolved = true;
            Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Mobile/Particles/Additive");
            if (sh != null)
                _glowMat = new Material(sh) { name = "PMD_PixelGlowAdditive" };
            return _glowMat;
        }

        /// <summary>Attach a pulsing, dithered pixel glow halo behind a VFX object; it follows the parent's alpha.</summary>
        public static void AttachGlowHalo(GameObject parent, Color tint, float relScale = 1.7f, float alpha = 0.42f)
        {
            if (parent == null) return;
            var parentSr = parent.GetComponent<SpriteRenderer>();

            var go = new GameObject("PMD_GlowHalo");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * relScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetGlowSprite();
            Color c = Color.Lerp(tint, Color.white, 0.18f);
            c.a = alpha;
            sr.color = c;
            var mat = GetGlowMaterial();
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = parentSr != null ? parentSr.sortingOrder - 1 : 90;

            var pulse = go.AddComponent<PixelGlowPulse>();
            pulse.Init(sr, parentSr, c, relScale);
        }

        /// <summary>4-point white-core impact star (2-3 frame hit flash at the contact point).</summary>
        public static void SpawnImpactStar(Vector3 pos, Color tint, float scale = 1.6f)
        {
            var go = new GameObject("PMD_ImpactStar");
            go.transform.position = SnapToPixel(pos + new Vector3(0f, 0.18f, 0f));
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.value < 0.5f ? 0f : 45f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetImpactStarSprite();
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 140;
            var run = go.AddComponent<ImpactStarRunner>();
            run.Init(sr, tint, scale);
            AttachGlowHalo(go, tint, 1.5f, 0.5f);
        }

        /// <summary>Anticipation: elemental motes converge into the caster.</summary>
        public static void SpawnChargeIn(Transform caster, Color tint, string moveType, int count = 10, float radius = 1.35f, float duration = 0.2f)
        {
            if (caster == null) return;
            Sprite sp = GetOrBuildParticleSprite(ResolveShape(moveType));
            for (int i = 0; i < count; i++)
            {
                float ang = (360f / count) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius * Random.Range(0.8f, 1.15f);
                var go = new GameObject("PMD_ChargeMote");
                go.transform.position = caster.position + offset;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.color = i % 2 == 0 ? Color.Lerp(tint, Color.white, 0.5f) : tint;
                sr.sortingOrder = Mathf.RoundToInt(-caster.position.y * 100f) + 130;
                var run = go.AddComponent<ChargeMoteRunner>();
                run.Init(sr, caster, offset, duration * Random.Range(0.85f, 1.1f));
            }
        }

        /// <summary>Fading tinted ghost copy of a moving VFX sprite (motion trail / smear).</summary>
        public static void SpawnAfterimage(SpriteRenderer src, float alpha = 0.55f, float life = 0.16f)
        {
            if (src == null || src.sprite == null) return;
            var go = new GameObject("PMD_VfxAfterimage");
            go.transform.position = src.transform.position;
            go.transform.rotation = src.transform.rotation;
            go.transform.localScale = src.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            Color c = src.color;
            c.a *= alpha;
            sr.color = c;
            sr.sortingOrder = src.sortingOrder - 2;
            var run = go.AddComponent<AfterimageRunner>();
            run.Init(sr, life);
        }

        /// <summary>Single slow elemental mote dropped behind projectiles.</summary>
        public static void SpawnTrailMote(Vector3 pos, Color tint, string moveType)
        {
            PMDPixelParticleShape shape = ResolveShape(moveType);
            var go = new GameObject("PMD_TrailMote");
            go.transform.position = SnapToPixel(pos + (Vector3)(Random.insideUnitCircle * 0.12f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrBuildParticleSprite(shape);
            sr.color = Random.value < 0.4f ? Color.Lerp(tint, Color.white, 0.5f) : tint;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 100;
            float gravity = shape == PMDPixelParticleShape.Ember || shape == PMDPixelParticleShape.ShadowWisp ? -1.4f : 1.2f;
            var mote = go.AddComponent<PixelMoteRunner>();
            mote.Init(sr, Random.insideUnitCircle * 0.6f, gravity, Random.Range(0.22f, 0.36f));
        }

        private static Sprite GetGlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PMD_PixelGlow"
            };
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 11.5f) / 11.5f;
                    float dy = (y - 11.5f) / 11.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a;
                    // 3 hard-stepped bands + checkerboard dither band (no smooth gradient)
                    if (d <= 0.38f) a = 1f;
                    else if (d <= 0.62f) a = 0.62f;
                    else if (d <= 0.82f) a = ((x + y) & 1) == 0 ? 0.38f : 0f;
                    else if (d <= 0.98f) a = ((x & 1) == 0 && (y & 1) == 0) ? 0.22f : 0f;
                    else a = 0f;
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU);
            return _glowSprite;
        }

        private static Sprite GetImpactStarSprite()
        {
            if (_impactStarSprite != null) return _impactStarSprite;
            const int size = 21;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PMD_ImpactStar"
            };
            Color[] px = new Color[size * size];
            int c = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int ax = Mathf.Abs(x - c);
                    int ay = Mathf.Abs(y - c);
                    // 4 tapering spikes + diamond core
                    bool spikeH = ay <= Mathf.Max(0, 2 - ax / 4) && ax <= c;
                    bool spikeV = ax <= Mathf.Max(0, 2 - ay / 4) && ay <= c;
                    bool core = ax + ay <= 3;
                    bool outline = !spikeH && !spikeV && !core &&
                                   ((ay <= Mathf.Max(0, 2 - ax / 4) + 1 && ax <= c) || (ax <= Mathf.Max(0, 2 - ay / 4) + 1 && ay <= c) || ax + ay <= 4);
                    if (core || spikeH || spikeV) px[y * size + x] = Color.white;
                    else if (outline) px[y * size + x] = new Color(0.75f, 0.75f, 0.75f, 0.85f);
                    else px[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _impactStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU);
            return _impactStarSprite;
        }

        private sealed class PixelGlowPulse : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private SpriteRenderer _parent;
            private Color _base;
            private float _relScale;
            private float _t;

            public void Init(SpriteRenderer sr, SpriteRenderer parent, Color baseCol, float relScale)
            {
                _sr = sr; _parent = parent; _base = baseCol; _relScale = relScale;
            }

            private void LateUpdate()
            {
                if (_sr == null) return;
                _t += Time.deltaTime;
                // Stepped pulse (2 sizes) for a crunchy pixel feel instead of smooth breathing
                float step = Mathf.FloorToInt(_t * 10f) % 2 == 0 ? 1f : 1.12f;
                transform.localScale = Vector3.one * (_relScale * step);
                float parentA = _parent != null ? _parent.color.a : 1f;
                Color c = _base;
                c.a = _base.a * parentA;
                _sr.color = c;
                if (_parent != null) _sr.sortingOrder = _parent.sortingOrder - 1;
            }
        }

        private sealed class ImpactStarRunner : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private Color _tint;
            private float _scale;
            private float _age;
            private const float Life = 0.2f;

            public void Init(SpriteRenderer sr, Color tint, float scale)
            {
                _sr = sr; _tint = tint; _scale = scale;
                transform.localScale = Vector3.one * (scale * 0.6f);
            }

            private void Update()
            {
                _age += Time.unscaledDeltaTime;
                if (_age >= Life) { Destroy(gameObject); return; }
                float t = _age / Life;
                // Frame steps: 0 white small -> 1 white big -> 2 tinted big -> 3 tinted thin
                int f = Mathf.Min(3, Mathf.FloorToInt(t * 4f));
                float[] scales = { 0.75f, 1.15f, 1.0f, 0.65f };
                transform.localScale = new Vector3(_scale * scales[f], _scale * scales[f] * (f == 3 ? 0.55f : 1f), 1f);
                if (_sr != null)
                    _sr.color = f < 2 ? Color.white : Color.Lerp(_tint, Color.white, f == 2 ? 0.35f : 0f);
            }
        }

        private sealed class ChargeMoteRunner : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private Transform _target;
            private Vector3 _offset;
            private float _life;
            private float _age;

            public void Init(SpriteRenderer sr, Transform target, Vector3 offset, float life)
            {
                _sr = sr; _target = target; _offset = offset; _life = Mathf.Max(0.05f, life);
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= _life || _target == null) { Destroy(gameObject); return; }
                float t = _age / _life;
                float ease = t * t; // accelerate inward
                Vector3 off = Quaternion.Euler(0f, 0f, 120f * t) * _offset * (1f - ease);
                transform.position = SnapToPixel(_target.position + off + new Vector3(0f, 0.2f, 0f));
                transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.6f, t);
            }
        }

        private sealed class AfterimageRunner : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private float _life;
            private float _age;
            private Color _base;

            public void Init(SpriteRenderer sr, float life)
            {
                _sr = sr; _life = Mathf.Max(0.04f, life); _base = sr.color;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= _life) { Destroy(gameObject); return; }
                float t = _age / _life;
                // Stepped 3-level fade (pixel-art friendly)
                float a = t < 0.34f ? 1f : (t < 0.67f ? 0.6f : 0.28f);
                Color c = _base;
                c.a = _base.a * a;
                if (_sr != null) _sr.color = c;
            }
        }

        public static void TriggerWhiteFlash(GameObject target, int frames = 2)
        {
            if (target == null) return;
            var sr = target.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;
            var mb = target.GetComponent<MonoBehaviour>();
            if (mb != null && mb.isActiveAndEnabled)
                mb.StartCoroutine(WhiteFlashRoutine(sr, frames));
        }

        private static IEnumerator WhiteFlashRoutine(SpriteRenderer sr, int frames)
        {
            if (sr == null) yield break;
            Color prev = sr.color;
            sr.color = Color.white;
            for (int i = 0; i < frames; i++)
                yield return new WaitForSecondsRealtime(1f / 60f);
            if (sr != null)
                sr.color = prev;
        }

        public static void TriggerHitStop(MonoBehaviour host, int frames = 2)
        {
            if (_inHitStop || host == null || !host.isActiveAndEnabled || frames <= 0)
                return;
            if (Time.unscaledTime - _lastHitStopUnscaled < 0.18f)
                return;

            _lastHitStopUnscaled = Time.unscaledTime;
            host.StartCoroutine(HitStopRoutine(Mathf.Clamp(frames, 1, 5)));
        }

        private static IEnumerator HitStopRoutine(int frames)
        {
            _inHitStop = true;
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(frames / 60f);
            Time.timeScale = prev > 0.01f ? prev : 1f;
            _inHitStop = false;
        }

        public static Vector3 SnapToPixel(Vector3 p)
        {
            return new Vector3(
                Mathf.Round(p.x * PPU) / PPU,
                Mathf.Round(p.y * PPU) / PPU,
                p.z);
        }

        public static PMDPixelParticleShape ResolveShape(string moveType)
        {
            if (string.IsNullOrEmpty(moveType)) return PMDPixelParticleShape.StarGlint;
            switch (moveType)
            {
                case "Fire": return PMDPixelParticleShape.Ember;
                case "Electric": return PMDPixelParticleShape.ZigzagSpark;
                case "Water": return PMDPixelParticleShape.Droplet;
                case "Ice": return PMDPixelParticleShape.IceDiamond;
                case "Poison": return PMDPixelParticleShape.PoisonBubble;
                case "Ghost":
                case "Dark": return PMDPixelParticleShape.ShadowWisp;
                default: return PMDPixelParticleShape.StarGlint;
            }
        }

        private static string ResolveDecalKind(string moveType)
        {
            if (moveType == "Fire") return "Scorch";
            if (moveType == "Ice" || moveType == "Water") return "Frost";
            if (moveType == "Poison" || moveType == "Ghost" || moveType == "Dark") return "Puddle";
            return "Rift";
        }

        private static Sprite GetOrBuildParticleSprite(PMDPixelParticleShape shape)
        {
            if (ParticleCache.TryGetValue(shape, out var cached) && cached != null)
                return cached;

            const int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            string[] mask = GetParticleMask(shape);
            for (int y = 0; y < size; y++)
            {
                string row = mask[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    char c = row[x];
                    if (c == '#') px[y * size + x] = OutlineDark;
                    else if (c == 'W') px[y * size + x] = Color.white;
                    else if (c == 'C') px[y * size + x] = new Color(0.85f, 0.85f, 0.85f, 1f);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU);
            ParticleCache[shape] = sp;
            return sp;
        }

        private static string[] GetParticleMask(PMDPixelParticleShape shape)
        {
            switch (shape)
            {
                case PMDPixelParticleShape.Ember:
                    return new[] {
                        "...#....",
                        "..#W#...",
                        ".#WCW#..",
                        ".#WWW#..",
                        ".#WCW#..",
                        "..#W#...",
                        "...#....",
                        "........"
                    };
                case PMDPixelParticleShape.ZigzagSpark:
                    return new[] {
                        "....##..",
                        "...#W#..",
                        "..#W#...",
                        ".#WWW#..",
                        "..##W#..",
                        "...#W#..",
                        "..#W#...",
                        "..##...."
                    };
                case PMDPixelParticleShape.Droplet:
                    return new[] {
                        "...#....",
                        "..#W#...",
                        "..#W#...",
                        ".#WCW#..",
                        ".#WWW#..",
                        ".#WWW#..",
                        "..###...",
                        "........"
                    };
                case PMDPixelParticleShape.IceDiamond:
                    return new[] {
                        "...##...",
                        "..#WW#..",
                        ".#WCCW#.",
                        "#WWCCWW#",
                        ".#WCCW#.",
                        "..#WW#..",
                        "...##...",
                        "........"
                    };
                case PMDPixelParticleShape.PoisonBubble:
                    return new[] {
                        "..####..",
                        ".#WWWW#.",
                        "#WWCCWW#",
                        "#WCCCCW#",
                        "#WWCCWW#",
                        ".#WWWW#.",
                        "..####..",
                        "........"
                    };
                case PMDPixelParticleShape.ShadowWisp:
                    return new[] {
                        "...###..",
                        "..#WWW#.",
                        ".#WCCCW#",
                        ".#WCCCW#",
                        "..#WWW#.",
                        "...#W#..",
                        "....#...",
                        "........"
                    };
                default:
                    return new[] {
                        "...##...",
                        "...#W#..",
                        ".##WW##.",
                        "#WWWWWW#",
                        ".##WW##.",
                        "..#W#...",
                        "...##...",
                        "........"
                    };
            }
        }

        private static Sprite GetOrBuildDecalSprite(string kind)
        {
            if (DecalCache.TryGetValue(kind, out var cached) && cached != null)
                return cached;

            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 7.5f) / 7.5f;
                    float dy = (y - 7.5f) / 7.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool checker = ((x + y) & 1) == 0;
                    if (dist <= 0.55f)
                        px[y * size + x] = Color.white;
                    else if (dist <= 0.88f && checker)
                        px[y * size + x] = new Color(1f, 1f, 1f, 0.7f);
                    else
                        px[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU);
            DecalCache[kind] = sp;
            return sp;
        }

        private sealed class PixelMoteRunner : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private Vector2 _vel;
            private float _gravity;
            private float _life;
            private float _age;

            public void Init(SpriteRenderer sr, Vector2 vel, float gravity, float life)
            {
                _sr = sr;
                _vel = vel;
                _gravity = gravity;
                _life = life;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= _life) { Destroy(gameObject); return; }

                _vel.y -= _gravity * Time.deltaTime;
                Vector3 next = transform.position + (Vector3)(_vel * Time.deltaTime);
                transform.position = SnapToPixel(next);

                float t = _age / _life;
                if (_sr != null)
                    _sr.enabled = t < 0.65f || (Mathf.FloorToInt(_age * 24f) % 2 == 0);
            }
        }

        private sealed class PixelDecalRunner : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private float _life;
            private float _age;

            public void Init(SpriteRenderer sr, float life)
            {
                _sr = sr;
                _life = life;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= _life) { Destroy(gameObject); return; }

                float t = _age / _life;
                if (_sr != null && t > 0.7f)
                    _sr.enabled = Mathf.FloorToInt(_age * 16f) % 2 == 0;
            }
        }

        private sealed class PixelTetherRunner : MonoBehaviour
        {
            private LineRenderer _lr;
            private Transform _src;
            private Transform _dst;
            private float _life;
            private float _age;

            public void Init(LineRenderer lr, Transform src, Transform dst, float life)
            {
                _lr = lr;
                _src = src;
                _dst = dst;
                _life = life;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= _life || _src == null || _dst == null)
                {
                    Destroy(gameObject);
                    return;
                }

                Vector3 a = _src.position;
                Vector3 b = _dst.position;
                for (int i = 0; i < 7; i++)
                {
                    float t = i / 6f;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    if (i > 0 && i < 6)
                    {
                        float jitter = Mathf.Sin(Time.time * 30f + i * 2.1f) * 0.14f;
                        p.y += jitter;
                    }
                    _lr.SetPosition(i, SnapToPixel(p));
                }
            }
        }
    }
}
