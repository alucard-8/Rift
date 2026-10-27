using UnityEngine;

namespace Rift.Audio
{
    /// <summary>
    /// Looping soundtrack that survives scene loads, so the music never restarts between title and level.
    /// Every scene carries one so it can be played on its own; extra copies remove themselves.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private SoundBank _bank;
        [SerializeField] private float _fadeInTime = 1.5f;

        private static MusicPlayer _active;
        private AudioSource _source;

        public AudioSource Source => _source;

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            _active = this;
            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
            _source.clip = _bank.music;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.Play();
        }

        private void OnDestroy()
        {
            if (_active == this) _active = null;
        }

        private void Update() =>
            _source.volume = Mathf.MoveTowards(_source.volume, _bank.musicVolume, _bank.musicVolume / _fadeInTime * Time.unscaledDeltaTime);
    }
}
