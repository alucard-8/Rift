using System;
using UnityEngine;

namespace Rift.Player
{
    /// <summary>
    /// Platformer movement: acceleration-based run, coyote time, jump buffer, variable jump height,
    /// apex hang, double jump, dash, wall slide and wall jump. Knows nothing about input or visuals.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        private Rigidbody2D _body;
        private CapsuleCollider2D _capsule;
        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _solidFilter;

        private float _move;
        private float _facing = 1f;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressTime = float.NegativeInfinity;
        private bool _jumpHeld;
        private bool _isJumping;
        private float _peakFallSpeed;
        private int _airJumpsLeft;
        private bool _dashReady = true;
        private float _dashEnd = float.NegativeInfinity;
        private float _dashDir;
        private float _nextDash;
        private float _controlLockEnd = float.NegativeInfinity;

        public event Action Jumped;
        public event Action DoubleJumped;
        public event Action<float> WallJumped;
        public event Action<float> Dashed;
        public event Action<float> Landed;

        public bool IsGrounded { get; private set; }
        public bool IsDashing => Time.time < _dashEnd;
        /// <summary>-1 wall on the left, 1 wall on the right, 0 none (only while airborne).</summary>
        public int WallSide { get; private set; }
        public bool IsWallSliding { get; private set; }
        public float Facing => _facing;
        public Vector2 Velocity => _body.linearVelocity;
        public PlayerConfig Config => _config;

        public void SetMove(float value)
        {
            _move = Mathf.Clamp(value, -1f, 1f);
            if (Mathf.Abs(_move) > 0.1f) _facing = Mathf.Sign(_move);
        }

        public void PressJump()
        {
            _lastJumpPressTime = Time.time;
            _jumpHeld = true;
        }

