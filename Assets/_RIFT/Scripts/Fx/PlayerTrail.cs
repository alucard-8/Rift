using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Short light trail behind the player, only at near full speed.</summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class PlayerTrail : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;
        [Range(0f, 1f)] [SerializeField] private float _speedFraction = 0.7f;

        private TrailRenderer _trail;

        private void Awake() => _trail = GetComponent<TrailRenderer>();

        private void Update()
        {
            float threshold = _motor.Config.runSpeed * _speedFraction;
            _trail.emitting = _motor.Velocity.magnitude > threshold;
        }
    }
}
