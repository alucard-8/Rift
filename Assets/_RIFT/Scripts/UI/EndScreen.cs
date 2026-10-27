using System.Collections;
using Rift.Levels;
using Rift.Mindsets;
using TMPro;
using UnityEngine;

namespace Rift.UI
{
    /// <summary>
    /// The win screen. Plays a short reveal once the level is completed: veil, "the rift chose",
    /// the chosen mindset spreading out, its line writing itself, the stats counting up, then the prompts.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private LevelRun _run;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private WallText[] _voices;

        [Header("Hierarchy")]
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private CanvasGroup _kicker;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private RectTransform _divider;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private CanvasGroup[] _statBlocks;
        [SerializeField] private TMP_Text _timeValue;
        [SerializeField] private TMP_Text _deathsValue;
        [SerializeField] private TMP_Text _voicesValue;
        [SerializeField] private CanvasGroup _prompts;
        [Tooltip("Anything that must not show through the win screen (e.g. the level intro card).")]
        [SerializeField] private GameObject[] _hideOnReveal;

        [Header("Feel")]
        [SerializeField] private float _titleSpacingFrom = -10f;
        [SerializeField] private float _titleSpacingTo = 40f;
        [SerializeField] private float _lineCharsPerSecond = 40f;
        [SerializeField] private float _countTime = 0.7f;

        private float _dividerWidth;

        private void Awake()
        {
            _dividerWidth = _divider.sizeDelta.x;
            _panel.alpha = 0f;
            _panel.blocksRaycasts = false;
        }

        private void OnEnable() => _run.Completed += OnCompleted;
        private void OnDisable() => _run.Completed -= OnCompleted;

        private void OnCompleted(float time, int deaths) => StartCoroutine(Reveal(time, deaths));

        private IEnumerator Reveal(float time, int deaths)
        {
            foreach (GameObject g in _hideOnReveal) if (g != null) g.SetActive(false);

            // Reset every element to its hidden state.
            _kicker.alpha = 0f;
            _title.text = _theme.resolutionTitle;
            _title.alpha = 0f;
            _divider.sizeDelta = new Vector2(0f, _divider.sizeDelta.y);
            _line.text = _theme.resolutionLine;
            _line.maxVisibleCharacters = 0;
            foreach (CanvasGroup g in _statBlocks) g.alpha = 0f;
            _prompts.alpha = 0f;

            int heard = 0;
            foreach (WallText w in _voices) if (w != null && w.WasHeard) heard++;

            yield return Fade(_panel, 1f, 0.6f);
            yield return Fade(_kicker, 1f, 0.4f);

            // The chosen mindset spreads out as it appears; long words spread less so they stay on screen.
            float spreadTo = _titleSpacingTo * Mathf.Min(1f, 6f / Mathf.Max(1, _title.text.Length));
            for (float t = 0f; t < 1.1f; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / 1.1f);
                _title.alpha = k;
                _title.characterSpacing = Mathf.Lerp(_titleSpacingFrom, spreadTo, k);
                yield return null;
            }

            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _divider.sizeDelta = new Vector2(_dividerWidth * Ease(t / 0.5f), _divider.sizeDelta.y);
                yield return null;
            }
            _divider.sizeDelta = new Vector2(_dividerWidth, _divider.sizeDelta.y);

            for (float c = 0f; c < _line.text.Length; c += _lineCharsPerSecond * Time.unscaledDeltaTime)
            {
                _line.maxVisibleCharacters = Mathf.FloorToInt(c);
                yield return null;
            }
            _line.maxVisibleCharacters = _line.text.Length;

            yield return CountUp(0, v => _timeValue.text = LevelHud.FormatTime(time * v));
            yield return CountUp(1, v => _deathsValue.text = Mathf.RoundToInt(deaths * v).ToString());
            yield return CountUp(2, v => _voicesValue.text = $"{Mathf.RoundToInt(heard * v)} / {_voices.Length}");

            yield return Fade(_prompts, 1f, 0.5f);
            for (float t = 0f; ; t += Time.unscaledDeltaTime)
            {
                _prompts.alpha = 0.7f + 0.3f * Mathf.Cos(t * 2.2f);
                yield return null;
            }
        }

        private IEnumerator CountUp(int block, System.Action<float> write)
        {
            CanvasGroup g = _statBlocks[block];
            for (float t = 0f; t < _countTime; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / _countTime);
                g.alpha = Mathf.Clamp01(k * 2f);
                write(k);
                yield return null;
            }
            g.alpha = 1f;
            write(1f);
        }

        private static IEnumerator Fade(CanvasGroup g, float to, float time)
        {
            float from = g.alpha;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, Ease(t / time));
                yield return null;
            }
            g.alpha = to;
        }

        private static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return 1f - Mathf.Pow(1f - x, 3f);
        }
    }
}
