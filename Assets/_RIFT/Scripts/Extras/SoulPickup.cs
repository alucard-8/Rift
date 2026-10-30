using System.Collections;
using Rift.Audio;
using Rift.Fx;
using Rift.Player;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>
    /// A soul. Touch it and it bursts into light. Hidden ones exist only inside the lens.
    /// Found souls stay found across runs.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class SoulPickup : MonoBehaviour
    {
        static readonly int FadeId = Shader.PropertyToID("_Fade");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");

        [SerializeField] private float _bobHeight = 0.16f;
        [SerializeField] private float _bobSpeed = 2.2f;
        [SerializeField] private float _burstTime = 0.45f;
        public bool hidden;
        [SerializeField] private float _collectRadius = 1.1f;
        [SerializeField] private float _hintRange = 30f;
        [SerializeField] private SoundBank _bank;

        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Vector3 _home;
        private bool _taken;
        private RiftLens _lens;
        private float _reveal;
        private Transform _player;
        private SpriteRenderer _hint;
        private MaterialPropertyBlock _hintBlock;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _block = new MaterialPropertyBlock();
            _home = transform.position;
        }

        private void Start()
        {
            _lens = FindFirstObjectByType<RiftLens>();
            PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
            _player = motor != null ? motor.transform : null;
            if (!SoulSkins.Found(Id))
            {
                if (hidden && _player != null) MakeHint();
                return;
            }
            Count(SoulTracker.Instance != null ? SoulTracker.Instance : FindFirstObjectByType<SoulTracker>());
            Destroy(gameObject);
        }

        /// <summary>A small wisp that circles the player and leans toward this soul.</summary>
        private void MakeHint()
        {
            var go = new GameObject("Hidden Soul Hint");
            go.transform.localScale = transform.localScale * 0.45f;
            _hint = go.AddComponent<SpriteRenderer>();
            _hint.sprite = _renderer.sprite;
            _hint.sharedMaterial = _renderer.sharedMaterial;
            _hint.sortingLayerID = _renderer.sortingLayerID;
            _hint.sortingOrder = _renderer.sortingOrder;
            _hintBlock = new MaterialPropertyBlock();
        }

        private void UpdateHint()
        {
            Vector2 to = _home - _player.position;
            float closeness = 1f - Mathf.Clamp01(to.magnitude / _hintRange);
            _hint.transform.position = (Vector2)_player.position + to.normalized * 1.5f;
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * Mathf.Lerp(3f, 12f, closeness));
            _hintBlock.SetFloat(FadeId, closeness > 0f ? Mathf.Lerp(0.25f, 1f, closeness) * pulse : 0f);
            _hint.SetPropertyBlock(_hintBlock);
        }

        private string Id => SoulSkins.IdFor(name);

        private void Count(SoulTracker tracker)
        {
            if (tracker == null) return;
            if (hidden) tracker.CollectHidden();
            else tracker.Collect();
        }

        private void Update()
        {
            if (_taken) return;
            Vector3 p = _home;
            p.y += Mathf.Sin(Time.time * _bobSpeed + _home.x) * _bobHeight;
            transform.position = p;

            // A faint shimmer outside the lens is the only clue; inside it the soul blooms.
            bool inLens = !hidden || _lens == null || _lens.Contains(transform.position);
            _reveal = Mathf.MoveTowards(_reveal, inLens ? 1f : 0f, Time.deltaTime * 5f);
            float shimmer = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 0.9f + _home.x)), 24f) * 0.22f;
            _block.SetFloat(FadeId, Mathf.Max(_reveal, shimmer));
            _renderer.SetPropertyBlock(_block);

            if (_hint != null) UpdateHint();

            // Distance, not triggers: a resting player sleeps and would never re-fire when the lens arrives.
            if (_reveal > 0.5f && _player != null && Vector2.Distance(_player.position, transform.position) < _collectRadius) Take();
        }

        private void Take()
        {
            _taken = true;
            if (_hint != null) Destroy(_hint.gameObject);
            GetComponent<Collider2D>().enabled = false;

            SoulTracker tracker = SoulTracker.Instance != null ? SoulTracker.Instance : FindFirstObjectByType<SoulTracker>();
            Count(tracker);
            SoulSkins.Save(Id);

            Color accent = Shader.GetGlobalColor("_RiftAccent");
            if (accent.a <= 0f) accent = Color.white;

            PlayerSoulVisual soul = FindFirstObjectByType<PlayerSoulVisual>();
            soul?.Pulse(accent);

            SfxPlayer sfx = FindFirstObjectByType<SfxPlayer>();
            if (sfx != null && _bank != null) sfx.Play(_bank.checkpoint, 0.55f);

            StartCoroutine(Burst(accent));
        }

        private IEnumerator Burst(Color accent)
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < _burstTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _burstTime);
                transform.localScale = startScale * Mathf.Lerp(1f, 2.6f, k);
                _block.SetFloat(FadeId, 1f - k);
                _block.SetFloat(PulseId, t * 6f);
                _block.SetColor("_CoreColor", accent * 2.4f);
                _renderer.SetPropertyBlock(_block);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
