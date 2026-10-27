using System;
using Rift.Player;
using UnityEngine;

namespace Rift.Levels
{
    /// <summary>End of the level. Fires once when the player touches it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        public event Action Reached;

        public bool IsReached { get; private set; }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsReached || !other.TryGetComponent(out PlayerMotor _)) return;

            IsReached = true;
            Reached?.Invoke();
        }
    }
}
