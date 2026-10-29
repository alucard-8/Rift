using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Enemies
{
    /// <summary>
    /// A Static-world creature. Outside the lens it is unreal and harmless, but it keeps creeping toward the player.
    /// Inside the lens it is real: it locks on, winds up, and lunges. If Ember rock re-forms through it as the lens
    /// leaves, it shatters.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(RiftPresence))]
    public class GlitchEnemy : MonoBehaviour
    {
        public enum Mode { Unseen, Idle, Windup, Lunge, Recover }

        [SerializeField] private GlitchConfig _config;
        [SerializeField] private PlayerRespawn _player;
        [Tooltip("Off for glitches that are part of a puzzle spot (e.g. buried in a wall to be crushed).")]
        [SerializeField] private bool _creeps = true;

        private Rigidbody2D _body;
        private RiftPresence _presence;
        private Vector2 _home;
        private Vector2 _spawnHome;   // where it goes back to when the player respawns
        private bool _crushCheckPending;
        private float _stateTime;
        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _solidFilter;

        public event Action Killed;
        public event Action HitPlayer;
        public event Action WindupStarted;
        public event Action Lunged;

        public bool IsAlive { get; private set; } = true;
        public Mode State { get; private set; } = Mode.Unseen;
        /// <summary>Direction locked at the start of the windup; the lunge follows it.</summary>
        public Vector2 LungeDirection { get; private set; } = Vector2.right;
        /// <summary>0..1 through the windup (for the tell).</summary>
        public float Windup01 => State == Mode.Windup ? Mathf.Clamp01(_stateTime / _config.windupTime) : 0f;
        // Resolved on demand: sibling components may subscribe in OnEnable before our Awake has run.
        public RiftPresence Presence => _presence != null ? _presence : _presence = GetComponent<RiftPresence>();
        public Vector2 Home => _home;
        public Transform Target => _player.transform;
        public GlitchConfig Config => _config;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _presence = GetComponent<RiftPresence>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _home = _spawnHome = transform.position;
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

            float dt = Time.fixedDeltaTime;
            _stateTime += dt;
            // Aim at the player's middle: a lunge from above carries on into the ground behind them.
            Vector2 toPlayer = (Vector2)_player.transform.position + Vector2.up * 0.45f - _body.position;

            if (!_presence.IsReal) { Creep(toPlayer, dt); return; }
            if (State == Mode.Unseen) Enter(Mode.Idle);

            switch (State)
            {
                case Mode.Idle:
                    if (toPlayer.magnitude < _config.detectRange) { BeginWindup(toPlayer); break; }
                    Vector2 bob = _home + Vector2.up * (Mathf.Sin(Time.time * _config.bobSpeed) * _config.bobAmplitude);
                    Steer((bob - _body.position) * 3f, dt);
                    break;

                case Mode.Windup:
                    _body.linearVelocity = Vector2.zero;
                    if (_stateTime >= _config.windupTime)
                    {
                        Enter(Mode.Lunge);
                        _body.linearVelocity = LungeDirection * _config.lungeSpeed;
                        Lunged?.Invoke();
                    }
                    break;

                case Mode.Lunge:
                    _body.linearVelocity = LungeDirection * _config.lungeSpeed;
                    if (_stateTime >= _config.lungeTime) Enter(Mode.Recover);
                    break;

                case Mode.Recover:
                    Steer(Vector2.zero, dt);
                    if (_stateTime >= _config.recoverTime)
                    {
                        if (toPlayer.magnitude < _config.detectRange) BeginWindup(toPlayer);
                        else Enter(Mode.Idle);
                    }
                    break;
            }
        }

        /// <summary>
        /// Unreal: no colliders, can't hurt. A lunge cut short by the lens keeps flying as a ghost and is crushed if it
        /// ends inside rock; otherwise it slides toward the player, never leaving its leash.
        /// </summary>
        private void Creep(Vector2 toPlayer, float dt)
        {
            if (State == Mode.Lunge)
            {
                _body.linearVelocity = LungeDirection * _config.lungeSpeed;
                if (_stateTime < _config.lungeTime) return;
                if (InsideEmberRock()) { Die(); return; }
            }
            if (State != Mode.Unseen) Enter(Mode.Unseen);
            _body.linearVelocity = Vector2.zero;
            float d = toPlayer.magnitude;
            if (!_creeps || d > _config.creepRange || d < _config.creepStopDistance) return;
            Vector2 next = _body.position + toPlayer / d * (_config.creepSpeed * dt);
            next = _spawnHome + Vector2.ClampMagnitude(next - _spawnHome, _config.creepLeash);
            _body.MovePosition(next);
            _home = next;
        }

        private void BeginWindup(Vector2 toPlayer)
        {
            LungeDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
            Enter(Mode.Windup);
            WindupStarted?.Invoke();
        }

        private void Enter(Mode mode)
        {
            State = mode;
            _stateTime = 0f;
        }

        private void Steer(Vector2 desired, float dt) =>
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, desired, _config.acceleration * dt);

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
            // Only the lunge kills: you always get the windup tell first.
            if (!IsAlive || !_presence.IsReal || State != Mode.Lunge || other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.gameObject != _player.gameObject) return;

            HitPlayer?.Invoke();
            _player.Respawn();
        }

        private void OnPlayerRespawned()
        {
            if (!IsAlive) return;
            _home = _spawnHome;
            _body.position = _home;
            transform.position = _home;
            _body.linearVelocity = Vector2.zero;
            Enter(_presence.IsReal ? Mode.Idle : Mode.Unseen);
        }

        private void Die()
        {
            IsAlive = false;
            Killed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
