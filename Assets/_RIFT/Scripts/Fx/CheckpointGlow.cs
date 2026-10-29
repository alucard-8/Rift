using Rift.Levels;
using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// The checkpoint's memory shard. Stirs as the player comes close, wakes with a pillar of light and sparks
    /// pulled out of the player, and calls the player back with a beam after a death. Listens to events only.
    /// </summary>
    public class CheckpointGlow : MonoBehaviour
    {
        static readonly int LitId = Shader.PropertyToID("_Lit");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int NearId = Shader.PropertyToID("_Near");
        static readonly int FlareId = Shader.PropertyToID("_Flare");
        static readonly int RecallId = Shader.PropertyToID("_Recall");
        static readonly int SeedId = Shader.PropertyToID("_Seed");
        static readonly int AccentId = Shader.PropertyToID("_RiftAccent");

        [SerializeField] private Checkpoint _checkpoint;
        [SerializeField] private Renderer _shard;
        [SerializeField] private float _wakeTime = 0.6f;
        [SerializeField] private float _pulseTime = 1.1f;
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private int _burstCount = 30;

        [Header("Approach")]
        [SerializeField] private PlayerRespawn _player;
        [SerializeField] private float _noticeRange = 6f;

        [Header("Reached")]
        [SerializeField] private float _flareTime = 1.4f;
        [SerializeField] private int _pullCount = 26;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private float _shakeTrauma = 0.15f;

        [Header("Recall after a death")]
        [SerializeField] private float _recallTime = 0.9f;
        [SerializeField] private int _recallCount = 24;

        private MaterialPropertyBlock _block;
        private float _lit, _pulse, _near, _flare, _recall;

        public float Near => _near;
        public float Recall => _recall;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _block.SetFloat(SeedId, transform.position.x * 0.37f);
            Apply();
        }

        private void OnEnable()
        {
            _checkpoint.Activated += OnActivated;
            if (_player != null) _player.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            _checkpoint.Activated -= OnActivated;
            if (_player != null) _player.Respawned -= OnRespawned;
        }

        private void OnActivated()
        {
            _pulse = 0.0001f;
            _flare = 1f;
            if (_shake != null) _shake.AddTrauma(_shakeTrauma);
            if (_burst == null) return;

            _burst.Emit(_burstCount);
            if (_player != null) Pull(_player.transform.position + Vector3.up * 0.7f, ShardCentre(), _pullCount);
        }

        /// <summary>Beam down + a ring of light closing on the player. Plays after a death at this checkpoint.</summary>
        public void PlayRecall()
        {
            _recall = 1f;
            if (_burst == null || _player == null) return;

            Vector3 at = _player.transform.position + Vector3.up * 0.7f;
            for (int i = 0; i < _recallCount; i++)
            {
                float a = i * Mathf.PI * 2f / _recallCount;
                Vector3 from = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.6f;
                EmitTowards(from, at, 0.35f);
            }
        }

        private void OnRespawned()
        {
            if (_checkpoint.IsActive && _player.SpawnPoint == _checkpoint.SpawnPoint) PlayRecall();
        }

        private void Pull(Vector3 from, Vector3 to, int count)
        {
            for (int i = 0; i < count; i++)
                EmitTowards(from + (Vector3)(Random.insideUnitCircle * 0.4f), to, Random.Range(0.35f, 0.6f));
        }

        private void EmitTowards(Vector3 from, Vector3 to, float life)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = from,
                velocity = (to - from) / life,
                startLifetime = life,
                startSize = Random.Range(0.06f, 0.14f),
                startColor = Shader.GetGlobalColor(AccentId),
            };
            _burst.Emit(emit, 1);
        }

        private Vector3 ShardCentre() => _shard.bounds.center + Vector3.down * _shard.bounds.extents.y * 0.4f;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_checkpoint.IsActive) _lit = Mathf.MoveTowards(_lit, 1f, dt / _wakeTime);
            if (_pulse > 0f) _pulse = _pulse >= 1f ? 0f : Mathf.Min(1f, _pulse + dt / _pulseTime);
            _flare = Mathf.MoveTowards(_flare, 0f, dt / _flareTime);
            _recall = Mathf.MoveTowards(_recall, 0f, dt / _recallTime);

            float target = 0f;
            if (_player != null && !_checkpoint.IsActive)
            {
                float d = Vector2.Distance(_player.transform.position, transform.position);
                target = 1f - Mathf.Clamp01(d / _noticeRange);
            }
            _near = Mathf.MoveTowards(_near, target * target, dt * 2f);
            Apply();
        }

        private void Apply()
        {
            _block.SetFloat(LitId, _lit);
            _block.SetFloat(PulseId, _pulse);
            _block.SetFloat(NearId, _near);
            _block.SetFloat(FlareId, _flare * _flare);
            _block.SetFloat(RecallId, _recall);
            _shard.SetPropertyBlock(_block);
        }
    }
}
