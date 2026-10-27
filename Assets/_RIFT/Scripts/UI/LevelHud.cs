using Rift.Levels;
using Rift.Mindsets;
using TMPro;
using UnityEngine;

namespace Rift.UI
{
    /// <summary>Run timer, death counter and the end-of-level panel. Only reads from LevelRun.</summary>
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private LevelRun _run;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private TMP_Text _deaths;
        [SerializeField] private CanvasGroup _endPanel;
        [SerializeField] private TMP_Text _endTitle;
        [SerializeField] private TMP_Text _endStats;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private float _panelFadeSpeed = 3f;

        private bool _showEnd;

        private void Awake()
        {
            _endPanel.alpha = 0f;
            _endPanel.blocksRaycasts = false;
        }

        private void OnEnable() => _run.Completed += OnCompleted;
        private void OnDisable() => _run.Completed -= OnCompleted;

        private void Update()
        {
            _timer.text = FormatTime(_run.Elapsed);
            _deaths.text = _run.Deaths == 1 ? "1 death" : $"{_run.Deaths} deaths";

            if (_showEnd) _endPanel.alpha = Mathf.MoveTowards(_endPanel.alpha, 1f, _panelFadeSpeed * Time.deltaTime);
        }

        private void OnCompleted(float time, int deaths)
        {
            _showEnd = true;
            string d = deaths == 1 ? "1 death" : $"{deaths} deaths";
            _endTitle.text = _theme.resolutionTitle;
            _endStats.text = $"<size=55%>{_theme.resolutionLine}</size>\n{FormatTime(time)}  ·  {d}\n<size=60%>[R] restart   ·   [esc] title</size>";
        }

        private static string FormatTime(float t) => $"{(int)(t / 60f):00}:{t % 60f:00.0}";
    }
}
