using UnityEngine;

namespace Rift.Fx
{
    /// <summary>
    /// Sparks fly off wherever the lens rim cuts through a platform, coloured by the world being torn.
    /// More sparks the faster the lens moves.
    /// </summary>
    public class LensRimSparks : MonoBehaviour
    {
        [SerializeField] private RiftLens _lens;
        [SerializeField] private RiftSolid[] _solids;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private int _rimSamples = 64;
        [SerializeField] private float _idleRate = 14f;
        [Tooltip("Extra sparks per second per world unit/second of lens speed.")]
        [SerializeField] private float _speedRate = 4f;
        [SerializeField] private float _sparkSpeed = 2.5f;
        [SerializeField] private Color _emberColor = new(2.2f, 1.1f, 0.4f);
        [SerializeField] private Color _staticColor = new(0.4f, 2.0f, 2.6f);

        private readonly Vector2[] _hits = new Vector2[64];
        private readonly bool[] _hitStatic = new bool[64];
        private Vector2 _lastCenter;
        private float _budget;

        private void Start() => _lastCenter = _lens.Center;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float speed = (_lens.Center - _lastCenter).magnitude / dt;
            _lastCenter = _lens.Center;

            int count = CollectRimHits();
            if (count == 0) return;

            _budget += (_idleRate + speed * _speedRate) * dt;
            var emit = new ParticleSystem.EmitParams();
            while (_budget >= 1f)
            {
                _budget -= 1f;
                int i = Random.Range(0, count);
                Vector2 outward = (_hits[i] - _lens.Center).normalized;
                Vector2 tangent = new(-outward.y, outward.x);
                emit.position = _hits[i];
                emit.velocity = (outward * Random.Range(0.3f, 1f) + tangent * Random.Range(-0.8f, 0.8f)) * _sparkSpeed;
                emit.startColor = _hitStatic[i] ? _staticColor : _emberColor;
                _particles.Emit(emit, 1);
            }
        }

        private int CollectRimHits()
        {
            int count = 0;
            int samples = Mathf.Min(_rimSamples, _hits.Length);
            float r = _lens.Radius;
            for (int s = 0; s < samples; s++)
            {
                float a = s * Mathf.PI * 2f / samples;
                Vector2 p = _lens.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                foreach (RiftSolid solid in _solids)
                {
                    Bounds b = solid.Bounds;
                    if (p.x < b.min.x || p.x > b.max.x || p.y < b.min.y || p.y > b.max.y) continue;
                    _hits[count] = p;
                    _hitStatic[count] = solid.World == RiftWorld.Static;
                    count++;
                    break;
                }
            }
            return count;
        }
    }
}
