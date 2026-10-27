using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rift
{
    /// <summary>
    /// The single source of truth for where the rift is. Follows the pointer and
    /// publishes its position to shaders as global properties.
    /// </summary>
    public class RiftLens : MonoBehaviour
    {
        static readonly int CenterId = Shader.PropertyToID("_RiftCenter");
        static readonly int RadiusId = Shader.PropertyToID("_RiftRadius");
        static readonly int EdgeId = Shader.PropertyToID("_RiftEdge");

        [SerializeField] private LensConfig _config;
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _hideCursor = true;

        private Vector2 _target;
        private bool _hasOverride;

        public event Action<Vector2> Moved;

        public Vector2 Center { get; private set; }
        public float Radius => _config.radius;
        public LensConfig Config => _config;

        public bool Contains(Vector2 worldPoint) => (worldPoint - Center).sqrMagnitude <= Radius * Radius;

        /// <summary>Is a point physically real in the given world right now?</summary>
        public bool IsReal(RiftWorld world, Vector2 worldPoint) =>
            world == RiftWorld.Static ? Contains(worldPoint) : !Contains(worldPoint);

        /// <summary>Pin the lens to a world position (tests, cutscenes). Pass null to follow the pointer again.</summary>
        public void SetOverride(Vector2? worldPoint)
        {
            _hasOverride = worldPoint.HasValue;
            if (worldPoint.HasValue) _target = Center = worldPoint.Value;
        }

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            Center = _target = transform.position;
            Publish();
        }

        private void OnEnable()
        {
            _pointAction.action.Enable();
            if (_hideCursor) Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (_hideCursor) Cursor.visible = true;
        }

        private void Update()
        {
            if (!_hasOverride)
            {
                Vector2 screen = _pointAction.action.ReadValue<Vector2>();
                _target = _camera.ScreenToWorldPoint(screen);
            }

            Vector2 previous = Center;
            float t = 1f - Mathf.Exp(-_config.followSharpness * Time.unscaledDeltaTime);
            Center = Vector2.Lerp(Center, _target, t);
            transform.position = new Vector3(Center.x, Center.y, transform.position.z);

            Publish();
            if ((Center - previous).sqrMagnitude > 0f) Moved?.Invoke(Center);
        }

        private void Publish()
        {
            Shader.SetGlobalVector(CenterId, Center);
            Shader.SetGlobalFloat(RadiusId, _config.radius);
            Shader.SetGlobalFloat(EdgeId, _config.edgeWidth);
        }
    }
}
