using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rift.Extras
{
    /// <summary>
    /// A small world-space speech card above an NPC. Built at runtime so a level only needs
    /// the NPC object and the bubble material; shows text with a typewriter reveal.
    /// </summary>
    public class NpcBubble : MonoBehaviour
    {
        private TMP_Text _text;
        private CanvasGroup _group;
        private RectTransform _card;
        private Coroutine _running;
        private Color _baseColor = Color.white;

        public bool IsVisible => _group != null && _group.alpha > 0.01f;

        public void Initialize(Material material)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "World";
            canvas.sortingOrder = 60;
            canvas.worldCamera = Camera.main;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _card = (RectTransform)transform;
            _card.sizeDelta = new Vector2(560f, 168f);
            _card.localScale = Vector3.one * 0.0065f;

            var bg = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(560f, 168f);
            var image = bg.GetComponent<Image>();
            image.raycastTarget = false;
            if (material != null) image.material = material;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            textGo.transform.SetParent(transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(510f, 128f);

            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.font = TMP_Settings.defaultFontAsset;
            _text.fontSize = 46f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.raycastTarget = false;
            _text.color = Color.white;
            _text.text = "";
        }

        public void Show(string text, float holdSeconds, bool secret = false)
        {
            if (_text == null) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Play(text, holdSeconds, secret));
        }

        public void Hide()
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FadeOut(0.18f));
        }

        private IEnumerator Play(string text, float holdSeconds, bool secret)
        {
            Color accent = Shader.GetGlobalColor("_RiftAccent");
            if (accent.a <= 0f) accent = Color.white;
            _baseColor = secret ? Color.white : new Color(0.92f, 0.95f, 1f, 1f);

            _text.color = secret ? accent : _baseColor;
            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate();
            int total = _text.textInfo.characterCount;

            Vector3 home = transform.localPosition;
            float reveal = Mathf.Clamp(text.Length * 0.018f, 0.25f, 1.2f);

            for (float t = 0f; t < reveal; t += Time.deltaTime)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, 8f * Time.deltaTime);
                _text.maxVisibleCharacters = Mathf.RoundToInt(total * (t / reveal));
                if (secret) transform.localPosition = home + (Vector3)(Random.insideUnitCircle * 0.012f);
                yield return null;
            }
            _text.maxVisibleCharacters = total;
            _group.alpha = 1f;

            for (float t = 0f; t < holdSeconds; t += Time.deltaTime)
            {
                if (secret) transform.localPosition = home + (Vector3)(Random.insideUnitCircle * 0.01f);
                yield return null;
            }

            transform.localPosition = home;
            yield return FadeOut(0.22f);
            _running = null;
        }

        private IEnumerator FadeOut(float duration)
        {
            float start = _group.alpha;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                _group.alpha = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _group.alpha = 0f;
            _text.maxVisibleCharacters = 0;
            _running = null;
        }
    }
}
