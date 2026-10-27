using Rift.Levels;
using UnityEngine;

namespace Rift.Fx
{
    /// <summary>Lights up a checkpoint's sprite when it activates, with a short flash.</summary>
    public class CheckpointGlow : MonoBehaviour
    {
        [SerializeField] private Checkpoint _checkpoint;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color _idle = new(0.45f, 0.25f, 0.3f);
        [SerializeField] private Color _lit = new(1f, 0.85f, 0.55f);
        [SerializeField] private float _flashScale = 1.6f;
        [SerializeField] private float _settleSharpness = 8f;
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private int _burstCount = 30;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = _sprite.transform.localScale;
            _sprite.color = _idle;
        }

        private void OnEnable() => _checkpoint.Activated += OnActivated;
        private void OnDisable() => _checkpoint.Activated -= OnActivated;

        private void OnActivated()
        {
            _sprite.color = _lit * 1.6f;
            _sprite.transform.localScale = _baseScale * _flashScale;
            if (_burst != null) _burst.Emit(_burstCount);
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-_settleSharpness * Time.deltaTime);
            _sprite.transform.localScale = Vector3.Lerp(_sprite.transform.localScale, _baseScale, t);
            if (_checkpoint.IsActive) _sprite.color = Color.Lerp(_sprite.color, _lit, t);
        }
    }
}