        public void ReleaseJump()
        {
            _jumpHeld = false;
            if (_isJumping && _body.linearVelocity.y > 0f)
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, _body.linearVelocity.y * _config.jumpCutMultiplier);
            _isJumping = false;
        }

        public void PressDash()
        {
            if (!_config.allowDash || !_dashReady || Time.time < _nextDash) return;
            _dashReady = IsGrounded;            // one dash per airtime; on the ground it recharges at once
            _dashDir = Mathf.Abs(_move) > 0.1f ? Mathf.Sign(_move) : _facing;
            _dashEnd = Time.time + _config.dashTime;
            _nextDash = Time.time + _config.dashCooldown;
            Dashed?.Invoke(_dashDir);
        }

        /// <summary>Takes the body out of physics entirely (level finished).</summary>
        public void Freeze()
        {
            ResetMotion();
            _body.simulated = false;
        }

        /// <summary>Stops all motion, e.g. after a respawn.</summary>
        public void ResetMotion()
        {
            _body.linearVelocity = Vector2.zero;
            _isJumping = false;
            _lastJumpPressTime = float.NegativeInfinity;
            _dashEnd = float.NegativeInfinity;
            _controlLockEnd = float.NegativeInfinity;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.gravityScale = _config.baseGravityScale;
            _solidFilter = new ContactFilter2D { useTriggers = false };
        }

        private void FixedUpdate()
        {
            UpdateGrounded();
            UpdateWall();
            PushOutOfSolids();

            if (IsDashing)
            {
                _body.gravityScale = 0f;
                _body.linearVelocity = new Vector2(_dashDir * _config.dashSpeed, 0f);
                return;
            }

            if (Time.time >= _controlLockEnd) Run();
            TryJump();
            ApplyGravity();
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;
            Bounds b = _capsule.bounds;
            Vector2 feet = new(b.center.x, b.min.y - _config.groundCheckOffset);
            IsGrounded = Touching(feet, _config.groundCheckSize);

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                _airJumpsLeft = _config.allowDoubleJump ? _config.airJumps : 0;
                _dashReady = true;
                if (!wasGrounded && _body.linearVelocity.y <= 0.01f)
                {
                    Landed?.Invoke(_peakFallSpeed);
                    _isJumping = false;
                }
                _peakFallSpeed = 0f;
            }
            else
            {
                _peakFallSpeed = Mathf.Max(_peakFallSpeed, -_body.linearVelocity.y);
            }
        }

        private void UpdateWall()
        {
            WallSide = 0;
            IsWallSliding = false;
            if (IsGrounded || !_config.allowWallJump) return;

            Bounds b = _capsule.bounds;
            Vector2 size = new(_config.wallCheckDepth, b.size.y * 0.6f);
            if (Touching(new Vector2(b.max.x + size.x * 0.5f, b.center.y), size)) WallSide = 1;
            else if (Touching(new Vector2(b.min.x - size.x * 0.5f, b.center.y), size)) WallSide = -1;

            // Sliding: pressing into the wall while falling.
            IsWallSliding = WallSide != 0 && _body.linearVelocity.y < 0f && Mathf.Sign(_move) == WallSide && Mathf.Abs(_move) > 0.1f;
        }

        private bool Touching(Vector2 centre, Vector2 size)
        {
            int count = Physics2D.OverlapBox(centre, size, 0f, _solidFilter, _hits);
            for (int i = 0; i < count; i++)
                if (_hits[i].attachedRigidbody != _body) return true;
            return false;
        }

        // Safety net: if the lens ever materialises geometry around us, slide out instead of getting stuck.
        private void PushOutOfSolids()
        {
            int count = _capsule.Overlap(_solidFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].attachedRigidbody == _body) continue;
                ColliderDistance2D d = _capsule.Distance(_hits[i]);
                if (d.isOverlapped)
                    _body.position += d.normal * d.distance;
            }
        }

        private void Run()
        {
            float target = _move * _config.runSpeed;
            float current = _body.linearVelocity.x;
            bool accelerating = Mathf.Abs(target) > 0.01f;
            float rate = IsGrounded
                ? (accelerating ? _config.groundAcceleration : _config.groundDeceleration)
                : (accelerating ? _config.airAcceleration : _config.airDeceleration);

            float x = Mathf.MoveTowards(current, target, rate * Time.fixedDeltaTime);
            _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);
        }

        private void TryJump()
        {
            bool buffered = Time.time - _lastJumpPressTime <= _config.jumpBuffer;
            if (!buffered) return;
            bool coyote = Time.time - _lastGroundedTime <= _config.coyoteTime;

            if (coyote && !_isJumping)
            {
                LaunchUp(_config.jumpHeight);
                _lastGroundedTime = float.NegativeInfinity;
                Jumped?.Invoke();
            }
            else if (WallSide != 0)
            {
                // Kick away from the wall; briefly ignore steering so the kick reads.
                float away = -WallSide;
                LaunchUp(_config.wallJumpHeight);
                _body.linearVelocity = new Vector2(away * _config.wallJumpPush, _body.linearVelocity.y);
                _controlLockEnd = Time.time + _config.wallJumpControlLock;
                _facing = away;
                _dashReady = true;
                WallJumped?.Invoke(away);
            }
            else if (_airJumpsLeft > 0)
            {
                _airJumpsLeft--;
                LaunchUp(_config.doubleJumpHeight);
                DoubleJumped?.Invoke();
            }
            else return;

            _isJumping = true;
            _lastJumpPressTime = float.NegativeInfinity;

            // Released before the jump actually fired (buffered tap): make it a short hop.
            if (!_jumpHeld) ReleaseJump();
        }

        private void LaunchUp(float height)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * _config.baseGravityScale;
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, Mathf.Sqrt(2f * gravity * height));
        }

        private void ApplyGravity()
        {
            float vy = _body.linearVelocity.y;
            float scale = _config.baseGravityScale;
            if (vy < 0f) scale *= _config.fallGravityMultiplier;
            // Apex hang: floatier near the top of a held jump, for precise aiming.
            if (!IsGrounded && _jumpHeld && Mathf.Abs(vy) < _config.apexThreshold) scale *= _config.apexGravityMultiplier;
            _body.gravityScale = scale;

            float maxFall = IsWallSliding ? _config.wallSlideSpeed : _config.maxFallSpeed;
            if (vy < -maxFall)
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, -maxFall);
        }
    }
}
