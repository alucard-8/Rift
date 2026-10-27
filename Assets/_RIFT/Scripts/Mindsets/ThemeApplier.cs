using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>Restyles a level from its mindset theme: platform and background materials.</summary>
    public class ThemeApplier : MonoBehaviour
    {
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private RiftSolid[] _solids;
        [SerializeField] private Renderer _background;
        [SerializeField] private Renderer _lensEdge;

        public MindsetTheme Theme => _theme;

        static readonly int AccentId = Shader.PropertyToID("_RiftAccent");

        private void Awake()
        {
            Shader.SetGlobalColor(AccentId, _theme.accentColor);

            foreach (RiftSolid solid in _solids)
            {
                Material m = solid.World == RiftWorld.Ember ? _theme.emberMaterial : _theme.staticMaterial;
                if (m != null) solid.GetComponent<SpriteRenderer>().sharedMaterial = m;
            }

            if (_background != null && _theme.backgroundMaterial != null)
                _background.sharedMaterial = _theme.backgroundMaterial;

            if (_lensEdge != null && _theme.lensEdgeMaterial != null)
                _lensEdge.sharedMaterial = _theme.lensEdgeMaterial;
        }
    }
}
