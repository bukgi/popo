using UnityEngine;
using UnityEngine.InputSystem;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Camera theo nhân vật: mượt, khóa trong biên bản đồ (không lộ khoảng trống ngoài map),
    /// bám lưới pixel (pixel-snap 24 PPU) để pixel-art không bị rung/nhòe, và rung màn hình khi trúng đòn mạnh.
    /// </summary>
    public class PMDCameraFollow : MonoBehaviour
    {
        private const float PixelsPerUnit = 24f;

        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float minOrthoSize = 4f;
        [SerializeField] private float maxOrthoSize = 14f;
        [SerializeField] private float zoomStep = 1.0f;

        private Camera _cam;
        private float _desiredOrthoSize = 7.5f;
        private Vector3 _smoothPos;
        private bool _hasSmoothPos;

        private static float _shakeTimer;
        private static float _shakeDuration;
        private static float _shakeAmount;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                _hasSmoothPos = false;
            }
        }

        public static void Shake(float amount, float duration)
        {
            if (amount < _shakeAmount && _shakeTimer > 0f)
                return;
            _shakeAmount = amount;
            _shakeDuration = duration;
            _shakeTimer = duration;
        }

        public void SnapToTarget()
        {
            if (target != null)
            {
                _smoothPos = new Vector3(target.position.x, target.position.y, transform.position.z);
                _hasSmoothPos = true;
                transform.position = ClampToRoomBounds(_smoothPos);
            }
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null && _cam.orthographic)
            {
                _desiredOrthoSize = _cam.orthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (Mouse.current != null && _cam != null && _cam.orthographic && !PMDGameState.IsInputBlocked)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _desiredOrthoSize = Mathf.Clamp(_desiredOrthoSize - Mathf.Sign(scroll) * zoomStep, minOrthoSize, maxOrthoSize);
                }
            }
            if (_cam != null && _cam.orthographic)
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _desiredOrthoSize, Time.unscaledDeltaTime * 10f);

            if (target == null)
                return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

            // Teleports (room transitions) snap instantly instead of sliding across the map
            if (!_hasSmoothPos || Vector2.Distance(_smoothPos, desired) > 12f)
            {
                _smoothPos = desired;
                _hasSmoothPos = true;
            }
            else
            {
                _smoothPos = Vector3.Lerp(_smoothPos, desired, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
            }

            Vector3 pos = ClampToRoomBounds(_smoothPos);

            // Screen shake
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.01f, _shakeDuration));
                pos += (Vector3)(Random.insideUnitCircle * _shakeAmount * k);
            }

            // Pixel snap so 24-PPU pixel art stays crisp while moving
            pos.x = Mathf.Round(pos.x * PixelsPerUnit) / PixelsPerUnit;
            pos.y = Mathf.Round(pos.y * PixelsPerUnit) / PixelsPerUnit;
            pos.z = transform.position.z;
            transform.position = pos;
        }

        private Vector3 ClampToRoomBounds(Vector3 p)
        {
            if (_cam == null || !_cam.orthographic)
                return p;

            var mapGen = PMDOpenWorldMapGenerator.Instance;
            if (mapGen == null || mapGen.ActiveZone == null)
                return p;

            Vector2 half = mapGen.ActiveZone.isIndoor
                ? new Vector2(8f, 6.5f)
                : new Vector2(PMDOpenWorldMapGenerator.RoomHalfWidth, PMDOpenWorldMapGenerator.RoomHalfHeight);

            float viewH = _cam.orthographicSize;
            float viewW = viewH * _cam.aspect;

            p.x = viewW >= half.x ? 0f : Mathf.Clamp(p.x, -half.x + viewW, half.x - viewW);
            p.y = viewH >= half.y ? 0f : Mathf.Clamp(p.y, -half.y + viewH, half.y - viewH);
            return p;
        }
    }
}
