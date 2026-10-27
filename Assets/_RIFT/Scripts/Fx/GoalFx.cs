using Rift.Levels;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>The rift gate breathes while it waits, then tears wide open when the player reaches it.</summary>
    public class GoalFx : MonoBehaviour
    {
        static readonly int OpenId = Shader.PropertyToID("_Open");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private Goal _goal;
        [SerializeField] private Renderer _gate;
        [SerializeField] private ParticleSystem _motes;
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private int _burstCount = 80;
        [SerializeField] private float _idleOpen = 1f;
        [SerializeField] private float _sealedOpen = 3.5f;
        [SerializeField] private float _openTime = 2f;

        private MaterialPropertyBlock _block;
        private float _open;
        private bool _sealed;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _block.SetFloat(SeedId, transform.position.x);
            _open = _idleOpen;
        }

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
            float target = _sealed ? _sealedOpen : _idleOpen;
            _open = Mathf.MoveTowards(_open, target, (_sealedOpen - _idleOpen) / _openTime * Time.unscaledDeltaTime);
            _block.SetFloat(OpenId, _open);
            _gate.SetPropertyBlock(_block);
        }
    }
}
