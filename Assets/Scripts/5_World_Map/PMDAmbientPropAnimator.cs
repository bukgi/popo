using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Animates multi-frame environmental props (swaying flowers, fluttering flags, portals, water waves, crystal spring).
    /// </summary>
    public class PMDAmbientPropAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 6f;
        private SpriteRenderer _sr;
        private float _offset;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _offset = Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1)
                return;

            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
                return;

            float t = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
            int idx = Mathf.FloorToInt((t + _offset) * fps) % frames.Length;
            if (idx < 0)
                idx += frames.Length;
            _sr.sprite = frames[idx];
        }
    }
}

