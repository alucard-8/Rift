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
            DeathCause cause = _respawn.LastCause;
            for (int i = 0; i < _shardCount; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                emit.position = at + (Vector3)(Random.insideUnitCircle * 0.3f);
                emit.startSize = Random.Range(0.08f, 0.22f);
                emit.startLifetime = Random.Range(0.4f, 0.9f);
                switch (cause)
                {
                    case DeathCause.Laser: // burnt into a thin rising column of ash
                        emit.position = at + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.6f, 0.6f));
                        emit.velocity = Vector3.up * Random.Range(1f, 2f) * _shardSpeed;
                        emit.startColor = new Color(0.5f, 2.4f, 2.8f);
                        emit.startSize = Random.Range(0.04f, 0.1f);
                        break;
                    case DeathCause.Debris: // flattened: shards squirt out along the ground
                        emit.position = at + Vector3.down * 0.4f;
                        emit.velocity = new Vector3(Mathf.Sign(dir.x) * Random.Range(0.5f, 1.6f) * _shardSpeed, Random.Range(0f, 1f));
                        emit.startColor = new Color(1f, 0.6f, 0.3f);
                        break;
                    case DeathCause.Glitch: // torn into square static that hangs in place
                        emit.position = at + (Vector3)(Random.insideUnitCircle * 0.9f);
                        emit.velocity = new Vector3(Random.value < 0.5f ? -1f : 1f, 0f) * Random.Range(0f, 0.6f) * _shardSpeed;
                        emit.startColor = Random.value < 0.5f ? new Color(2f, 0.2f, 1.6f) : new Color(0.2f, 2f, 2f);
                        emit.startLifetime = Random.Range(0.7f, 1.2f);
                        break;
                    default: // lost to the fall: a soft scatter
                        emit.velocity = (Vector3)(dir * Random.Range(0.4f, 1f) * _shardSpeed) + Vector3.up * 2f;
                        emit.startColor = Color.white;
                        break;
                }
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
