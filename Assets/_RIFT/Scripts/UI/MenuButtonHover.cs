using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rift.UI
{
    /// <summary>Text-button hover: brighten, nudge right and grow a thin underline.</summary>
    public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private RectTransform _underline;
        [SerializeField] private Color _idle = new(1f, 0.88f, 0.75f, 0.6f);
        [SerializeField] private Color _hover = new(1.4f, 1.25f, 1.05f, 1f);
        [SerializeField] private float _nudge = 14f;
        [SerializeField] private float _sharpness = 14f;

        private bool _hovered;
        private Vector2 _basePos;
        private float _underlineWidth;

        public event Action Hovered;

        private void Awake()
        {
            _basePos = _label.rectTransform.anchoredPosition;
            _underlineWidth = _underline.sizeDelta.x;
            _label.color = _idle;
            _underline.sizeDelta = new Vector2(0f, _underline.sizeDelta.y);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Hovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        private void Update()
        {
            float t = 1f - Mathf.Exp(-_sharpness * Time.unscaledDeltaTime);
            _label.color = Color.Lerp(_label.color, _hovered ? _hover : _idle, t);
            Vector2 target = _basePos + (_hovered ? Vector2.right * _nudge : Vector2.zero);
            _label.rectTransform.anchoredPosition = Vector2.Lerp(_label.rectTransform.anchoredPosition, target, t);
            float w = Mathf.Lerp(_underline.sizeDelta.x, _hovered ? _underlineWidth : 0f, t);
            _underline.sizeDelta = new Vector2(w, _underline.sizeDelta.y);
        }
    }
}
