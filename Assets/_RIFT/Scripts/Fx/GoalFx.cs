using Rift.Levels;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Portal motes always drift in; reaching the goal releases a big flash burst.</summary>
    public class GoalFx : MonoBehaviour
    {
        [SerializeField] private Goal _goal;
        [SerializeField] private ParticleSystem _motes;
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private int _burstCount = 80;
        [SerializeField] private Transform _visual;
        [SerializeField] private float _sealedScale = 1.6f;

        private Vector3 _baseScale;
        private bool _sealed;

        private void Awake() => _baseScale = _visual.localScale;
        private void OnEnable() => _goal.Reached += OnReached;
        private void OnDisable() => _goal.Reached -= OnReached;

        private void OnReached()
        {
            _sealed = true;
            _burst.Emit(_burstCount);
            _motes.Stop();
        }

        private void Update()
        {
            Vector3 target = _sealed ? _baseScale * _sealedScale : _baseScale;
            _visual.localScale = Vector3.Lerp(_visual.localScale, target, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        }
    }
}
