using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Gadgets
{
    /// <summary>
    /// A platform that cracks when stood on, gives way a moment later, then grows back.
    /// Works on top of RiftSolid: while fallen, the solid is switched off entirely.
    /// </summary>
    [RequireComponent(typeof(RiftSolid))]
    public class CrumblingFloor : MonoBehaviour
    {
        public enum State { Solid, Cracking, Fallen }

        [SerializeField] private int _crackSteps = 30;     // 0.6 s at 50 Hz
        [SerializeField] private int _fallenSteps = 150;   // 3 s
        [SerializeField] private Vector2 _topProbe = new(0.9f, 0.15f);

        private RiftSolid _solid;
        private SpriteRenderer _sprite;
        private Collider2D[] _cells;
        private readonly Collider2D[] _hits = new Collider2D[32];   // room for the platform's own cells
        private int _timer;
        private Vector3 _home;

        public event Action Cracked;
        public event Action Fell;
        public event Action Restored;

        public State Current { get; private set; }
        /// <summary>0 when solid, rising to 1 just before it falls (for shake/crack visuals).</summary>
        public float CrackAmount => Current == State.Cracking ? 1f - (float)_timer / _crackSteps : 0f;

        public void Crumble()
        {
            if (Current != State.Solid) return;
            Current = State.Cracking;
            _timer = _crackSteps;
            Cracked?.Invoke();
        }

        private void Awake()
        {
            _solid = GetComponent<RiftSolid>();
            _sprite = GetComponent<SpriteRenderer>();
            _home = transform.position;
        }

        private void Start() => _cells = GetComponentsInChildren<Collider2D>();

        private void FixedUpdate()
        {
            switch (Current)
            {
                case State.Solid:
                    if (PlayerOnTop()) Crumble();
                    break;
                case State.Cracking:
                    transform.position = _home + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.05f * CrackAmount);
                    if (--_timer <= 0) SetFallen(true);
                    break;
                case State.Fallen:
                    if (--_timer <= 0) SetFallen(false);
                    break;
            }
        }

        private void SetFallen(bool fallen)
        {
            Current = fallen ? State.Fallen : State.Solid;
            _timer = _fallenSteps;
            transform.position = _home;
            _solid.enabled = !fallen;
            foreach (Collider2D c in _cells) c.enabled = !fallen && c.enabled;
            Color col = _sprite.color; col.a = fallen ? 0f : 1f; _sprite.color = col;
            if (fallen) Fell?.Invoke(); else Restored?.Invoke();
        }

        private bool PlayerOnTop()
        {
            Bounds b = _solid.Bounds;
            Vector2 centre = new(b.center.x, b.max.y + _topProbe.y * 0.5f + 0.02f);
            int n = Physics2D.OverlapBox(centre, new Vector2(b.size.x * _topProbe.x, _topProbe.y), 0f, ContactFilter2D.noFilter, _hits);
            for (int i = 0; i < n; i++) if (_hits[i].GetComponent<PlayerMotor>() != null) return true;
            return false;
        }
    }
}
