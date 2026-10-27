using System;
using UnityEngine;

namespace Rift
{
    /// <summary>
    /// Makes a single object exist only where its world is real (Static: inside the lens, Ember: outside).
    /// Toggles its colliders and announces when it materialises or dephases.
    /// </summary>
    public class RiftPresence : MonoBehaviour
    {
        [SerializeField] private RiftWorld _world = RiftWorld.Static;
        [SerializeField] private RiftLens _lens;
        [SerializeField] private Collider2D[] _colliders;
        [Tooltip("How fast the visual Phase follows the real state.")]
        [SerializeField] private float _phaseSpeed = 8f;

        public event Action Materialized;
        public event Action Dephased;

        public bool IsReal { get; private set; }
        /// <summary>Smoothed 0..1 for visuals: 1 = fully real.</summary>
        public float Phase { get; private set; }
        public RiftWorld World => _world;
        public RiftLens Lens => _lens;

        private void Start()
        {
            IsReal = _lens.IsReal(_world, transform.position);
            Phase = IsReal ? 1f : 0f;
            ApplyColliders();
        }

        private void FixedUpdate()
        {
            bool real = _lens.IsReal(_world, transform.position);
            if (real == IsReal) return;

            IsReal = real;
            ApplyColliders();
            if (real) Materialized?.Invoke();
            else Dephased?.Invoke();
        }

        private void Update() =>
            Phase = Mathf.MoveTowards(Phase, IsReal ? 1f : 0f, _phaseSpeed * Time.deltaTime);

        private void ApplyColliders()
        {
            foreach (Collider2D c in _colliders) c.enabled = IsReal;
        }
    }
}
