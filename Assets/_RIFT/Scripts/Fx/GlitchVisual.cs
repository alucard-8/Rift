using Rift.Enemies;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Feeds the glitch shader its phase (ghost vs real), where the eye looks and how far the windup is.</summary>
    public class GlitchVisual : MonoBehaviour
    {
        static readonly int PhaseId = Shader.PropertyToID("_Phase");
        static readonly int LookId = Shader.PropertyToID("_LookDir");
        static readonly int SeedId = Shader.PropertyToID("_Seed");
        static readonly int WindupId = Shader.PropertyToID("_Windup");

        [SerializeField] private GlitchEnemy _glitch;
        [SerializeField] private Renderer _renderer;
        [Tooltip("How far the body stretches along the lunge.")]
        [SerializeField] private float _lungeStretch = 0.35f;

        private MaterialPropertyBlock _block;
        private Vector3 _baseScale;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _block.SetFloat(SeedId, Random.value * 10f);
            _baseScale = _renderer.transform.localScale;
        }

        private void LateUpdate()
        {
            bool locked = _glitch.State is GlitchEnemy.Mode.Windup or GlitchEnemy.Mode.Lunge;
            Vector2 look = locked ? _glitch.LungeDirection : (Vector2)(_glitch.Target.position - transform.position);
            _block.SetFloat(PhaseId, _glitch.Presence.Phase);
            _block.SetVector(LookId, look.normalized);
            _block.SetFloat(WindupId, _glitch.Windup01);
            _renderer.SetPropertyBlock(_block);

            // Stretch along the lunge, pinch slightly while winding up.
            Vector2 d = _glitch.LungeDirection;
            float stretch = _glitch.State == GlitchEnemy.Mode.Lunge ? _lungeStretch : -0.12f * _glitch.Windup01;
            Vector3 s = _baseScale;
            s.x *= 1f + stretch * Mathf.Abs(d.x) - stretch * 0.5f * Mathf.Abs(d.y);
            s.y *= 1f + stretch * Mathf.Abs(d.y) - stretch * 0.5f * Mathf.Abs(d.x);
            _renderer.transform.localScale = Vector3.Lerp(_renderer.transform.localScale, s, 1f - Mathf.Exp(-20f * Time.deltaTime));
        }
    }
}
