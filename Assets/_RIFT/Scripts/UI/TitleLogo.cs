using TMPro;
using UnityEngine;

namespace Rift.UI
{
    /// <summary>
    /// The title glitches when the rift touches it: a cyan/magenta copy of the logo
    /// appears behind the cream one, jittering sideways, stronger the closer the lens is.
    /// </summary>
    public class TitleLogo : MonoBehaviour
    {
        [SerializeField] private RiftLens _lens;
        [SerializeField] private TMP_Text _main;
        [SerializeField] private TMP_Text _glitchA;
        [SerializeField] private TMP_Text _glitchB;
        [Tooltip("How far beyond the lens rim the logo still reacts.")]
        [SerializeField] private float _reach = 1.5f;
        [SerializeField] private float _maxJitter = 0.25f;
        [SerializeField] private float _jitterRate = 14f;

        private Vector3 _baseA;
        private Vector3 _baseB;
        private float _nextJitter;
        private float _jitter;

        private void Awake()
        {
            _baseA = _glitchA.transform.localPosition;
            _baseB = _glitchB.transform.localPosition;
        }

        private void Update()
        {
            Bounds b = _main.bounds;
            b.center += _main.transform.position;
            Vector2 nearest = b.ClosestPoint(_lens.Center);
            float rimDistance = Vector2.Distance(nearest, _lens.Center) - _lens.Radius;
            float strength = 1f - Mathf.Clamp01(rimDistance / _reach);

            if (Time.unscaledTime >= _nextJitter)
            {
                _nextJitter = Time.unscaledTime + 1f / _jitterRate;
                _jitter = Random.Range(-1f, 1f) * (Random.value < 0.2f ? 1f : 0.3f);
            }

            float offset = _jitter * _maxJitter * strength;
            _glitchA.transform.localPosition = _baseA + new Vector3(offset + 0.06f * strength, 0f, 0f);
            _glitchB.transform.localPosition = _baseB + new Vector3(-offset - 0.06f * strength, 0f, 0f);
            _glitchA.alpha = strength;
            _glitchB.alpha = strength;
        }
    }
}
