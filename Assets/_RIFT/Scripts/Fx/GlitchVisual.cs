using Rift.Enemies;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Feeds the glitch shader its phase (ghost vs real) and where the eye looks.</summary>
    public class GlitchVisual : MonoBehaviour
    {
        static readonly int PhaseId = Shader.PropertyToID("_Phase");
        static readonly int LookId = Shader.PropertyToID("_LookDir");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private GlitchEnemy _glitch;
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _block.SetFloat(SeedId, Random.value * 10f);
        }

        private void LateUpdate()
        {
            Vector2 look = _glitch.Target.position - transform.position;
            _block.SetFloat(PhaseId, _glitch.Presence.Phase);
            _block.SetVector(LookId, look.normalized);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
