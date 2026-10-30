using System.Collections;
using Rift.Player;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>A rock falling toward a telegraphed point. Breaks on the player or on the ground.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FallingDebris : MonoBehaviour
    {
        static readonly int FadeId = Shader.PropertyToID("_Fade");

        [SerializeField] private float _lifetime = 7f;

        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float _impactY = float.NegativeInfinity;
        private float _age;
        private bool _broken;
        private Vector3 _startScale;

        public void SetImpact(float y) => _impactY = y;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _block = new MaterialPropertyBlock();
            _startScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_broken) return;
            if (transform.position.y < _impactY - 0.2f || _age > _lifetime) Shatter();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_broken) return;
            if (other.GetComponentInParent<PlayerRespawn>() != null) other.GetComponentInParent<PlayerRespawn>().Respawn(DeathCause.Debris);
            Shatter();
        }

        private void Shatter()
        {
            if (_broken) return;
            _broken = true;
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(Break());
        }

        private IEnumerator Break()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float k = t / 0.3f;
                _block.SetFloat(FadeId, 1f - k);
                _renderer.SetPropertyBlock(_block);
                transform.localScale = _startScale * (1f + k * 0.7f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
