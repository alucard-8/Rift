using Rift.Player;
using UnityEngine;

namespace Rift.Gadgets
{
    /// <summary>
    /// Glides back and forth between two points and carries the player. It belongs to one world through
    /// RiftPresence, so a Static mover only holds you while the lens is on it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 _offset = new(8f, 0f);
        [SerializeField] private float _speed = 2.5f;
        [SerializeField] private int _pauseSteps = 25;
        [SerializeField] private RiftPresence _presence;
        [SerializeField] private Vector2 _riderProbe = new(1.6f, 0.2f);

        private Rigidbody2D _body;
        private Vector2 _a, _b;
        private bool _toB = true;
        private int _pause;
        private readonly Collider2D[] _hits = new Collider2D[32];   // room for the platform's own cells

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _a = _body.position;
            _b = _a + _offset;
        }

        private void FixedUpdate()
        {
            if (_pause > 0) { _pause--; return; }

            Vector2 target = _toB ? _b : _a;
            Vector2 next = Vector2.MoveTowards(_body.position, target, _speed * Time.fixedDeltaTime);
            Vector2 delta = next - _body.position;
            CarryRider(delta);
            _body.MovePosition(next);

            if (next == target) { _toB = !_toB; _pause = _pauseSteps; }
        }

        private void CarryRider(Vector2 delta)
        {
            if (_presence != null && !_presence.IsReal) return;
            Vector2 top = _body.position + Vector2.up * 0.35f;
            int n = Physics2D.OverlapBox(top, _riderProbe, 0f, ContactFilter2D.noFilter, _hits);
            for (int i = 0; i < n; i++)
                if (_hits[i].TryGetComponent(out PlayerMotor rider) && rider.IsGrounded)
                    rider.GetComponent<Rigidbody2D>().position += delta;
        }
    }
}
