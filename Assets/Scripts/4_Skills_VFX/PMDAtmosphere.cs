using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Thứ tự vẽ cố định cho các lớp nền (luôn nằm dưới mọi vật thể Y-sorted có sortingOrder trong khoảng ±4000).
    /// </summary>
    public static class PMDSortLayers
    {
        public const int Ground = -30000;
        public const int WaterShimmer = -29900;
        public const int GroundDecal = -29800;
        public const int Shadow = -29500;
        public const int AmbientParticles = 9000;
    }

    /// <summary>
    /// Nâng cấp đồ họa toàn cảnh theo từng vùng đất:
    /// - Hậu kỳ URP: Bloom (phát sáng), Vignette (tối viền), chỉnh màu/độ bão hòa riêng từng vùng.
    /// - Ánh sáng 2D toàn cục theo vùng (Vùng 8 là ban đêm, đèn lồng/lửa trại tự phát sáng).
    /// - Hạt không khí: lá bay, cánh hoa anh đào, tuyết rơi, bụi cát, đom đóm, lấp lánh biển.
    /// </summary>
    public class PMDAtmosphere : MonoBehaviour
    {
        public static PMDAtmosphere Instance { get; private set; }

        private Volume _volume;
        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjust;
        private Light2D _globalLight;
        private ParticleSystem _particles;
        private ParticleSystem _particles2;
        private Material _spriteMaterial;
        private static readonly Dictionary<string, Sprite> ParticleSprites = new Dictionary<string, Sprite>();

        public static void EnsureExists()
        {
            if (Instance != null)
                return;
            var go = new GameObject("0_PMD_Atmosphere_PostFX");
            go.AddComponent<PMDAtmosphere>();
        }

        private void Awake()
        {
            Instance = this;
            SetupPostProcessing();
            FindGlobalLight();
        }

        // =========================================================================================
        // POST PROCESSING
        // =========================================================================================
        private void SetupPostProcessing()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.sharedProfile = profile;

            _bloom = profile.Add<Bloom>(true);
            _bloom.threshold.Override(0.82f);
            _bloom.intensity.Override(0.55f);
            _bloom.scatter.Override(0.62f);

            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.26f);
            _vignette.smoothness.Override(0.45f);

            _colorAdjust = profile.Add<ColorAdjustments>(true);
            _colorAdjust.saturation.Override(12f);
            _colorAdjust.contrast.Override(8f);
            _colorAdjust.postExposure.Override(0.05f);
            _colorAdjust.colorFilter.Override(Color.white);

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                    data.renderPostProcessing = true;
            }
        }

        private void FindGlobalLight()
        {
            var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    _globalLight = l;
                    break;
                }
            }

            if (_globalLight == null)
            {
                var go = new GameObject("Global Light 2D (PMD)");
                _globalLight = go.AddComponent<Light2D>();
                _globalLight.lightType = Light2D.LightType.Global;
            }
        }

        // =========================================================================================
        // PER-BIOME LOOK
        // =========================================================================================
        public void ApplyBiome(RegionBiomeStyle biome)
        {
            Color lightCol;
            float lightIntensity;
            Color filter;
            float saturation;
            float vignette;
            float bloom;

            switch (biome)
            {
                case RegionBiomeStyle.DeepWoodland:
                    lightCol = new Color(0.86f, 1f, 0.9f); lightIntensity = 0.88f; filter = new Color(0.94f, 1f, 0.96f); saturation = 14f; vignette = 0.34f; bloom = 0.7f; break;
                case RegionBiomeStyle.RedRockCanyon:
                    lightCol = new Color(1f, 0.9f, 0.76f); lightIntensity = 1.05f; filter = new Color(1f, 0.95f, 0.88f); saturation = 10f; vignette = 0.24f; bloom = 0.45f; break;
                case RegionBiomeStyle.TropicalArchipelago:
                    lightCol = new Color(1f, 0.98f, 0.92f); lightIntensity = 1.08f; filter = new Color(0.98f, 1f, 1f); saturation = 18f; vignette = 0.18f; bloom = 0.6f; break;
                case RegionBiomeStyle.SakuraSanctuary:
                    lightCol = new Color(1f, 0.93f, 0.97f); lightIntensity = 1.0f; filter = new Color(1f, 0.96f, 0.99f); saturation = 14f; vignette = 0.24f; bloom = 0.75f; break;
                case RegionBiomeStyle.GlacialHighlands:
                    lightCol = new Color(0.86f, 0.94f, 1f); lightIntensity = 1.0f; filter = new Color(0.93f, 0.97f, 1f); saturation = 4f; vignette = 0.26f; bloom = 0.65f; break;
                case RegionBiomeStyle.AutumnGuildHighlands:
                    lightCol = new Color(1f, 0.86f, 0.68f); lightIntensity = 0.92f; filter = new Color(1f, 0.93f, 0.84f); saturation = 12f; vignette = 0.3f; bloom = 0.6f; break;
                case RegionBiomeStyle.StarlightApex:
                    lightCol = new Color(0.5f, 0.56f, 0.95f); lightIntensity = 0.52f; filter = new Color(0.85f, 0.88f, 1f); saturation = 6f; vignette = 0.4f; bloom = 1.1f; break;
                case RegionBiomeStyle.IndoorRoom:
                    lightCol = new Color(1f, 0.9f, 0.74f); lightIntensity = 0.95f; filter = new Color(1f, 0.96f, 0.9f); saturation = 8f; vignette = 0.38f; bloom = 0.5f; break;
                default:
                    lightCol = new Color(1f, 0.98f, 0.92f); lightIntensity = 1.0f; filter = Color.white; saturation = 12f; vignette = 0.22f; bloom = 0.5f; break;
            }

            if (_globalLight != null)
            {
                _globalLight.color = lightCol;
                _globalLight.intensity = lightIntensity;
            }
            if (_colorAdjust != null)
            {
                _colorAdjust.colorFilter.Override(filter);
                _colorAdjust.saturation.Override(saturation);
            }
            if (_vignette != null)
                _vignette.intensity.Override(vignette);
            if (_bloom != null)
                _bloom.intensity.Override(bloom);

            SetupAmbientParticles(biome);
        }

        // =========================================================================================
        // AMBIENT PARTICLES (follow the camera)
        // =========================================================================================
        private static void SafeDestroy(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private Material GetSpriteMaterial()
        {
            if (_spriteMaterial != null)
                return _spriteMaterial;

            var tmp = new GameObject("tmp_sprite_mat");
            var sr = tmp.AddComponent<SpriteRenderer>();
            _spriteMaterial = sr.sharedMaterial;
            SafeDestroy(tmp);
            return _spriteMaterial;
        }

        private void SetupAmbientParticles(RegionBiomeStyle biome)
        {
            if (_particles != null) SafeDestroy(_particles.gameObject);
            if (_particles2 != null) SafeDestroy(_particles2.gameObject);
            _particles = null;
            _particles2 = null;

            if (!Application.isPlaying)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            switch (biome)
            {
                case RegionBiomeStyle.MeadowVillage:
                    _particles = CreateParticles(cam, "Leaf_Green", 6f, new Vector2(0.6f, -0.35f), 0.35f, 8f, new Color(0.65f, 0.95f, 0.45f), false);
                    _particles2 = CreateParticles(cam, "Glow_Soft", 3f, new Vector2(0f, 0.15f), 0.2f, 6f, new Color(1f, 1f, 0.7f, 0.7f), true);
                    break;
                case RegionBiomeStyle.DeepWoodland:
                    _particles = CreateParticles(cam, "Glow_Soft", 14f, new Vector2(0f, 0.1f), 0.35f, 6f, new Color(0.8f, 1f, 0.45f), true);
                    _particles2 = CreateParticles(cam, "Leaf_Green", 4f, new Vector2(0.3f, -0.4f), 0.3f, 9f, new Color(0.35f, 0.75f, 0.55f), false);
                    break;
                case RegionBiomeStyle.RedRockCanyon:
                    _particles = CreateParticles(cam, "Dust", 22f, new Vector2(2.2f, 0.05f), 0.5f, 5f, new Color(1f, 0.82f, 0.6f, 0.55f), false);
                    break;
                case RegionBiomeStyle.TropicalArchipelago:
                    _particles = CreateParticles(cam, "Sparkle", 12f, new Vector2(0f, 0.05f), 0.1f, 2.5f, new Color(1f, 1f, 1f, 0.9f), true);
                    _particles2 = CreateParticles(cam, "Leaf_Green", 3f, new Vector2(1f, -0.2f), 0.4f, 8f, new Color(0.5f, 0.9f, 0.4f), false);
                    break;
                case RegionBiomeStyle.SakuraSanctuary:
                    _particles = CreateParticles(cam, "Petal", 16f, new Vector2(0.7f, -0.55f), 0.45f, 9f, new Color(1f, 0.75f, 0.88f), false);
                    _particles2 = CreateParticles(cam, "Glow_Soft", 4f, new Vector2(0f, 0.2f), 0.2f, 5f, new Color(0.7f, 0.95f, 1f, 0.8f), true);
                    break;
                case RegionBiomeStyle.GlacialHighlands:
                    _particles = CreateParticles(cam, "Snow", 45f, new Vector2(0.35f, -1.1f), 0.35f, 9f, new Color(1f, 1f, 1f, 0.95f), false);
                    break;
                case RegionBiomeStyle.AutumnGuildHighlands:
                    _particles = CreateParticles(cam, "Leaf_Orange", 12f, new Vector2(0.9f, -0.5f), 0.45f, 9f, new Color(1f, 0.7f, 0.3f), false);
                    break;
                case RegionBiomeStyle.StarlightApex:
                    _particles = CreateParticles(cam, "Glow_Soft", 16f, new Vector2(0f, 0.25f), 0.25f, 7f, new Color(0.6f, 0.75f, 1f), true);
                    _particles2 = CreateParticles(cam, "Sparkle", 10f, new Vector2(0f, 0f), 0.05f, 3f, new Color(0.9f, 0.9f, 1f), true);
                    break;
                case RegionBiomeStyle.IndoorRoom:
                    _particles = CreateParticles(cam, "Dust", 4f, new Vector2(0.1f, 0.05f), 0.1f, 8f, new Color(1f, 0.95f, 0.8f, 0.35f), true);
                    break;
            }
        }

        private ParticleSystem CreateParticles(Camera cam, string spriteKey, float rate, Vector2 drift, float noiseStrength, float lifetime, Color tint, bool glowPulse)
        {
            var go = new GameObject($"Ambient_{spriteKey}");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 10f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = tint;
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            float halfH = cam.orthographic ? cam.orthographicSize : 7.5f;
            float halfW = halfH * Mathf.Max(1.2f, cam.aspect);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(halfW * 2.6f, halfH * 2.6f, 0.1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(drift.x * 0.7f, drift.x * 1.3f);
            vel.y = new ParticleSystem.MinMaxCurve(drift.y * 0.7f, drift.y * 1.3f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = ps.noise;
            noise.enabled = noiseStrength > 0f;
            noise.strength = noiseStrength;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.25f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = !glowPulse;
            rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            if (glowPulse)
            {
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.25f, 0.4f),
                        new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0.3f, 0.8f), new GradientAlphaKey(0f, 1f)
                    });
            }
            else
            {
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            }
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            tsa.AddSprite(GetParticleSprite(spriteKey));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetSpriteMaterial();
            renderer.sortingOrder = PMDSortLayers.AmbientParticles;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            return ps;
        }

        private static Sprite GetParticleSprite(string key)
        {
            if (ParticleSprites.TryGetValue(key, out var cached) && cached != null)
                return cached;

            int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Particle_" + key
            };
            var px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 3.5f, dy = y - 3.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Color.clear;
                    switch (key)
                    {
                        case "Leaf_Green":
                        case "Leaf_Orange":
                            // diagonal leaf shape with a darker vein
                            if (Mathf.Abs(dx - dy) < 1.6f && d < 3.6f)
                                c = (Mathf.Abs(dx - dy) < 0.5f) ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;
                            break;
                        case "Petal":
                            if ((dx * dx) / 9f + (dy * dy) / 4f < 1f)
                                c = dy > 0.5f ? Color.white : new Color(0.92f, 0.9f, 0.92f, 1f);
                            break;
                        case "Snow":
                            if (d < 2.2f) c = new Color(1f, 1f, 1f, d < 1.2f ? 1f : 0.6f);
                            break;
                        case "Dust":
                            if (d < 1.6f) c = new Color(1f, 1f, 1f, 0.8f);
                            break;
                        case "Sparkle":
                            if ((Mathf.Abs(dx) < 0.6f && Mathf.Abs(dy) < 3.2f) || (Mathf.Abs(dy) < 0.6f && Mathf.Abs(dx) < 3.2f))
                                c = new Color(1f, 1f, 1f, 1f - d / 4f);
                            break;
                        default: // Glow_Soft
                            if (d < 3.8f) c = new Color(1f, 1f, 1f, Mathf.Pow(1f - d / 3.8f, 1.6f));
                            break;
                    }
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
            ParticleSprites[key] = sprite;
            return sprite;
        }

        // =========================================================================================
        // POINT LIGHTS (lanterns, campfires, portals)
        // =========================================================================================
        public static Light2D AttachPointLight(GameObject host, Color color, float intensity, float radius, bool flicker)
        {
            if (!Application.isPlaying || host == null)
                return null;

            var go = new GameObject("PointLight2D");
            go.transform.SetParent(host.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
            light.falloffIntensity = 0.7f;

            if (flicker)
            {
                var f = go.AddComponent<PMDLightFlicker>();
                f.baseIntensity = intensity;
            }
            return light;
        }
    }
}

