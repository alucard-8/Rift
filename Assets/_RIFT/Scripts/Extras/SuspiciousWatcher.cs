using System.Collections;
using Rift.Player;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>
    /// Someone who is always somewhere they shouldn't be. They appear further away — and stranger —
    /// as the player gets deeper into the level, watch for a while, then are gone.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SuspiciousWatcher : MonoBehaviour
    {
        static readonly int LookId = Shader.PropertyToID("_Look");
        static readonly int FadeId = Shader.PropertyToID("_Fade");
        static readonly int WatcherId = Shader.PropertyToID("_Watcher");
        static readonly int GlitchId = Shader.PropertyToID("_Glitch");

        [SerializeField] private Transform _goal;
        [Tooltip("Progress through the level (0..1) at which each sighting happens.")]
        [SerializeField] private float[] _at = { 0.16f, 0.34f, 0.55f, 0.76f };
        [SerializeField] private float _hold = 3.2f;
        [SerializeField] private float _fadeTime = 0.35f;

        private SpriteRenderer _renderer;
        private Transform _visual;
        private Transform _player;
        private MaterialPropertyBlock _block;
        private float _startDistance;
        private int _next;
        private bool _busy;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _visual = transform;
            _block = new MaterialPropertyBlock();
            _block.SetFloat(WatcherId, 1f);
            _block.SetFloat(FadeId, 0f);
            _renderer.SetPropertyBlock(_block);
            _renderer.enabled = false;

            var motor = FindFirstObjectByType<PlayerMotor>();
            _player = motor != null ? motor.transform : null;
            if (_goal != null && _player != null)
                _startDistance = Mathf.Max(1f, Vector2.Distance(_player.position, _goal.position));
        }

        private void Update()
        {
            if (_player == null || _goal == null || _busy || _next >= _at.Length) return;

            float progress = 1f - Mathf.Clamp01(Vector2.Distance(_player.position, _goal.position) / _startDistance);
            if (progress < _at[_next]) return;
            StartCoroutine(Sighting(_next));
            _next++;
        }

        private void LateUpdate()
        {
            if (_player == null) return;
            Vector2 to = _player.position - transform.position;
            _block.SetVector(LookId, new Vector4(to.normalized.x, to.normalized.y, 0f, 0f));
            _renderer.SetPropertyBlock(_block);
        }

        private IEnumerator Sighting(int index)
        {
            _busy = true;
            PlaceAt(index);
            _renderer.enabled = true;

            // Fade in.
            for (float t = 0f; t < _fadeTime; t += Time.deltaTime)
            {
                _block.SetFloat(FadeId, t / _fadeTime);
                yield return null;
            }
            _block.SetFloat(FadeId, 1f);

            // Watch. If the player comes close, they are already gone.
            for (float t = 0f; t < _hold; t += Time.deltaTime)
            {
                if (Vector2.Distance(_player.position, transform.position) < 3.5f) break;
                yield return null;
            }

            // Fade out.
            for (float t = 0f; t < _fadeTime; t += Time.deltaTime)
            {
                _block.SetFloat(FadeId, 1f - t / _fadeTime);
                _block.SetFloat(GlitchId, t / _fadeTime);
                yield return null;
            }
            _block.SetFloat(GlitchId, 0f);
            _renderer.enabled = false;
            _busy = false;
        }

        private void PlaceAt(int index)
        {
            Vector2 p;
            float scale = 1f;
            float rotation = 0f;

            switch (index)
            {
                case 0:
                    // Just behind, feet on the ground.
                    p = new Vector2(_player.position.x - 6.5f, _player.position.y);
                    p.y = GroundY(p.x, p.y + 8f) + 0.8f;
                    break;
                case 1:
                    // Above, hanging in the air.
                    p = new Vector2(_player.position.x + 4f, _player.position.y + 7.5f);
                    scale = 0.85f;
                    break;
                case 2:
                    // Far ahead, upside down over the goal.
                    p = new Vector2(_goal.position.x, _goal.position.y + 8.5f);
                    rotation = 180f;
                    scale = 0.75f;
                    break;
                default:
                    // Far behind, barely in frame.
                    p = new Vector2(_player.position.x - 15f, _player.position.y + 3.5f);
                    scale = 1.35f;
                    break;
            }

            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            _visual.localScale = new Vector3(0.95f * scale, 1.5f * scale, 1f);
        }

        private static float GroundY(float x, float fromY)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, 40f);
            float best = float.NegativeInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                best = Mathf.Max(best, hit.point.y);
            }
            return best > float.NegativeInfinity ? best : fromY - 5f;
        }
    }
}
