using System.Collections;
using Rift.Audio;
using Rift.Core;
using Rift.Mindsets;
using Rift.Player;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// When a wall line starts writing, the world leans in to listen: time slows, the edges darken,
    /// the camera eases toward the words, the music dips and a soft chime plays. Once per line.
    /// Listens to WallText; the lines never know this exists.
    /// </summary>
    public class WhisperMoment : MonoBehaviour
    {
        [SerializeField] private WallText[] _lines;
        [SerializeField] private CameraFollow _camera;
        [SerializeField] private CanvasGroup _vignette;
        [SerializeField] private SfxPlayer _sfx;
        [SerializeField] private SoundBank _bank;
        [SerializeField] private PlayerSoulVisual _soul;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private PlayerRespawn _respawn;

        [Header("Feel")]
        [SerializeField] private float _duration = 1.6f;
        [SerializeField] private float _slowTimeScale = 0.6f;
        [SerializeField] private float _cameraLean = 0.3f;
        [SerializeField] private float _musicDuck = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _vignetteAlpha = 0.7f;

        private Coroutine _running;
        private MusicPlayer _music;
        private WallText _line;
        private float _t;

        /// <summary>Turn whispers off (admin panel); lines still write themselves.</summary>
        public static bool Enabled { get; set; } = true;
        public bool IsPlaying => _running != null;
        public int Count { get; private set; }

        private void OnEnable()
        {
            foreach (WallText line in _lines) line.BeganWriting += Play;
            _respawn.Died += Cancel;
        }

        private void OnDisable()
        {
            foreach (WallText line in _lines) if (line != null) line.BeganWriting -= Play;
            _respawn.Died -= Cancel;
            Cancel();
        }

        private void Start()
        {
            _music = FindAnyObjectByType<MusicPlayer>();
            _vignette.alpha = 0f;
        }

        public void Play(WallText line)
        {
            if (!Enabled) return;
            if (_running != null) StopCoroutine(_running);
            Count++;
            _line = line;
            _t = 0f;
            _sfx.Play(_bank.whisper);
            _soul.Pulse(_theme.TextColorFor(line.Voice));
            _running = StartCoroutine(Whisper());
        }

        private IEnumerator Whisper()
        {
            while (Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f))) yield return null;   // a hitch must not skip the moment
            Restore();
        }

        /// <summary>Advance the moment by dt real seconds. False once it's over.</summary>
        private bool Tick(float dt)
        {
            _t += dt;
            // Swell in fast, let go slowly.
            float k = Mathf.Min(Mathf.SmoothStep(0f, 1f, _t / 0.25f), Mathf.SmoothStep(0f, 1f, (_duration - _t) / (_duration * 0.6f)));
            if (Time.timeScale > 0.2f) Time.timeScale = Mathf.Lerp(1f, _slowTimeScale, k);   // hit-stop wins
            _vignette.alpha = _vignetteAlpha * k;
            _camera.SetAttention(_line.transform.position, _cameraLean * k);
            if (_music != null) _music.Duck = Mathf.Lerp(1f, _musicDuck, k);
            return _t < _duration;
        }

        private void Cancel()
        {
            if (_running == null) return;
            StopCoroutine(_running);
            Restore();
        }

        private void Restore()
        {
            _running = null;
            if (Time.timeScale > 0.2f) Time.timeScale = 1f;
            if (_vignette != null) _vignette.alpha = 0f;
            if (_camera != null) _camera.SetAttention(Vector2.zero, 0f);
            if (_music != null) _music.Duck = 1f;
        }
    }
}
