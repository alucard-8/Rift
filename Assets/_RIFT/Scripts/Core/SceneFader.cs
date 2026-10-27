using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rift.Core
{
    /// <summary>Full-screen black fade: fades in when a scene starts, fades out before loading another.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SceneFader : MonoBehaviour
    {
        [SerializeField] private float _fadeInTime = 0.6f;
        [SerializeField] private float _fadeOutTime = 0.45f;

        private CanvasGroup _group;
        private bool _loading;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
        }

        private void Start() => StartCoroutine(Fade(1f, 0f, _fadeInTime));

        public void FadeOutAndLoad(int buildIndex)
        {
            if (_loading) return;
            _loading = true;
            StartCoroutine(FadeThenLoad(buildIndex));
        }

        public void ReloadCurrent() => FadeOutAndLoad(SceneManager.GetActiveScene().buildIndex);

        private IEnumerator FadeThenLoad(int buildIndex)
        {
            _group.blocksRaycasts = true;
            yield return Fade(_group.alpha, 1f, _fadeOutTime);
            Time.timeScale = 1f;
            SceneManager.LoadScene(buildIndex);
        }

        private IEnumerator Fade(float from, float to, float time)
        {
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
