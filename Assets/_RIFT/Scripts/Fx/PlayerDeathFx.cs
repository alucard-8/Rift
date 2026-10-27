using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>The player shatters where they died and re-forms (implodes) at the respawn point.</summary>
    public class PlayerDeathFx : MonoBehaviour
    {
        [SerializeField] private PlayerRespawn _respawn;
        [SerializeField] private ParticleSystem _shards;
        [SerializeField] private int _shardCount = 28;
        [SerializeField] private float _shardSpeed = 6f;
        [SerializeField] private int _implodeCount = 22;
        [SerializeField] private float _implodeRadius = 1.6f;
        [SerializeField] private float _implodeTime = 0.35f;

        private void OnEnable()
        {
            _respawn.Died += OnDied;
            _respawn.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            _respawn.Died -= OnDied;
            _respawn.Respawned -= OnRespawned;
        }

        // Died fires before the teleport, so the player is still where they fell.
        private void OnDied()
        {
            var emit = new ParticleSystem.EmitParams();
            Vector3 at = _respawn.transform.position;
            for (int i = 0; i < _shardCount; i++)
            {
                emit.position = at + (Vector3)(Random.insideUnitCircle * 0.3f);
                emit.velocity = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * _shardSpeed) + Vector3.up * 2f;
                emit.startSize = Random.Range(0.08f, 0.22f);
                emit.startLifetime = Random.Range(0.4f, 0.9f);
                _shards.Emit(emit, 1);
            }
        }

        private void OnRespawned()
        {
            var emit = new ParticleSystem.EmitParams();
            Vector3 at = _respawn.transform.position;
            for (int i = 0; i < _implodeCount; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                emit.position = at + (Vector3)(dir * _implodeRadius);
                emit.velocity = -(Vector3)dir * (_implodeRadius / _implodeTime);
                emit.startLifetime = _implodeTime;
                emit.startSize = Random.Range(0.06f, 0.14f);
                _shards.Emit(emit, 1);
            }
        }
    }
}
