using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Nháy đỏ khi trúng đòn (hit flash) cho mọi SpriteRenderer.
    /// </summary>
    public class PMDHitFlash : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _timer;
        private Color _flashColor = new Color(1f, 0.4f, 0.4f, 1f);

        public static void Flash(GameObject target, Color? color = null, float duration = 0.12f)
        {
            if (target == null)
                return;
            var f = target.GetComponent<PMDHitFlash>();
            if (f == null)
                f = target.AddComponent<PMDHitFlash>();
            f._sr = target.GetComponent<SpriteRenderer>();
            f._timer = duration;
            if (color.HasValue)
                f._flashColor = color.Value;
        }

        private void LateUpdate()
        {
            if (_sr == null)
                return;

            if (_timer > 0f)
            {
                _timer -= Time.deltaTime;
                bool on = Mathf.FloorToInt(_timer * 40f) % 2 == 0;
                _sr.color = on ? _flashColor : Color.white;
                if (_timer <= 0f)
                    _sr.color = Color.white;
            }
        }
    }
}

