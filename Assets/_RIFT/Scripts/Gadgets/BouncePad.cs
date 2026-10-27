using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Gadgets
{
    /// <summary>
    /// Launches the player upward. It belongs to one world (via RiftPresence), so a Static pad only
    /// springs while the lens is held over it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BouncePad : MonoBehaviour
    {
        [SerializeField] private RiftPresence _presence;
        [SerializeField] private float _launchHeight = 7.5f;
        [Tooltip("Physics steps before the same pad can fire again.")]
        [SerializeField] private int _cooldownSteps = 15;

        private int _cooldown;

        public event Action Bounced;
        public bool IsActive => _presence == null || _presence.IsReal;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void FixedUpdate()
        {
            if (_cooldown > 0) _cooldown--;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_cooldown > 0 || !IsActive || !other.TryGetComponent(out PlayerMotor motor)) return;
            if (motor.Velocity.y > 1f) return;   // only on the way down / standing on it

            motor.Launch(_launchHeight);
            _cooldown = _cooldownSteps;
            Bounced?.Invoke();
        }
    }
}
