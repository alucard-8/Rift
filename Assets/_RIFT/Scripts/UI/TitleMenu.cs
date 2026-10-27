using Rift.Core;
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

        public void StartGame() => _fader.FadeOutAndLoad(_firstLevelIndex);

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
