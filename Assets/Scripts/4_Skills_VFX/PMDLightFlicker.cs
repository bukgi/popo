using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace POKE.PMDCollab.OpenWorld
{
    public class PMDLightFlicker : MonoBehaviour
    {
        public float baseIntensity = 1f;
        public float amount = 0.18f;
        public float speed = 7f;
        private Light2D _light;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (_light == null)
                return;
            float n = Mathf.PerlinNoise(_seed, Time.time * speed) - 0.5f;
            _light.intensity = baseIntensity * (1f + n * amount * 2f);
        }
    }
}

