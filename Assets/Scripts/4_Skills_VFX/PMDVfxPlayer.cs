using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public struct PMDFloatingTextEntry
    {
        public Vector3 worldPosition;
        public string text;
        public Color color;
        public float expireTime;
        public float startTime;
    }

    /// <summary>
    /// Plays genuine PMDCollab Particle/VFX sprite strips in world space and manages floating combat text.
    /// </summary>
    public class PMDVfxPlayer : MonoBehaviour
    {
        public static readonly List<PMDFloatingTextEntry> ActiveFloatingTexts = new List<PMDFloatingTextEntry>();

        private SpriteRenderer _renderer;
        private List<Sprite> _frames;
        private float _fps = 24f;
        private float _timer;
        private int _frameIndex;
        private bool _loop;
        private float _maxLifetime = 1.5f;
        private float _elapsed;

        public static void SpawnFloatingText(Vector3 worldPos, string text, Color color, float duration = 0.95f)
        {
            if (ActiveFloatingTexts.Count > 64)
            {
                ActiveFloatingTexts.RemoveAt(0);
            }

            ActiveFloatingTexts.Add(new PMDFloatingTextEntry
            {
                worldPosition = worldPos + new Vector3(Random.Range(-0.18f, 0.18f), 0.65f, 0f),
                text = text,
                color = color,
                startTime = Time.time,
                expireTime = Time.time + duration
            });
        }

        public static PMDVfxPlayer SpawnVfx(string vfxFileName, Vector3 worldPos, float scale = 1.25f, float fps = 26f, Color? tint = null, Transform followTarget = null)
        {
            if (string.IsNullOrEmpty(vfxFileName))
                return null;

            List<Sprite> frames = PMDRuntimeCharacterLoader.LoadVfxFrames(vfxFileName, 24f);
            if (frames == null || frames.Count == 0)
                return null;

            var go = new GameObject($"VFX_{vfxFileName}");
            if (followTarget != null)
            {
                go.transform.SetParent(followTarget, false);
                go.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            }
            else
            {
                go.transform.position = worldPos;
            }

            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * 100f) + 80;
            if (tint.HasValue)
            {
                sr.color = tint.Value;
            }

            var player = go.AddComponent<PMDVfxPlayer>();
            player._renderer = sr;
            player._frames = frames;
            player._fps = Mathf.Max(8f, fps);
            player._loop = false;
            player._maxLifetime = (frames.Count / player._fps) + 0.1f;
            return player;
        }

        private void Update()
        {
            if (_frames == null || _frames.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            if (!_loop && _elapsed >= _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            _timer += Time.deltaTime * _fps;
            int nextIndex = Mathf.FloorToInt(_timer);

            if (nextIndex >= _frames.Count)
            {
                if (_loop)
                {
                    _timer = 0f;
                    nextIndex = 0;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (nextIndex != _frameIndex && nextIndex >= 0 && nextIndex < _frames.Count)
            {
                _frameIndex = nextIndex;
                if (_renderer != null)
                {
                    _renderer.sprite = _frames[_frameIndex];
                    _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 80;
                }
            }
        }
    }
}

