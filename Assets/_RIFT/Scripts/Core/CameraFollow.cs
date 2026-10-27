using UnityEngine;

namespace Rift.Core
{
    /// <summary>
    /// Smoothly follows a target with look-ahead, a vertical dead-zone and level bounds.
    /// Lives on the camera rig; the camera itself is a child so shake can offset it locally.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Rigidbody2D _target;
        [SerializeField] private BoxCollider2D _bounds;
        [SerializeField] private float _smoothTime = 0.18f;
        [SerializeField] private float _lookAhead = 2.5f;
        [SerializeField] private float _lookAheadSharpness = 3f;
        [SerializeField] private float _verticalDeadZone = 1.2f;
        [SerializeField] private float _verticalOffset = 1f;

        private Rect _limits;
        private Vector3 _velocity;
        private float _lead;
        private float _focusY;
        private Vector2 _attention;
        private float _attentionWeight;

        /// <summary>Lean the camera toward a point (0 = none, 1 = centred on it). Used for whisper moments.</summary>
        public void SetAttention(Vector2 point, float weight)
        {
            _attention = point;
            _attentionWeight = Mathf.Clamp01(weight);
        }

        private void Awake()
        {
            // The bounds collider is kept disabled (it's data, not physics), so compute the rect from its shape.
            Vector2 center = _bounds.transform.TransformPoint(_bounds.offset);
            Vector2 size = Vector2.Scale(_bounds.size, _bounds.transform.lossyScale);
            _limits = new Rect(center - size * 0.5f, size);
            _focusY = _target.position.y;
            transform.position = Clamp(Desired());
        }

        private void LateUpdate()
        {
            float dir = Mathf.Abs(_target.linearVelocity.x) > 0.5f ? Mathf.Sign(_target.linearVelocity.x) : 0f;
            _lead = Mathf.Lerp(_lead, dir * _lookAhead, 1f - Mathf.Exp(-_lookAheadSharpness * Time.deltaTime));

            // Only follow vertically once the player leaves the dead-zone (or lands somewhere new).
            float dy = _target.position.y - _focusY;
            if (Mathf.Abs(dy) > _verticalDeadZone) _focusY = _target.position.y - Mathf.Sign(dy) * _verticalDeadZone;

            transform.position = Vector3.SmoothDamp(transform.position, Clamp(Desired()), ref _velocity, _smoothTime);
        }

        private Vector3 Desired()
        {
            Vector2 follow = new(_target.position.x + _lead, _focusY + _verticalOffset);
            Vector2 p = Vector2.Lerp(follow, _attention, _attentionWeight);
            return new Vector3(p.x, p.y, transform.position.z);
        }

        private Vector3 Clamp(Vector3 p)
        {
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            p.x = _limits.width > halfW * 2f ? Mathf.Clamp(p.x, _limits.xMin + halfW, _limits.xMax - halfW) : _limits.center.x;
            p.y = _limits.height > halfH * 2f ? Mathf.Clamp(p.y, _limits.yMin + halfH, _limits.yMax - halfH) : _limits.center.y;
            return p;
        }
    }
}
