using UnityEngine;

namespace Rift.Audio
{
    /// <summary>The lens hums: a quiet drone at rest that swells and rises in pitch as the lens moves.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class LensHum : MonoBehaviour
    {
        [SerializeField] private RiftLens _lens;
        [SerializeField] private SoundBank _bank;
        [SerializeField] private float _idleVolume = 0.12f;
        [SerializeField] private float _maxVolume = 0.45f;
        [SerializeField] private Vector2 _pitchRange = new(0.8f, 1.35f);
        [Tooltip("Lens speed (world units/s) that counts as 'moving fast'.")]
        [SerializeField] private float _fastSpeed = 25f;
        [SerializeField] private float _smoothing = 6f;

        private AudioSource _source;
        private Vector2 _lastCenter;
        private float _intensity;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.clip = _bank.lensHum;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = _idleVolume;
            _source.Play();
        }

        private void Start() => _lastCenter = _lens.Center;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            float speed = (_lens.Center - _lastCenter).magnitude / dt;
            _lastCenter = _lens.Center;

            float target = Mathf.Clamp01(speed / _fastSpeed);
            _intensity = Mathf.Lerp(_intensity, target, 1f - Mathf.Exp(-_smoothing * dt));
            _source.volume = AudioPrefs.SfxOn ? Mathf.Lerp(_idleVolume, _maxVolume, _intensity) : 0f;
            _source.pitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, _intensity);
        }
    }
}
