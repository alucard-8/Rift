using System.Collections;
using Rift.Player;
using UnityEngine;

namespace Rift.Extras
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingLaser : MonoBehaviour
    {
        static readonly int OnId = Shader.PropertyToID("_On");
        static readonly int ChargeId = Shader.PropertyToID("_Charge");
        static readonly int AlongId = Shader.PropertyToID("_Along");

        [SerializeField] private SpriteRenderer _beam;
        [SerializeField] private float _width = 6f;
        [SerializeField] private float _low = 0.3f;
        [SerializeField] private float _high = 2.1f;
        [SerializeField] private float _speed = 1.3f;
        [SerializeField] private float _thickness = 0.16f;
        [SerializeField] private float _onTime = 2.8f;
        [SerializeField] private float _offTime = 0.9f;
        [SerializeField] private float _warnTime = 0.55f;

        private BoxCollider2D _collider;
        private MaterialPropertyBlock _block;
        private float _lastHit = float.NegativeInfinity;
        private float _offset;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _collider.size = new Vector2(_width, _thickness);
            _block = new MaterialPropertyBlock();
            _offset = Random.value;
        }

        private void Update()
        {
            float period = _onTime + _offTime;
            float phase = Mathf.Repeat(Time.time + _offset * period, period);
            bool on = phase < _onTime;

            // Sweep while it is alive; the beam keeps travelling when it blinks off.
            float k = 0.5f + 0.5f * Mathf.Sin((Time.time + _offset * 4f) * _speed);
            float y = Mathf.Lerp(_low, _high, k);

            if (_beam != null) _beam.transform.localPosition = new Vector3(0f, y, 0f);
            _collider.offset = new Vector2(0f, y);
            _collider.enabled = on;

            float charge = !on && phase > period - _warnTime
                ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 22f))
                : 0f;

            _block.SetFloat(OnId, on ? 1f : 0.1f);
            _block.SetFloat(ChargeId, charge);
            _block.SetFloat(AlongId, _offset);
            if (_beam != null) _beam.SetPropertyBlock(_block);
        }

        private void OnTriggerEnter2D(Collider2D other) => Hit(other);
        private void OnTriggerStay2D(Collider2D other) => Hit(other);

        private void Hit(Collider2D other)
        {
            if (Time.time - _lastHit < 0.4f) return;
            PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null) return;

            _lastHit = Time.time;
            respawn.Respawn(DeathCause.Laser);
        }
    }
}
