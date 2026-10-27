using Rift.Mindsets;
using UnityEngine;
using UnityEngine.UI;

namespace Rift.UI
{
    /// <summary>
    /// A small glowing chevron at the screen edge that points toward the nearest line the player
    /// hasn't heard yet, while that line is off-screen. Hides once every nearby line is heard.
    /// </summary>
    public class UnreadLineMarker : MonoBehaviour
    {
        [SerializeField] private WallText[] _lines;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _player;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private Graphic _graphic;
        [SerializeField] private MindsetTheme _theme;
        [SerializeField] private float _range = 22f;
        [SerializeField] private float _edgePadding = 48f;

        private float _alpha;

        public WallText Target { get; private set; }

        private void LateUpdate()
        {
            Target = Nearest();
            Vector3 screen = Target != null ? _camera.WorldToScreenPoint(Target.transform.position) : Vector3.zero;
            bool offScreen = Target != null && (screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height);

            _alpha = Mathf.MoveTowards(_alpha, offScreen ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            if (Target != null)
            {
                Color c = _theme.TextColorFor(Target.Voice);
                float max = Mathf.Max(1f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
                c /= max;
                c.a = _alpha * (0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f));
                _graphic.color = c;
            }
            else _graphic.color = Color.clear;
            if (!offScreen) return;

            Vector2 centre = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = ((Vector2)screen - centre).normalized;
            Vector2 half = centre - Vector2.one * _edgePadding;
            float t = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(1e-4f, dir.x)), Mathf.Abs(half.y / Mathf.Max(1e-4f, dir.y)));
            _marker.position = centre + dir * t;
            _marker.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        private WallText Nearest()
        {
            WallText best = null;
            float bestD = _range;
            foreach (WallText line in _lines)
            {
                if (line == null || line.WasHeard) continue;
                float d = Vector2.Distance(line.transform.position, _player.position);
                if (d < bestD) { bestD = d; best = line; }
            }
            return best;
        }
    }
}
