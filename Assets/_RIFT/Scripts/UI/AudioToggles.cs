using Rift.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rift.UI
{
    /// <summary>Two menu buttons that switch the music and the sound effects on or off.</summary>
    public class AudioToggles : MonoBehaviour
    {
        [SerializeField] private Button _music;
        [SerializeField] private Button _sfx;
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _sfxLabel;

        private void OnEnable()
        {
            _music.onClick.AddListener(ToggleMusic);
            _sfx.onClick.AddListener(ToggleSfx);
            Refresh();
        }

        private void OnDisable()
        {
            _music.onClick.RemoveListener(ToggleMusic);
            _sfx.onClick.RemoveListener(ToggleSfx);
        }

        private void ToggleMusic() { AudioPrefs.MusicOn = !AudioPrefs.MusicOn; Refresh(); }
        private void ToggleSfx() { AudioPrefs.SfxOn = !AudioPrefs.SfxOn; Refresh(); }

        private void Refresh()
        {
            _musicLabel.text = AudioPrefs.MusicOn ? "music  on" : "music  off";
            _sfxLabel.text = AudioPrefs.SfxOn ? "sound  on" : "sound  off";
        }
    }
}
