using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Squash and stretch on the player's visual child. Purely cosmetic; reacts to motor events.</summary>
    public class PlayerSquash : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Vector2 _jumpStretch = new(0.75f, 1.3f);
        [SerializeField] private Vector2 _landSquash = new(1.35f, 0.7f);
        [Tooltip("Fall speed at which the landing squash is at full strength.")]
        [SerializeField] private float _fullSquashSpeed = 18f;
        [SerializeField] private float _recoverSharpness = 14f;

        private Vector3 _baseScale;

        private void Awake() => _baseScale = transform.localScale;

        private void OnEnable()
        {
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
            _motor.DoubleJumped += OnJumped;
            _motor.WallJumped += OnWallJumped;
            _motor.Dashed += OnDashed;
        }

        private void OnDisable()
        {
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
            _motor.DoubleJumped -= OnJumped;
            _motor.WallJumped -= OnWallJumped;
            _motor.Dashed -= OnDashed;
        }

        private void OnJumped() => SetShape(_jumpStretch);
        private void OnWallJumped(float away) => SetShape(_jumpStretch);
        private void OnDashed(float dir) => SetShape(new Vector2(1.45f, 0.7f));

        private void OnLanded(float impactSpeed)
        {
            float t = Mathf.Clamp01(impactSpeed / _fullSquashSpeed);
            SetShape(Vector2.Lerp(Vector2.one, _landSquash, t));
        }

        private void SetShape(Vector2 shape) =>
            transform.localScale = new Vector3(_baseScale.x * shape.x, _baseScale.y * shape.y, _baseScale.z);

        private void Update()
        {
            float t = 1f - Mathf.Exp(-_recoverSharpness * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, t);
        }
    }
}
