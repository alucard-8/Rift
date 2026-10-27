using System;
using UnityEngine;

namespace Rift.Gadgets
{
    /// <summary>
    /// "Hold the rift here": charges while the lens covers it, and once full stays on for a while
    /// after the lens leaves. Doors and other gadgets listen to it.
    /// </summary>
    public class LensSwitch : MonoBehaviour
    {
        [SerializeField] private RiftLens _lens;
        [SerializeField] private int _chargeSteps = 35;    // 0.7 s of holding
        [SerializeField] private int _holdOpenSteps = 220; // 4.4 s after release

        private int _charge;
        private int _openTimer;

        public event Action<bool> Toggled;

        public bool IsOn { get; private set; }
        public float Charge01 => (float)_charge / _chargeSteps;

        private void FixedUpdate()
        {
            bool covered = _lens.Contains(transform.position);
            _charge = Mathf.Clamp(_charge + (covered ? 1 : -2), 0, _chargeSteps);

            if (_charge >= _chargeSteps) _openTimer = _holdOpenSteps;
            else if (_openTimer > 0) _openTimer--;

            bool on = _openTimer > 0;
            if (on == IsOn) return;
            IsOn = on;
            Toggled?.Invoke(on);
        }
    }
}
