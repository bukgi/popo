using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Handles pixel-art Poké Ball throwing animations, capture rate calculation,
    /// shake suspense, and capture success/failure resolution on dazed wild Pokémon.
    /// </summary>
    public class PMDCaptureBall : MonoBehaviour
    {
        private static Sprite _pokeBallSprite;
        private static Sprite _apricornBallSprite;
        private static Sprite _starSprite;

        private SpriteRenderer _sr;

        /// <summary>
        /// Calculates the capture probability (0.15 to 0.95) for a dazed wild Pokémon
        /// based on species rarity tier, level difference, temperament, and ball bonus.
        /// </summary>
        public static float CalculateCatchChance(PMDWildPokemonAI target, bool isApricornBoost)
        {
            if (target == null || target.IsBoss)
                return 0f;

            float baseRate = GetSpeciesBaseCatchRate(target.PokemonName);
            int levelDiff = PMDGameState.CurrentLevel - target.Level;
            float levelMod = levelDiff >= 0
                ? Mathf.Min(0.14f, levelDiff * 0.025f)
                : Mathf.Max(-0.18f, levelDiff * 0.04f);

            float temperMod = target.IsAggressive ? -0.04f : 0.04f;
            float ballBonus = isApricornBoost ? 0.22f : 0f;

            return Mathf.Clamp(baseRate + levelMod + temperMod + ballBonus, 0.15f, 0.95f);
        }

        private static float GetSpeciesBaseCatchRate(string speciesName)
        {
            if (string.IsNullOrEmpty(speciesName))
                return 0.55f;

            switch (speciesName)
            {
                case "Mewtwo":
                case "Rayquaza":
                case "Mew":
                    return 0.32f;
                case "Charizard":
                case "Dragonite":
                case "Tyranitar":
                case "Garchomp":
                case "Gyarados":
                    return 0.38f;
                case "Arcanine":
                case "Gengar":
                case "Lapras":
                case "Snorlax":
                case "Gardevoir":
                case "Lucario":
                case "Glaceon":
                case "Greninja":
                    return 0.50f;
                case "Pikachu":
                case "Bulbasaur":
                case "Charmander":
                case "Squirtle":
                case "Eevee":
                case "Treecko":
                case "Torchic":
                case "Mudkip":
                    return 0.68f;
                default:
                    return 0.52f;
            }
        }

        /// <summary>
        /// Spawns a pixel-art Poké Ball or Apricorn Ball thrown toward a dazed wild Pokémon.
        /// </summary>
        public static bool SpawnCaptureThrow(PMDPlayerController thrower, PMDWildPokemonAI target, bool isApricornBoost)
        {
            if (thrower == null || target == null || !target.TryBeginCapture())
                return false;

            EnsureSprites();

            var go = new GameObject(isApricornBoost ? "PMD_ApricornBall" : "PMD_PokeBall");
            Vector3 startPos = thrower.transform.position + new Vector3(0f, 0.35f, 0f);
            go.transform.position = startPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isApricornBoost ? _apricornBallSprite : _pokeBallSprite;
            sr.sortingOrder = Mathf.RoundToInt(-startPos.y * 100f) + 90;

            var ball = go.AddComponent<PMDCaptureBall>();
            ball._sr = sr;
            ball.StartCoroutine(ball.RunCaptureRoutine(thrower, target, startPos, isApricornBoost));
            return true;
        }

        /// <summary>
        /// Attaches swirling pixel-art gold dizzy stars above a dazed Pokémon.
        /// </summary>
        public static PMDDizzyStars AttachDizzyStars(PMDWildPokemonAI target)
        {
            if (target == null)
                return null;

            EnsureSprites();

            var existing = target.GetComponentInChildren<PMDDizzyStars>();
            if (existing != null)
                return existing;

            var go = new GameObject("DizzyStars");
            go.transform.SetParent(target.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.52f, 0f);

            var dizzy = go.AddComponent<PMDDizzyStars>();
            dizzy.Initialize(target, _starSprite);
            return dizzy;
        }

        private IEnumerator RunCaptureRoutine(PMDPlayerController thrower, PMDWildPokemonAI target, Vector3 startPos, bool isApricornBoost)
        {
            PMDAudio.Play(PMDSfx.BallThrow);

            Vector3 targetPos = target != null ? target.transform.position + new Vector3(0f, 0.12f, 0f) : startPos + Vector3.right;
            float throwDuration = 0.34f;
            float elapsed = 0f;

            // Phase 1: Parabolic throw flight
            while (elapsed < throwDuration)
            {
                if (target == null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                targetPos = target.transform.position + new Vector3(0f, 0.12f, 0f);
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / throwDuration);

                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 1.15f;
                transform.position = pos;
                transform.localRotation = Quaternion.Euler(0f, 0f, -t * 540f);

                float pulse = 0.85f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                transform.localScale = Vector3.one * pulse;
                _sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 95;
                yield return null;
            }

            if (target == null)
            {
                Destroy(gameObject);
                yield break;
            }

            // Phase 2: Hit & absorb monster into the ball
            transform.position = targetPos;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            string absorbVfx = isApricornBoost ? "Circle_Blue_Out.None" : "Circle_Yellow_Out.None";
            PMDVfxPlayer.SpawnVfx(absorbVfx, targetPos, 1.1f, 26f);
            PMDAudio.Play(PMDSfx.Hit, 0.65f);
            target.SetCaptureVisualHidden(true);

            // Short settle hop down onto ground
            float settleDuration = 0.24f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * 0.35f * (1f - t);
                transform.position = targetPos + new Vector3(0f, bounce, 0f);
                yield return null;
            }

            transform.position = targetPos;

            float catchChance = CalculateCatchChance(target, isApricornBoost);
            bool caught = Random.value <= catchChance;
            int shakeCount = caught ? 3 : Random.Range(1, 4);

            // Phase 3: Suspenseful wobbles (1 to 3 shakes)
            for (int s = 0; s < shakeCount; s++)
            {
                if (target == null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                PMDAudio.Play(PMDSfx.BallShake, 0.85f);
                PMDVfxPlayer.SpawnFloatingText(targetPos + new Vector3(0f, 0.1f, 0f), new string('.', s + 1), new Color(1f, 0.92f, 0.45f), 0.45f);

                float shakeTime = 0.46f;
                float st = 0f;
                while (st < shakeTime)
                {
                    st += Time.deltaTime;
                    float p = Mathf.Clamp01(st / shakeTime);
                    float angle = Mathf.Sin(p * Mathf.PI * 2f) * 22f * (1f - p * 0.35f);
                    float hop = Mathf.Abs(Mathf.Sin(p * Mathf.PI)) * 0.08f;
                    transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    transform.position = targetPos + new Vector3(0f, hop, 0f);
                    yield return null;
                }

                transform.localRotation = Quaternion.identity;
                transform.position = targetPos;
                yield return new WaitForSeconds(0.18f);
            }

            if (target == null)
            {
                Destroy(gameObject);
                yield break;
            }

            // Phase 4: Resolution
            target.SetCaptureVisualHidden(false);
            if (caught)
            {
                PMDVfxPlayer.SpawnVfx("Circle_Green_Out.None", targetPos, 1.35f, 26f);
                PMDVfxPlayer.SpawnVfx("Synthesis_Sparkle.None", targetPos + new Vector3(0f, 0.25f, 0f), 1.15f, 24f);
                target.EndCaptureAttempt(true, catchChance);

                if (thrower != null)
                {
                    thrower.CompleteCapturedPokemon(target, catchChance);
                }
            }
            else
            {
                PMDVfxPlayer.SpawnVfx("Circle_Red_Out.None", targetPos, 1.15f, 26f);
                PMDAudio.Play(PMDSfx.BallFail);
                target.EndCaptureAttempt(false, catchChance);
            }

            Destroy(gameObject);
        }

        // =========================================================================================
        // PROCEDURAL PIXEL-ART SPRITES (FilterMode.Point)
        // =========================================================================================
        private static void EnsureSprites()
        {
            if (_pokeBallSprite == null)
                _pokeBallSprite = CreateBallSprite(new Color(0.92f, 0.20f, 0.22f), new Color(0.70f, 0.10f, 0.14f), Color.white);

            if (_apricornBallSprite == null)
                _apricornBallSprite = CreateBallSprite(new Color(0.20f, 0.56f, 0.96f), new Color(0.12f, 0.32f, 0.72f), new Color(1f, 0.88f, 0.32f));

            if (_starSprite == null)
                _starSprite = CreateStarSprite();
        }

        private static Sprite CreateBallSprite(Color topLight, Color topShade, Color buttonGlow)
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            Color outline = new Color(0.08f, 0.08f, 0.12f, 1f);
            Color whiteMain = new Color(0.96f, 0.97f, 0.99f, 1f);
            Color whiteShade = new Color(0.76f, 0.80f, 0.86f, 1f);
            Color highlight = new Color(1f, 0.92f, 0.92f, 1f);

            float cx = 7.5f;
            float cy = 7.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 7.2f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    if (dist >= 6.0f)
                    {
                        pixels[y * size + x] = outline;
                        continue;
                    }

                    // Center button ring (radius ~2.4)
                    if (dist <= 2.35f)
                    {
                        pixels[y * size + x] = dist <= 1.25f ? buttonGlow : outline;
                        continue;
                    }

                    // Black belt along the middle
                    if (y == 7 || y == 8)
                    {
                        pixels[y * size + x] = outline;
                        continue;
                    }

                    if (y > 8)
                    {
                        // Top colored shell + pixel specular dot
                        bool isSpec = (x >= 4 && x <= 5 && y >= 11 && y <= 12);
                        bool isShaded = dx > 1.8f;
                        pixels[y * size + x] = isSpec ? highlight : (isShaded ? topShade : topLight);
                    }
                    else
                    {
                        // Bottom white shell
                        bool isShaded = dx > 1.5f || y <= 2;
                        pixels[y * size + x] = isShaded ? whiteShade : whiteMain;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite CreateStarSprite()
        {
            const int size = 7;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            string[] map =
            {
                "...O...",
                "...Y...",
                ".OYYYO.",
                "OYYWYYO",
                ".OYYYO.",
                "..OYO..",
                ".O...O."
            };

            Color[] pixels = new Color[size * size];
            Color outline = new Color(0.24f, 0.14f, 0.04f, 1f);
            Color gold = new Color(1f, 0.85f, 0.22f, 1f);
            Color white = new Color(1f, 0.98f, 0.85f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    char c = map[size - 1 - y][x];
                    pixels[y * size + x] = c == 'O' ? outline : (c == 'Y' ? gold : (c == 'W' ? white : Color.clear));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        }
    }

    /// <summary>
    /// Orbiting 8-bit gold stars shown above a Pokémon in the dazed ("ngất ngư") state.
    /// </summary>
    public class PMDDizzyStars : MonoBehaviour
    {
        private PMDWildPokemonAI _owner;
        private readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>(3);
        private float _angleOffset;

        public void Initialize(PMDWildPokemonAI owner, Sprite starSprite)
        {
            _owner = owner;
            for (int i = 0; i < 3; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(transform, false);
                var sr = starGo.AddComponent<SpriteRenderer>();
                sr.sprite = starSprite;
                sr.sortingOrder = 100;
                _stars.Add(sr);
            }
        }

        private void Update()
        {
            if (_owner == null || !_owner.IsDazed)
            {
                Destroy(gameObject);
                return;
            }

            bool hidden = _owner.IsBeingCaptured;
            _angleOffset += Time.deltaTime * 4.2f;

            for (int i = 0; i < _stars.Count; i++)
            {
                var sr = _stars[i];
                if (sr == null)
                    continue;

                sr.enabled = !hidden;
                if (hidden)
                    continue;

                float angle = _angleOffset + (i * Mathf.PI * 2f / _stars.Count);
                float x = Mathf.Cos(angle) * 0.34f;
                float y = Mathf.Sin(angle) * 0.11f;
                sr.transform.localPosition = new Vector3(x, y, 0f);
                sr.sortingOrder = Mathf.RoundToInt(-_owner.transform.position.y * 100f) + (Mathf.Sin(angle) < 0f ? 60 : 20);
            }
        }
    }
}

