using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// Drives the soul shader: it faces where it runs, blinks, glances around nervously when left idle,
    /// and its inner light grows as it gets closer to the rift. Purely visual.
    /// </summary>
    public class PlayerSoulVisual : MonoBehaviour
    {
        static readonly int ConfidenceId = Shader.PropertyToID("_Confidence");
        static readonly int FacingId = Shader.PropertyToID("_Facing");
        static readonly int LookId = Shader.PropertyToID("_Look");
        static readonly int BlinkId = Shader.PropertyToID("_Blink");
        static readonly int PulseId = Shader.PropertyToID("_PulseColor");

        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Renderer _renderer;
        [Tooltip("Where the level ends; confidence grows as the soul gets closer.")]
        [SerializeField] private Transform _goal;
        [SerializeField] private Vector2 _confidenceRange = new(0.15f, 1f);
        [SerializeField] private float _idleBeforeGlancing = 1.5f;
        [SerializeField] private Vector2 _glanceInterval = new(0.5f, 1.4f);
        [SerializeField] private Vector2 _blinkInterval = new(2f, 5f);
        [SerializeField] private float _blinkDuration = 0.12f;

        private MaterialPropertyBlock _block;
        private float _startDistance;
        private float _confidence;
        private float _facing = 1f;
        private Vector2 _look;
        private Vector2 _lookTarget;
        private float _idleTime;
        private float _nextGlance;
        private float _nextBlink;
        private float _blinkEnd;

        private Color _pulse;

        /// <summary>Flash the outline in a voice's colour; it fades on its own.</summary>
        public void Pulse(Color color) => _pulse = color * 2.2f;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _startDistance = Mathf.Max(1f, Vector2.Distance(_motor.transform.position, _goal.position));
            _nextBlink = Time.time + Random.Range(_blinkInterval.x, _blinkInterval.y);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 v = _motor.Velocity;

            // Confidence: how far along the level this soul has come.
            float progress = 1f - Mathf.Clamp01(Vector2.Distance(_motor.transform.position, _goal.position) / _startDistance);
            float target = Mathf.Lerp(_confidenceRange.x, _confidenceRange.y, progress);
            _confidence = Mathf.MoveTowards(_confidence, target, dt * 0.4f);

            if (Mathf.Abs(v.x) > 0.3f) _facing = Mathf.Sign(v.x);

            // Moving: eyes look ahead. Standing still for a while: nervous glances.
            bool idle = v.sqrMagnitude < 0.05f && _motor.IsGrounded;
            _idleTime = idle ? _idleTime + dt : 0f;
            if (!idle) _lookTarget = new Vector2(0.6f, Mathf.Clamp(v.y * 0.05f, -0.5f, 0.5f));
            else if (_idleTime > _idleBeforeGlancing && Time.time >= _nextGlance)
            {
                _lookTarget = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.6f, 0.4f));
                _nextGlance = Time.time + Random.Range(_glanceInterval.x, _glanceInterval.y);
            }
            _look = Vector2.Lerp(_look, _lookTarget, 1f - Mathf.Exp(-14f * dt));

            if (Time.time >= _nextBlink)
            {
                _blinkEnd = Time.time + _blinkDuration;
                _nextBlink = Time.time + Random.Range(_blinkInterval.x, _blinkInterval.y);
            }
            float blink = Time.time < _blinkEnd ? 1f : 0f;

            _block.SetFloat(ConfidenceId, _confidence);
            _block.SetFloat(FacingId, _facing);
            _block.SetVector(LookId, _look);
            _block.SetFloat(BlinkId, blink);
            _pulse = Color.Lerp(_pulse, Color.clear, 1f - Mathf.Exp(-1.6f * Time.unscaledDeltaTime));
            _block.SetColor(PulseId, _pulse);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
