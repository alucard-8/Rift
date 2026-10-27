using System;
using TMPro;
using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>
    /// A line of thought written onto the world. It writes itself the first time the player comes near —
    /// each letter rises in with a hot flash, a caret blinks at the pen — then the line keeps breathing.
    /// It belongs to one world: Ember lines show outside the lens, Static lines only inside it,
    /// letter by letter where the rim cuts through.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class WallText : MonoBehaviour
    {
        [SerializeField] private RiftWorld _voice = RiftWorld.Ember;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private RiftLens _lens;
        [SerializeField] private Transform _player;
        [Tooltip("Optional thin bar that blinks after the last written letter.")]
        [SerializeField] private RectTransform _caret;
        [SerializeField] private float _triggerRange = 9f;
        [SerializeField] private float _charsPerSecond = 16f;
        [Tooltip("Optional soft glow card behind the line; fades in as the line writes.")]
        [SerializeField] private UnityEngine.UI.Graphic _card;
        [SerializeField] private float _cardAlpha = 0.55f;

        [Header("Writing")]
        [Tooltip("Characters fade in over this many characters of typing.")]
        [SerializeField] private float _fadeWidth = 3f;
        [SerializeField] private float _riseDistance = 0.18f;
        [SerializeField] private float _popScale = 0.3f;
        [Tooltip("How many characters a fresh letter stays white-hot for.")]
        [SerializeField] private float _flashLength = 6f;
        [Tooltip("Extra size of the letter under the pen, so the eye follows the writing.")]
        [SerializeField] private float _penScale = 0.55f;

        [Header("Breathing")]
        [SerializeField] private float _breathSpeed = 1.4f;
        [Range(0f, 0.5f)] [SerializeField] private float _breathDepth = 0.18f;
        [Range(0f, 0.05f)] [SerializeField] private float _flickerChance = 0.004f;

        [Header("Lens")]
        [Tooltip("Soft band at the lens rim, in world units.")]
        [SerializeField] private float _rimSoftness = 0.35f;

        private TMP_Text _text;
        private Color32 _baseColor;
        private float _typed;
        private bool _started;

        public RiftWorld Voice => _voice;
        /// <summary>How many characters have been written so far.</summary>
        public int TypedCount => Mathf.FloorToInt(_typed);
        public bool IsFullyTyped => _text != null && _typed >= _text.textInfo.characterCount + _flashLength;
        /// <summary>True only if the player came close enough for this line to start writing itself.</summary>
        public bool WasHeard { get; private set; }

        /// <summary>Raised once, the moment the line starts writing itself.</summary>
        public event Action<WallText> BeganWriting;

        /// <summary>Write the whole line at once (used when the rift resolves).</summary>
        public void RevealAll()
        {
            _started = true;
            _typed = _text.textInfo.characterCount + _flashLength + 1f;
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            // Vertex colours are LDR; the HDR intensity that bloom picks up comes from the glow material.
            Color c = _theme.TextColorFor(_voice);
            float max = Mathf.Max(1f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
            _baseColor = new Color(c.r / max, c.g / max, c.b / max, 1f);
            _text.color = _baseColor;
            Material glow = _theme.TextMaterialFor(_voice);
            if (glow != null) _text.fontSharedMaterial = glow;
            if (_caret != null) _caret.gameObject.SetActive(false);
            if (_card != null) { Color k = _baseColor; k.a = 0f; _card.color = k; }
        }

        private void LateUpdate()
        {
            if (!_started && Vector2.Distance(_player.position, transform.position) < _triggerRange)
            {
                _started = true;
                WasHeard = true;
                BeganWriting?.Invoke(this);
            }
            if (_started) _typed += _charsPerSecond * Time.deltaTime;

            ApplyLetters();
        }

        private void ApplyLetters()
        {
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float radius = _lens.Radius;
            float t = Time.time;
            int lastWritten = -1;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;

                float age = _typed - i;
                float appear = Mathf.Clamp01(age / _fadeWidth);
                if (appear > 0f) lastWritten = i;

                // Pop in: rise into place, overshoot in size, start white-hot and cool to the voice colour.
                float ease = 1f - (1f - appear) * (1f - appear);
                float rise = (1f - ease) * _riseDistance;
                float scale = 1f + _popScale * Mathf.Sin(appear * Mathf.PI) * (1f - appear * 0.5f);
                float pen = Mathf.Clamp01(1f - Mathf.Abs(age - 1f) / 1.5f);   // the letter being written right now
                scale += _penScale * pen;
                float flash = 1f - Mathf.Clamp01(age / _flashLength);

                // Once written, the letter breathes, and very rarely stutters.
                float breath = 1f - _breathDepth * (0.5f + 0.5f * Mathf.Sin(t * _breathSpeed + i * 0.45f));
                float flicker = Hash(Mathf.Floor(t * 12f) + i * 17.3f) < _flickerChance ? 0.35f : 1f;

                Vector3 mid = (c.bottomLeft + c.topRight) * 0.5f;
                Vector2 world = _text.transform.TransformPoint(mid);
                float d = Vector2.Distance(world, _lens.Center) - radius;
                float inside = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-_rimSoftness, _rimSoftness, d));
                float inWorld = _voice == RiftWorld.Static ? inside : 1f - inside;

                float alpha = appear * inWorld * breath * flicker;
                Color32 col = Color32.Lerp(_baseColor, new Color32(255, 255, 255, 255), flash * 0.85f);
                col.a = (byte)(255f * alpha);

                int mat = c.materialReferenceIndex;
                Color32[] colors = info.meshInfo[mat].colors32;
                Vector3[] verts = info.meshInfo[mat].vertices;
                for (int v = 0; v < 4; v++)
                {
                    int k = c.vertexIndex + v;
                    colors[k] = col;
                    verts[k] = mid + (verts[k] - mid) * scale + Vector3.up * rise;
                }
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            UpdateCaret(info, lastWritten);
            UpdateCard(info.characterCount);
        }

        private void UpdateCard(int count)
        {
            if (_card == null || count == 0) return;
            float written = Mathf.Clamp01(_typed / count);
            float d = Vector2.Distance(_card.transform.position, _lens.Center) - _lens.Radius;
            float inside = d < 0f ? 1f : 0f;
            float inWorld = _voice == RiftWorld.Static ? inside : 1f - inside;
            Color k = _baseColor;
            k.a = Mathf.MoveTowards(_card.color.a, _cardAlpha * written * inWorld, Time.deltaTime * 2f);
            _card.color = k;
        }

        private void UpdateCaret(TMP_TextInfo info, int lastWritten)
        {
            if (_caret == null) return;

            bool writing = _started && lastWritten >= 0 && _typed < info.characterCount + _fadeWidth;
            _caret.gameObject.SetActive(writing && Mathf.Repeat(Time.time * 2.5f, 1f) < 0.6f);
            if (!_caret.gameObject.activeSelf) return;

            TMP_CharacterInfo c = info.characterInfo[lastWritten];
            _caret.localPosition = new Vector3(c.topRight.x + 0.08f, (c.baseLine + c.ascender) * 0.5f, 0f);
            _caret.sizeDelta = new Vector2(0.06f, (c.ascender - c.descender) * 0.8f);
        }

        private static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f) * 43758.5453f, 1f);
    }
}
