using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Enemies
{
    /// <summary>
    /// A Static-world creature. Inside the lens it is real and hunts the player;
    /// outside it freezes. If Ember rock re-forms through it as the lens leaves, it shatters.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(RiftPresence))]
    public class GlitchEnemy : MonoBehaviour
    {
        [SerializeField] private GlitchConfig _config;
        [SerializeField] private PlayerRespawn _player;

        private Rigidbody2D _body;
        private RiftPresence _presence;
        private Vector2 _home;
        private bool _crushCheckPending;
        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _solidFilter;

        public event Action Killed;
        public event Action HitPlayer;

        public bool IsAlive { get; private set; } = true;
        // Resolved on demand: sibling components may subscribe in OnEnable before our Awake has run.
        public RiftPresence Presence => _presence != null ? _presence : _presence = GetComponent<RiftPresence>();
        public Vector2 Home => _home;
        public Transform Target => _player.transform;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _presence = GetComponent<RiftPresence>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _home = transform.position;
            _solidFilter = new ContactFilter2D { useTriggers = false };
        }

        private void OnEnable()
        {
            _presence.Dephased += OnDephased;
            _player.Respawned += OnPlayerRespawned;
        }

        private void OnDisable()
        {
            _presence.Dephased -= OnDephased;
            _player.Respawned -= OnPlayerRespawned;
        }

        // Ember cells switch back on in the same physics step the lens leaves,
        // so the crush test runs one step later, once the rock is solid again.
        private void OnDephased() => _crushCheckPending = true;

        private void FixedUpdate()
        {
            if (_crushCheckPending)
            {
                _crushCheckPending = false;
                if (InsideEmberRock()) { Die(); return; }
            }

            if (!_presence.IsReal)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toPlayer = (Vector2)_player.transform.position - _body.position;
            Vector2 desired;
            if (toPlayer.magnitude < _config.detectRange)
                desired = toPlayer.normalized * _config.chaseSpeed;
            else
            {
                Vector2 bobPoint = _home + Vector2.up * (Mathf.Sin(Time.time * _config.bobSpeed) * _config.bobAmplitude);
                desired = (bobPoint - _body.position) * 3f;
            }

            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, desired, _config.acceleration * Time.fixedDeltaTime);
        }

        private bool InsideEmberRock()
        {
            int count = Physics2D.OverlapCircle(_body.position, _config.crushRadius, _solidFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                RiftSolid solid = _hits[i].GetComponentInParent<RiftSolid>();
                if (solid != null && solid.World == RiftWorld.Ember) return true;
            }
            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive || !_presence.IsReal || other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.gameObject != _player.gameObject) return;

            HitPlayer?.Invoke();
            _player.Respawn();
        }

        private void OnPlayerRespawned()
        {
            if (!IsAlive) return;
            _body.position = _home;
            transform.position = _home;
            _body.linearVelocity = Vector2.zero;
        }

        private void Die()
        {
            IsAlive = false;
            Killed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
