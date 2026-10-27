using System;
using UnityEngine;

namespace Rift.Player
{
    /// <summary>
    /// Platformer movement: acceleration-based run, coyote time, jump buffer,
    /// variable jump height and heavier falls. Knows nothing about input or visuals.
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
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressTime = float.NegativeInfinity;
        private bool _jumpHeld;
        private bool _isJumping;
        private float _peakFallSpeed;

        public event Action Jumped;
        public event Action<float> Landed;

        public bool IsGrounded { get; private set; }
        public Vector2 Velocity => _body.linearVelocity;
        public PlayerConfig Config => _config;

        public void SetMove(float value) => _move = Mathf.Clamp(value, -1f, 1f);

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

        /// <summary>Stops all motion, e.g. after a respawn.</summary>
        public void ResetMotion()
        {
            _body.linearVelocity = Vector2.zero;
            _isJumping = false;
            _lastJumpPressTime = float.NegativeInfinity;
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
            PushOutOfSolids();
            Run();
            TryJump();
            ApplyGravity();
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;
            Bounds b = _capsule.bounds;
            Vector2 feet = new(b.center.x, b.min.y - _config.groundCheckOffset);
            IsGrounded = false;

            int count = Physics2D.OverlapBox(feet, _config.groundCheckSize, 0f, _solidFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].attachedRigidbody == _body) continue;
                IsGrounded = true;
                break;
            }

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
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
            bool coyote = Time.time - _lastGroundedTime <= _config.coyoteTime;
            if (!buffered || !coyote || _isJumping) return;

            float gravity = Mathf.Abs(Physics2D.gravity.y) * _config.baseGravityScale;
            float speed = Mathf.Sqrt(2f * gravity * _config.jumpHeight);
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, speed);

            _isJumping = true;
            _lastJumpPressTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
            Jumped?.Invoke();

            // Released before the jump actually fired (buffered tap): make it a short hop.
            if (!_jumpHeld) ReleaseJump();
        }

        private void ApplyGravity()
        {
            bool falling = _body.linearVelocity.y < 0f;
            _body.gravityScale = _config.baseGravityScale * (falling ? _config.fallGravityMultiplier : 1f);

            if (_body.linearVelocity.y < -_config.maxFallSpeed)
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, -_config.maxFallSpeed);
        }
    }
}
