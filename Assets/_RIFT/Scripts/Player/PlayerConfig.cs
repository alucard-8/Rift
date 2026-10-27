using UnityEngine;

namespace Rift.Player
{
    [CreateAssetMenu(menuName = "RIFT/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Run")]
        [Min(0f)] public float runSpeed = 7.5f;
        [Min(0f)] public float groundAcceleration = 70f;
        [Min(0f)] public float groundDeceleration = 90f;
        [Min(0f)] public float airAcceleration = 45f;
        [Min(0f)] public float airDeceleration = 25f;

        [Header("Jump")]
        [Min(0.1f)] public float jumpHeight = 3.2f;
        [Tooltip("Vertical speed is multiplied by this when jump is released early.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        [Min(1f)] public float fallGravityMultiplier = 1.8f;
        [Min(1f)] public float baseGravityScale = 3f;
        [Min(0f)] public float maxFallSpeed = 22f;
        [Min(0f)] public float coyoteTime = 0.1f;
        [Min(0f)] public float jumpBuffer = 0.12f;

        [Tooltip("Gravity is multiplied by this near the top of a held jump (apex hang).")]
        [Range(0.1f, 1f)] public float apexGravityMultiplier = 0.55f;
        [Tooltip("Vertical speed under which the apex hang applies.")]
        [Min(0f)] public float apexThreshold = 1.6f;

        [Header("Double jump")]
        public bool allowDoubleJump = true;
        [Min(0)] public int airJumps = 1;
        [Min(0.1f)] public float doubleJumpHeight = 2.2f;

        [Header("Dash")]
        public bool allowDash = true;
        [Min(0f)] public float dashSpeed = 19f;
        [Min(0f)] public float dashTime = 0.14f;
        [Min(0f)] public float dashCooldown = 0.35f;

        [Header("Wall")]
        public bool allowWallJump = true;
        [Min(0f)] public float wallSlideSpeed = 2.6f;
        [Min(0.1f)] public float wallJumpHeight = 2.6f;
        [Min(0f)] public float wallJumpPush = 8f;
        [Min(0f)] public float wallJumpControlLock = 0.14f;
        [Min(0.01f)] public float wallCheckDepth = 0.08f;

        [Header("Ground check")]
        public Vector2 groundCheckSize = new(0.5f, 0.08f);
        [Min(0f)] public float groundCheckOffset = 0.02f;

        [Header("World")]
        [Tooltip("Falling below this Y kills the player.")]
        public float killHeight = -10f;
    }
}
