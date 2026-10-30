using System.Collections;
using Rift.Audio;
using Rift.Fx;
using Rift.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rift.Extras
{
    /// <summary>
    /// A stranger you meet along the level. Walk up and press E: they say something about the place.
    /// Press E at them too many times and they eventually say something they shouldn't.
    /// </summary>
    public class TalkingNpc : MonoBehaviour
    {
        static readonly int LookId = Shader.PropertyToID("_Look");
        static readonly int TalkId = Shader.PropertyToID("_Talk");
        static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int FadeId = Shader.PropertyToID("_Fade");

        [Header("What they say")]
        [SerializeField] private string[] _lines;
        [Tooltip("Shown as the player keeps pestering them, before the secret.")]
        [SerializeField] private string[] _pesterLines;
        [SerializeField] private string _secret = "alucard chikour";

        [Header("Talking")]
        [SerializeField] private float _talkRadius = 3.2f;
        [SerializeField] private float _bubbleHold = 2.8f;
        [SerializeField] private int _spamLimit = 10;
        [Tooltip("Seconds the pester counter remembers. Spamming slower than this never triggers the secret.")]
        [SerializeField] private float _spamWindow = 7f;
        [SerializeField] private float _turnSpeed = 12f;

        [Header("Look")]
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Material _bubbleMaterial;
        [SerializeField] private SoundBank _bank;

        private Transform _player;
        private NpcBubble _bubble;
        private MaterialPropertyBlock _block;
        private int _spam;
        private float _spamStart;
        private float _talk;
        private float _glitch;
        private int _lastLine = -1;
        private float _facing = 1f;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_visual == null) _visual = _renderer != null ? _renderer.transform : transform;

            PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
            _player = motor != null ? motor.transform : null;

            _block = new MaterialPropertyBlock();

            var bubbleGo = new GameObject("Bubble", typeof(RectTransform));
            bubbleGo.transform.SetParent(transform, false);
            bubbleGo.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            _bubble = bubbleGo.AddComponent<NpcBubble>();
            _bubble.Initialize(_bubbleMaterial);
        }

        private void Update()
        {
            if (_player == null) return;

            float dt = Time.deltaTime;
            Vector2 toPlayer = _player.position - transform.position;

            // Always turn to watch the player; the eyes follow even when the body is still.
            float wanted = Mathf.Abs(toPlayer.x) > 0.05f ? Mathf.Sign(toPlayer.x) : _facing;
            _facing = Mathf.Lerp(_facing, wanted, 1f - Mathf.Exp(-_turnSpeed * dt));
            Vector3 scale = _visual.localScale;
            _visual.localScale = new Vector3(Mathf.Sign(_facing) * Mathf.Abs(scale.x), scale.y, scale.z);

            Vector2 look = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.zero;
            _talk = Mathf.MoveTowards(_talk, _bubble != null && _bubble.IsVisible ? 1f : 0f, dt * 5f);
            _glitch = Mathf.MoveTowards(_glitch, 0f, dt * 2.5f);

            _block.SetVector(LookId, new Vector4(look.x * Mathf.Sign(_facing), look.y, 0f, 0f));
            _block.SetFloat(TalkId, _talk);
            _block.SetFloat(GlitchId, _glitch);
            _block.SetFloat(FadeId, 1f);
            _renderer.SetPropertyBlock(_block);

            if (Vector2.Distance(_player.position, transform.position) > _talkRadius * 1.35f && _bubble.IsVisible)
                _bubble.Hide();
        }

        private void LateUpdate()
        {
            if (_player == null) return;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryTalk();
            if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) TryTalk();
        }

        private void TryTalk()
        {
            if (_player == null || Vector2.Distance(_player.position, transform.position) > _talkRadius) return;

            float now = Time.time;
            if (now - _spamStart > _spamWindow)
            {
                _spamStart = now;
                _spam = 0;
            }
            _spam++;

            string line;
            bool secret = false;

            if (_spam >= _spamLimit)
            {
                line = _secret;
                secret = true;
                _spam = 0;
                _spamStart = now;
                _glitch = 1f;
                transform.localPosition += Vector3.up * 0.02f;
                StartCoroutine(Settle());
            }
            else if (_spam >= Mathf.CeilToInt(_spamLimit * 0.65f) && _pesterLines != null && _pesterLines.Length > 0)
            {
                line = _pesterLines[Random.Range(0, _pesterLines.Length)];
            }
            else
            {
                line = PickLine();
            }

            _bubble.Show(line, secret ? 4.5f : _bubbleHold, secret);
            if (Random.value < 0.05f) StartCoroutine(RareSpin());

            SfxPlayer sfx = FindFirstObjectByType<SfxPlayer>();
            if (sfx != null && _bank != null)
            {
                sfx.Play(_bank.whisper, secret ? 0.35f : 0.22f);
                if (secret) sfx.Play(_bank.glitchMaterialize, 0.5f);
            }

            if (secret)
            {
                PlayerSoulVisual soul = FindFirstObjectByType<PlayerSoulVisual>();
                soul?.Pulse(Shader.GetGlobalColor("_RiftAccent"));
            }
        }

        private string PickLine()
        {
            if (_lines == null || _lines.Length == 0) return "...";
            if (_lines.Length == 1) return _lines[0];

            int i = Random.Range(0, _lines.Length - 1);
            if (i >= _lastLine) i++;
            _lastLine = i;
            return _lines[i];
        }

        public void PlayRareSpin() => StartCoroutine(RareSpin());

        // One talk in twenty, they spin once, as if something passed through them.
        private IEnumerator RareSpin()
        {
            _glitch = 1f;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                _visual.localRotation = Quaternion.Euler(0f, 0f, 360f * t / 0.6f);
                yield return null;
            }
            _visual.localRotation = Quaternion.identity;
        }

        private IEnumerator Settle()
        {
            Vector3 home = transform.localPosition;
            yield return new WaitForSeconds(0.25f);
            transform.localPosition = home - Vector3.up * 0.02f;
        }
    }
}
