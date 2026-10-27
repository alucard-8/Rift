using UnityEngine;

namespace Rift.Enemies
{
    [CreateAssetMenu(menuName = "RIFT/Glitch Config", fileName = "GlitchConfig")]
    public class GlitchConfig : ScriptableObject
    {
        [Min(0f)] public float chaseSpeed = 3.2f;
        [Min(0f)] public float acceleration = 9f;
        [Tooltip("Starts chasing when the player is closer than this.")]
        [Min(0f)] public float detectRange = 6f;
        [Min(0f)] public float bobAmplitude = 0.25f;
        [Min(0f)] public float bobSpeed = 2.2f;
        [Tooltip("Radius used to test whether returning Ember rock crushes it.")]
        [Min(0f)] public float crushRadius = 0.3f;
    }
}
