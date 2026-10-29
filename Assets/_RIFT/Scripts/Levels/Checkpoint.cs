using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Levels
{
    /// <summary>Trigger that moves the player's respawn point here the first time they touch it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;

        public event Action Activated;

        public bool IsActive { get; private set; }
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive || !other.TryGetComponent(out PlayerRespawn respawn)) return;

            respawn.SetSpawnPoint(SpawnPoint);
            IsActive = true;
            Activated?.Invoke();
        }
    }
}
