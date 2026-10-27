using System.Collections;
using Rift.Levels;
using UnityEngine;

namespace Rift.Mindsets
{
    /// <summary>
    /// When the player steps into the rift, the game makes the choice: the healthy world grows
    /// until it fills everything, and all of its words become readable.
    /// </summary>
    public class RiftResolution : MonoBehaviour
    {
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private Goal _goal;
        [SerializeField] private RiftLens _lens;
        [SerializeField] private WallText[] _texts;
        [SerializeField] private float _duration = 2.2f;
        [SerializeField] private float _finalScale = 30f;

        public bool IsResolving { get; private set; }

        private void OnEnable() => _goal.Reached += OnReached;
        private void OnDisable() => _goal.Reached -= OnReached;

        private void OnReached()
        {
            IsResolving = true;
            foreach (WallText t in _texts)
                if (t.Voice == _theme.healthyWorld) t.RevealAll();

            // Static is real inside the lens, so a huge lens means the Static world everywhere;
            // a healthy Ember world is reached by shrinking the lens to nothing instead.
            StartCoroutine(_theme.healthyWorld == RiftWorld.Static ? Scale(1f, _finalScale) : Scale(1f, 0f));
        }

        private IEnumerator Scale(float from, float to)
        {
            for (float t = 0f; t < _duration; t += Time.unscaledDeltaTime)
            {
                float k = t / _duration;
                _lens.SetRadiusScale(Mathf.Lerp(from, to, k * k * k));
                yield return null;
            }
            _lens.SetRadiusScale(to);
        }
    }
}
