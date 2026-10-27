using Rift.UI;
using UnityEngine;

namespace Rift.Audio
{
    /// <summary>Title screen sounds: hover ticks and the start whoosh.</summary>
    public class MenuAudio : MonoBehaviour
    {
        [SerializeField] private SoundBank _bank;
        [SerializeField] private SfxPlayer _sfx;
        [SerializeField] private TitleMenu _menu;
        [SerializeField] private MenuButtonHover[] _buttons;

        private void OnEnable()
        {
            _menu.Started += OnStarted;
            foreach (MenuButtonHover b in _buttons) b.Hovered += OnHovered;
        }

        private void OnDisable()
        {
            _menu.Started -= OnStarted;
            foreach (MenuButtonHover b in _buttons) b.Hovered -= OnHovered;
        }

        private void OnHovered() => _sfx.Play(_bank.uiHover);
        private void OnStarted() => _sfx.Play(_bank.uiStart);
    }
}
