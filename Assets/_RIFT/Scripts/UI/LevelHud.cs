using Rift.Extras;
using Rift.Levels;
using TMPro;
using UnityEngine;

namespace Rift.UI
{
    /// <summary>In-run HUD: time, deaths and a checkpoint pip. Fades away when the level is won. Only reads.</summary>
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private LevelRun _run;
        [SerializeField] private CanvasGroup _hud;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private TMP_Text _deaths;
        [Tooltip("Souls collected this run, e.g. \"2 / 3\". Optional.")]
        [SerializeField] private TMP_Text _souls;
        [SerializeField] private Checkpoint[] _checkpoints;
        [SerializeField] private CanvasGroup _checkpointPip;
        [SerializeField] private float _pipHold = 1.6f;
        [SerializeField] private float _fadeSpeed = 2.5f;

        private bool _hidden;
        private float _pipTimer;

        public static string FormatTime(float t) => $"{(int)(t / 60f):00}:{t % 60f:00.0}";

        private void Awake() => _checkpointPip.alpha = 0f;

        private void OnEnable()
        {
            _run.Completed += OnCompleted;
            foreach (Checkpoint c in _checkpoints) c.Activated += OnCheckpoint;
        }

        private void OnDisable()
        {
            _run.Completed -= OnCompleted;
            foreach (Checkpoint c in _checkpoints) if (c != null) c.Activated -= OnCheckpoint;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _timer.text = FormatTime(_run.Elapsed);
            _deaths.text = _run.Deaths.ToString();

            if (_souls != null)
            {
                SoulTracker souls = SoulTracker.Instance;
                _souls.text = souls != null ? $"{souls.Collected} / {souls.Total}" : "0 / 3";
                if (Time.unscaledTime - SoulSkins.LastUnlockTime < 4f) _souls.text += $"   {SoulSkins.LastUnlock} CLOAK UNLOCKED";
                if (souls != null && souls.AllCollected)
                {
                    Color accent = Shader.GetGlobalColor("_RiftAccent");
                    _souls.color = accent.a > 0f ? accent : Color.white;
                }
            }

            _hud.alpha = Mathf.MoveTowards(_hud.alpha, _hidden ? 0f : 1f, _fadeSpeed * dt);

            _pipTimer -= dt;
            float pipTarget = _pipTimer > 0f ? 1f : 0f;
            _checkpointPip.alpha = Mathf.MoveTowards(_checkpointPip.alpha, pipTarget, 4f * dt);
        }

        private void OnCheckpoint() => _pipTimer = _pipHold;
        private void OnCompleted(float time, int deaths) => _hidden = true;
    }
}
