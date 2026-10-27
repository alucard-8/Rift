using Rift.Levels;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Wakes the checkpoint's memory shard when it activates: it lights up, rings pulse out, sparks fly.</summary>
    public class CheckpointGlow : MonoBehaviour
    {
        static readonly int LitId = Shader.PropertyToID("_Lit");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private Checkpoint _checkpoint;
        [SerializeField] private Renderer _shard;
        [SerializeField] private float _wakeTime = 0.6f;
        [SerializeField] private float _pulseTime = 1.1f;
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private int _burstCount = 30;

        private MaterialPropertyBlock _block;
        private float _lit;
        private float _pulse;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _block.SetFloat(SeedId, transform.position.x * 0.37f);
            Apply();
        }

        private void OnEnable() => _checkpoint.Activated += OnActivated;
        private void OnDisable() => _checkpoint.Activated -= OnActivated;

        private void OnActivated()
        {
            _pulse = 0.0001f;
            if (_burst != null) _burst.Emit(_burstCount);
        }

        private void Update()
        {
            if (_checkpoint.IsActive) _lit = Mathf.MoveTowards(_lit, 1f, Time.deltaTime / _wakeTime);
            if (_pulse > 0f) _pulse = _pulse >= 1f ? 0f : Mathf.Min(1f, _pulse + Time.deltaTime / _pulseTime);
            Apply();
        }

        private void Apply()
        {
            _block.SetFloat(LitId, _lit);
            _block.SetFloat(PulseId, _pulse);
            _shard.SetPropertyBlock(_block);
        }
    }
}
