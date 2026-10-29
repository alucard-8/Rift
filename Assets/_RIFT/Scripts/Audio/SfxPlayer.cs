using UnityEngine;

namespace Rift.Audio
{
    /// <summary>Small round-robin pool of 2D audio sources for one-shot effects.</summary>
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private int _voices = 8;

        private AudioSource[] _sources;
        private int _next;

        /// <summary>Name of the last clip played (handy for tests and debugging).</summary>
        public string LastPlayed { get; private set; }

        private void Awake()
        {
            _sources = new AudioSource[_voices];
            for (int i = 0; i < _voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f;
            }
        }

        public void Play(Sound sound, float volumeScale = 1f)
        {
            if (sound == null || sound.clip == null || !AudioPrefs.SfxOn) return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.pitch = 1f + Random.Range(-sound.pitchJitter, sound.pitchJitter);
            source.PlayOneShot(sound.clip, sound.volume * volumeScale);
            LastPlayed = sound.clip.name;
        }
    }
}
