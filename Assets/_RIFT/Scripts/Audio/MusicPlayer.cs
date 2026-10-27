using UnityEngine;

namespace Rift.Audio
{
    public enum MusicTrack
    {
        Title,
        Level
    }

    /// <summary>
    /// Soundtrack that survives scene loads. Each scene carries one saying which track it wants:
    /// the same track keeps playing untouched, a different one crossfades in.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private SoundBank _bank;
        [SerializeField] private MusicTrack _track = MusicTrack.Level;
        [SerializeField] private float _fadeTime = 2f;

        private static MusicPlayer _active;
        private AudioSource _source;
        private AudioClip _pending;

        public AudioSource Source => _source;

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                _active.Request(_bank.ClipFor(_track));
                Destroy(gameObject);
                return;
            }

            _active = this;
            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.clip = _bank.ClipFor(_track);
            _source.Play();
        }

        private void OnDestroy()
        {
            if (_active == this) _active = null;
        }

        /// <summary>Switch to another track: fade out, swap, fade back in. Same track: nothing happens.</summary>
        private void Request(AudioClip clip)
        {
            if (clip == _source.clip) { _pending = null; return; }
            _pending = clip;
        }

        private void Update()
        {
            float step = _bank.musicVolume / _fadeTime * Time.unscaledDeltaTime;

            if (_pending != null)
            {
                _source.volume = Mathf.MoveTowards(_source.volume, 0f, step);
                if (_source.volume > 0f) return;

                _source.clip = _pending;
                _pending = null;
                _source.Play();
            }

            _source.volume = Mathf.MoveTowards(_source.volume, _bank.musicVolume, step);
        }
    }
}
