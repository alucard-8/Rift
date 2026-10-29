using Rift.Enemies;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// Crackle when a glitch materialises, a shard burst and shake when rock crushes it.
    /// Emits into a shared scene particle system so the burst survives the glitch being disabled.
    /// </summary>
    public class GlitchFx : MonoBehaviour
    {
        [SerializeField] private GlitchEnemy _glitch;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private Color _crackleColor = new(0.4f, 2.2f, 2.8f);
        [SerializeField] private Color _crushColor = new(2.6f, 0.35f, 1.8f);
        [SerializeField] private int _crackleCount = 10;
        [SerializeField] private int _crushCount = 40;
        [SerializeField] private float _crushTrauma = 0.35f;

        [SerializeField] private Color _tellColor = new(3.2f, 2.6f, 2.8f);
        [SerializeField] private float _lungeTrauma = 0.12f;
        [Tooltip("Trail particles emitted per frame while lunging.")]
        [SerializeField] private int _trailPerFrame = 3;

        private void OnEnable()
        {
            _glitch.Presence.Materialized += OnMaterialized;
            _glitch.Killed += OnKilled;
            _glitch.WindupStarted += OnWindup;
            _glitch.Lunged += OnLunged;
        }

        private void OnDisable()
        {
            _glitch.Presence.Materialized -= OnMaterialized;
            _glitch.Killed -= OnKilled;
            _glitch.WindupStarted -= OnWindup;
            _glitch.Lunged -= OnLunged;
        }

        private void OnMaterialized() => Burst(_crackleCount, _crackleColor, 2.5f, 0.35f);
        private void OnWindup() => Burst(_crackleCount, _tellColor, 1.5f, 0.3f);

        private void OnLunged()
        {
            if (_shake != null) _shake.AddTrauma(_lungeTrauma);
        }

        // Streak left behind while it lunges.
        private void Update()
        {
            if (_glitch.State != GlitchEnemy.Mode.Lunge) return;
            var emit = new ParticleSystem.EmitParams { startColor = _crushColor };
            Vector2 back = -_glitch.LungeDirection;
            for (int i = 0; i < _trailPerFrame; i++)
            {
                emit.position = _glitch.transform.position + (Vector3)(Random.insideUnitCircle * 0.18f);
                emit.velocity = back * Random.Range(0.5f, 2f) + Random.insideUnitCircle * 0.4f;
                emit.startLifetime = Random.Range(0.15f, 0.35f);
                emit.startSize = Random.Range(0.05f, 0.12f);
                _particles.Emit(emit, 1);
            }
        }

        private void OnKilled()
        {
            Burst(_crushCount, _crushColor, 7f, 0.8f);
            if (_shake != null) _shake.AddTrauma(_crushTrauma);
        }

        private void Burst(int count, Color color, float speed, float life)
        {
            var emit = new ParticleSystem.EmitParams { startColor = color };
            Vector3 at = _glitch.transform.position;
            for (int i = 0; i < count; i++)
            {
                emit.position = at + (Vector3)(Random.insideUnitCircle * 0.25f);
                emit.velocity = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * speed;
                emit.startLifetime = Random.Range(life * 0.5f, life);
                emit.startSize = Random.Range(0.06f, 0.18f);
                _particles.Emit(emit, 1);
            }
        }
    }
}
