using Rift.Player;
using UnityEngine;

namespace Rift.Levels
{
    /// <summary>Trigger under pits: touching it sends the player back to their last checkpoint.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerRespawn respawn)) respawn.Respawn();
        }
    }
}
