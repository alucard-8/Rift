using UnityEngine;

namespace Rift
{
    [CreateAssetMenu(menuName = "RIFT/Lens Config", fileName = "LensConfig")]
    public class LensConfig : ScriptableObject
    {
        [Tooltip("Lens radius in world units.")]
        [Min(0.1f)] public float radius = 2.2f;

        [Tooltip("Width of the soft visual edge in world units.")]
        [Min(0f)] public float edgeWidth = 0.18f;

        [Tooltip("How fast the lens catches up with the cursor (higher = snappier).")]
        [Min(0.1f)] public float followSharpness = 22f;

        [Tooltip("Size of the collider cells RiftSolid splits platforms into.")]
        [Min(0.05f)] public float cellSize = 0.25f;
    }
}
