using System.Collections;
using Rift.Extras;
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
        private TMP_Text _soulsValue;
        private TMP_Text _hiddenValue;

        private void Awake()
        {
            _dividerWidth = _divider.sizeDelta.x;
            _panel.alpha = 0f;
            _panel.blocksRaycasts = false;
            AddSoulStats();
        }

        /// <summary>Two more stats, cloned from the third; the row re-centres to fit five.</summary>
        private void AddSoulStats()
        {
            CanvasGroup last = _statBlocks[2];
            var a = (RectTransform)_statBlocks[1].transform;
            var b = (RectTransform)last.transform;
            Vector2 step = b.anchoredPosition - a.anchoredPosition;

            var blocks = new CanvasGroup[5];
            System.Array.Copy(_statBlocks, blocks, 3);
            blocks[3] = Instantiate(last, last.transform.parent);
            blocks[4] = Instantiate(last, last.transform.parent);
            for (int i = 3; i < 5; i++)
                ((RectTransform)blocks[i].transform).anchoredPosition = b.anchoredPosition + step * (i - 2);
            if (last.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
                foreach (CanvasGroup g in blocks) ((RectTransform)g.transform).anchoredPosition -= step;
            _statBlocks = blocks;

            _soulsValue = SetupStat(blocks[3], "SOULS");
            _hiddenValue = SetupStat(blocks[4], "HIDDEN");
        }

        /// <summary>Renames a cloned block's label and returns its value text.</summary>
        private TMP_Text SetupStat(CanvasGroup block, string label)
        {
            TMP_Text value = null;
            string valuePath = _voicesValue.name;
            foreach (TMP_Text t in block.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.name == valuePath) value = t;
                else t.text = label;
            }
            return value;
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

            SoulTracker souls = SoulTracker.Instance;
            int found = souls != null ? souls.Collected : 0, secret = souls != null ? souls.HiddenCollected : 0;
            yield return CountUp(3, v => _soulsValue.text = $"{Mathf.RoundToInt(found * v)} / 3");
            yield return CountUp(4, v => _hiddenValue.text = $"{Mathf.RoundToInt(secret * v)} / {SoulTracker.HiddenTotal}");

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
