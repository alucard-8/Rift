using UnityEngine;

namespace Rift.Enemies
{
    [CreateAssetMenu(menuName = "RIFT/Glitch Config", fileName = "GlitchConfig")]
    public class GlitchConfig : ScriptableObject
    {
        [Header("Idle")]
        [Min(0f)] public float acceleration = 9f;
        [Min(0f)] public float bobAmplitude = 0.25f;
        [Min(0f)] public float bobSpeed = 2.2f;

        [Header("Unseen: creeps toward you as a ghost")]
        [Tooltip("Speed it slides toward the player while outside the lens (unreal, harmless).")]
        [Min(0f)] public float creepSpeed = 1.2f;
        [Tooltip("Only creeps when the player is closer than this.")]
        [Min(0f)] public float creepRange = 14f;
        [Tooltip("Never creeps closer than this — it waits for you to look.")]
        [Min(0f)] public float creepStopDistance = 2.2f;
        [Tooltip("It guards a spot: never creeps further than this from where it started.")]
        [Min(0f)] public float creepLeash = 4f;

        [Header("Seen: winds up, then lunges")]
        [Tooltip("Strikes only when the player is closer than this.")]
        [Min(0f)] public float detectRange = 7f;
        [Tooltip("The tell: it stops and locks on for this long before lunging.")]
        [Min(0f)] public float windupTime = 0.45f;
        [Min(0f)] public float lungeSpeed = 11f;
        [Min(0f)] public float lungeTime = 0.35f;
        [Tooltip("Slow and open after a lunge.")]
        [Min(0f)] public float recoverTime = 0.6f;

        [Header("Crush")]
        [Tooltip("Radius used to test whether returning Ember rock crushes it.")]
        [Min(0f)] public float crushRadius = 0.3f;
    }
}
