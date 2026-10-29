using System;
using UnityEngine;

namespace Rift.Player
{
    /// <summary>Sends the player back to the spawn point when they fall out of the world.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;

        private PlayerMotor _motor;

        public event Action Died;
        public event Action Respawned;

        public Transform SpawnPoint => _spawnPoint;

        public void SetSpawnPoint(Transform spawn) => _spawnPoint = spawn;

        public void Respawn()
        {
            Died?.Invoke();
            transform.position = _spawnPoint.position;
            GetComponent<Rigidbody2D>().position = _spawnPoint.position;
            _motor.ResetMotion();
            Respawned?.Invoke();
        }

        private void Awake() => _motor = GetComponent<PlayerMotor>();

        private void FixedUpdate()
        {
            if (transform.position.y < _motor.Config.killHeight) Respawn();
        }
    }
}
