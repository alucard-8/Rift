using System;
using Rift.Core;
using Rift.Extras;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rift.UI
{
    /// <summary>Title screen: start the first level or quit. Mouse, keyboard or gamepad.</summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] private SceneFader _fader;
        [SerializeField] private Button _start;
        [SerializeField] private Button _quit;
        [SerializeField] private InputActionReference _submit;
        [SerializeField] private InputActionReference _cancel;
        [SerializeField] private int _firstLevelIndex = 1;

        private bool _started;

        public event Action Started;

        private TMP_Text _skinLabel;
        private float _skinFontSize;
        private float _skinWidth;

        private void Awake()
        {
            // The skin picker is the quit button's twin, under a small heading.
            Button skin = Instantiate(_quit, _quit.transform.parent);
            skin.name = "Skin";
            var rt = (RectTransform)skin.transform;
            rt.anchoredPosition -= new Vector2(0f, rt.rect.height * 1.7f);
            _skinLabel = skin.transform.Find("Label").GetComponent<TMP_Text>();
            _skinFontSize = _skinLabel.fontSize;
            _skinWidth = rt.rect.width - 24f;
            // Long cloak names ("< RIFTBORN >") must stay on one line, or they wrap
            // down into the SKIN heading below them.
            _skinLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // The clone's hover still points at the original quit label/underline; retarget it.
            MenuButtonHover hover = skin.GetComponent<MenuButtonHover>();
            if (hover != null)
                hover.Rebind(_skinLabel, (RectTransform)skin.transform.Find("Underline"));

            TMP_Text heading = Instantiate(_skinLabel, skin.transform);
            heading.name = "Heading";
            heading.text = "SKIN";
            heading.fontSize = _skinFontSize * 0.55f;
            heading.raycastTarget = false;
            heading.rectTransform.anchoredPosition += new Vector2(0f, rt.rect.height * 0.6f);
            skin.onClick.AddListener(() => { SoulSkins.Cycle(); RefreshSkin(); });
            RefreshSkin();
        }

        private void RefreshSkin()
        {
            string skinName = SoulSkins.All[SoulSkins.Selected].Name;
            _skinLabel.text = $"< {skinName} >";

            // Shrink only as much as needed so the name fits the button width in one line.
            _skinLabel.fontSize = _skinFontSize;
            float preferred = _skinLabel.GetPreferredValues(_skinLabel.text, 0f, 0f).x;
            if (preferred <= 0.01f) preferred = _skinLabel.text.Length * _skinFontSize * 0.6f;
            if (preferred > _skinWidth)
                _skinLabel.fontSize = Mathf.Max(18f, _skinFontSize * (_skinWidth / preferred));
        }

        private void OnEnable()
        {
            _start.onClick.AddListener(StartGame);
            _quit.onClick.AddListener(Quit);
            _submit.action.Enable();
            _cancel.action.Enable();
            _submit.action.performed += OnSubmit;
            _cancel.action.performed += OnCancel;
        }

        private void OnDisable()
        {
            _start.onClick.RemoveListener(StartGame);
            _quit.onClick.RemoveListener(Quit);
            _submit.action.performed -= OnSubmit;
            _cancel.action.performed -= OnCancel;
        }

        private void OnSubmit(InputAction.CallbackContext _) => StartGame();
        private void OnCancel(InputAction.CallbackContext _) => Quit();

        public void StartGame()
        {
            if (_started) return;
            _started = true;
            Started?.Invoke();
            _fader.FadeOutAndLoad(_firstLevelIndex);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
