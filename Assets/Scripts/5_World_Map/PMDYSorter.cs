using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PMDYSorter : MonoBehaviour
    {
        [SerializeField] public int shadowSize = 1;
        [SerializeField] private float yOffset = -0.2f;
        [SerializeField] private int sortingOrderMultiplier = 100;
        [SerializeField] private int baseSortingOrder = 0;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            float sortY = transform.position.y + yOffset;
            _renderer.sortingOrder = baseSortingOrder + Mathf.RoundToInt(-sortY * sortingOrderMultiplier);
        }
    }
}

