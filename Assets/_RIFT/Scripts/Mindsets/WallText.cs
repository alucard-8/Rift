using TMPro;
using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>
    /// A line of thought written onto the world. It types itself out the first time the player comes near,
    /// stays pinned where it is, and belongs to one world: Ember lines show outside the lens,
    /// Static lines only inside it, letter by letter where the rim cuts through.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class WallText : MonoBehaviour
    {
        [SerializeField] private RiftWorld _voice = RiftWorld.Ember;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private RiftLens _lens;
        [SerializeField] private Transform _player;
        [SerializeField] private float _triggerRange = 9f;
        [SerializeField] private float _charsPerSecond = 18f;
        [Tooltip("Characters fade in over this many characters of typing.")]
        [SerializeField] private float _fadeWidth = 3f;
        [Tooltip("Soft band at the lens rim, in world units.")]
        [SerializeField] private float _rimSoftness = 0.35f;

        private TMP_Text _text;
        private float _typed;
        private bool _started;

        public RiftWorld Voice => _voice;
        /// <summary>How many characters have been written so far.</summary>
        public int TypedCount => Mathf.FloorToInt(_typed);
        public bool IsFullyTyped => _text != null && _typed >= _text.textInfo.characterCount + _fadeWidth;

        /// <summary>Write the whole line at once (used when the rift resolves).</summary>
        public void RevealAll()
        {
            _started = true;
            _typed = _text.textInfo.characterCount + _fadeWidth + 1f;
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _text.color = _theme.TextColorFor(_voice);
        }

        private void LateUpdate()
        {
            if (!_started && Vector2.Distance(_player.position, transform.position) < _triggerRange)
                _started = true;
            if (_started) _typed += _charsPerSecond * Time.deltaTime;

            ApplyAlphas();
        }

        /// <summary>Per character: alpha = typed-in amount × visibility in this voice's world.</summary>
        private void ApplyAlphas()
        {
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float radius = _lens.Radius;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;

                float typedIn = Mathf.Clamp01((_typed - i) / _fadeWidth);

                Vector3 mid = (c.bottomLeft + c.topRight) * 0.5f;
                Vector2 world = _text.transform.TransformPoint(mid);
                float d = Vector2.Distance(world, _lens.Center) - radius;
                float inside = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-_rimSoftness, _rimSoftness, d));
                float inWorld = _voice == RiftWorld.Static ? inside : 1f - inside;

                byte a = (byte)(255f * typedIn * inWorld * _text.color.a);
                Color32[] colors = info.meshInfo[c.materialReferenceIndex].colors32;
                for (int v = 0; v < 4; v++) colors[c.vertexIndex + v].a = a;
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
