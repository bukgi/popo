using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public class PMDWorldItemPickup : MonoBehaviour
    {
        public string itemName;
        public float healAmount = 35f;
        public bool IsCollected { get; private set; }

        private Vector3 _basePos;
        private float _phase;

        private void Start()
        {
            _basePos = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (!Application.isPlaying || IsCollected)
                return;

            float bob = Mathf.Sin(Time.time * 3.2f + _phase) * 0.08f;
            transform.position = _basePos + new Vector3(0f, bob, 0f);
        }

        public void Collect(PMDPlayerController player)
        {
            if (IsCollected)
                return;

            IsCollected = true;
            player.Heal(healAmount);
            gameObject.SetActive(false);
        }
    }
}

