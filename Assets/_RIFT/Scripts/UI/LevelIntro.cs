using System.Collections;
using Rift.Mindsets;
using TMPro;
using UnityEngine;

namespace Rift.UI
{
    /// <summary>Title card when a level starts: the two clashing mindsets in their worlds' colours, then it fades.</summary>
    public class LevelIntro : MonoBehaviour
    {
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _clash;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _delay = 0.6f;
        [SerializeField] private float _fadeIn = 0.8f;
        [SerializeField] private float _hold = 2f;
        [SerializeField] private float _fadeOut = 1.2f;

        private void Start()
        {
            string ember = ColorUtility.ToHtmlStringRGB(Normalised(_theme.emberTextColor));
            string stat = ColorUtility.ToHtmlStringRGB(Normalised(_theme.staticTextColor));
            _clash.text = $"<color=#{ember}>{_theme.emberMindset}</color>   <alpha=#66>·</alpha>   <color=#{stat}>{_theme.staticMindset}</color>";
            _label.text = _theme.levelLabel;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            yield return new WaitForSecondsRealtime(_delay);
            for (float t = 0f; t < _fadeIn; t += Time.unscaledDeltaTime) { _group.alpha = t / _fadeIn; yield return null; }
            _group.alpha = 1f;
            yield return new WaitForSecondsRealtime(_hold);
            for (float t = 0f; t < _fadeOut; t += Time.unscaledDeltaTime) { _group.alpha = 1f - t / _fadeOut; yield return null; }
            _group.alpha = 0f;
        }

        private static Color Normalised(Color c)
        {
            float max = Mathf.Max(1f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
            return new Color(c.r / max, c.g / max, c.b / max, 1f);
        }
    }
}
