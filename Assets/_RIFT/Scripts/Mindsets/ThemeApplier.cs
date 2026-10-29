using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>
    /// Restyles a level from its mindset theme: platform and background materials.
    /// With a pool of themes (the title screen), it picks one at random each time and tints the logo to match.
    /// </summary>
    public class ThemeApplier : MonoBehaviour
    {
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private RiftSolid[] _solids;
        [SerializeField] private Renderer _background;
        [SerializeField] private Renderer _lensEdge;
        [Tooltip("Optional: pick one of these at random instead of the theme above.")]
        [SerializeField] private MindsetTheme[] _randomThemes;
        [Tooltip("Optional: logo whose rift effect takes this theme's colours.")]
        [SerializeField] private Renderer _logo;

        public MindsetTheme Theme => _theme;

        static readonly int AccentId = Shader.PropertyToID("_RiftAccent");
        static readonly int WarmId = Shader.PropertyToID("_WarmColor");
        static readonly int RimId = Shader.PropertyToID("_RimColor");

        private void Awake()
        {
            if (_randomThemes != null && _randomThemes.Length > 0)
                _theme = _randomThemes[Random.Range(0, _randomThemes.Length)];

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

            if (_logo != null)
            {
                var block = new MaterialPropertyBlock();
                _logo.GetPropertyBlock(block);
                block.SetColor(WarmId, _theme.accentColor);
                block.SetColor(RimId, _theme.staticTextColor);
                _logo.SetPropertyBlock(block);
            }
        }
    }
}
