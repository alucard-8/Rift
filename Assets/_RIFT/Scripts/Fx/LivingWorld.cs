using Rift.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rift.Fx
{
    /// <summary>
    /// The living background, dressed to each level's sky. Dusk hills: trees, windmills, deer, birds.
    /// Rainy shore: mooring posts, a lighthouse, boats, gulls. Burnt land: dead trees, smouldering ruins,
    /// torch-bearers, crows. Hollow void: floating rocks, drifters, paper lanterns.
    /// Very rarely something enormous passes. Fliers scatter when the player dies.
    /// Builds itself in any scene with a player; purely visual.
    /// </summary>
    public class LivingWorld : MonoBehaviour
    {
        const int Landmarks = 6, Walkers = 4, Fliers = 5;
        const float LandmarkParallax = 0.92f, WalkerParallax = 0.85f;

        private enum Biome { Hills, Shore, Burnt, Hollow }

        private struct Actor { public Transform t, spin; public SpriteRenderer lamp, wingA, wingB; public float x, y, speed, seed; }

        public static LivingWorld Instance { get; private set; }

        private readonly Actor[] _landmarks = new Actor[Landmarks], _walkers = new Actor[Walkers], _fliers = new Actor[Fliers];
        private Biome _biome;
        private Color _ink;
        private Camera _cam;
        private Sprite _square, _circle;
        private int _order, _layer;
        private Transform _beast;
        private float _beastX, _nextBeast;
        private float _scatter;
        private PlayerRespawn _respawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook() => SceneManager.sceneLoaded += (_, _) =>
        {
            if (FindFirstObjectByType<PlayerMotor>() != null) new GameObject("Living World").AddComponent<LivingWorld>();
        };

        /// <summary>Sends the rare creature across right now.</summary>
        public void SummonBeast()
        {
            _beastX = -(_cam.orthographicSize * _cam.aspect + 18f);
            _beast.gameObject.SetActive(true);
            _nextBeast = Time.time + Random.Range(90f, 180f);
        }

        public void Scatter() => _scatter = 1f;

        private void Start()
        {
            Instance = this;
            _cam = Camera.main;
            if (_cam == null) { Destroy(gameObject); return; }

            _layer = SortingLayer.NameToID("Background");
            _order = 1;
            _square = MakeSprite(false);
            _circle = MakeSprite(true);

            switch (SceneManager.GetActiveScene().name)
            {
                case "Level02": _biome = Biome.Shore; _ink = new Color(0.05f, 0.07f, 0.1f); break;
                case "Level03": _biome = Biome.Burnt; _ink = new Color(0.03f, 0.02f, 0.02f); break;
                case "Level04": _biome = Biome.Hollow; _ink = new Color(0.04f, 0.05f, 0.09f); break;
                default: _biome = Biome.Hills; _ink = new Color(0.1f, 0.05f, 0.06f); break;
            }

            float h = _cam.orthographicSize, span = h * _cam.aspect * 2.4f;
            for (int i = 0; i < Landmarks; i++)
            {
                _landmarks[i] = MakeLandmark(i);
                _landmarks[i].x = span * i / Landmarks + Random.Range(-1f, 1f);
                _landmarks[i].seed = Random.value * 10f;
            }
            for (int i = 0; i < Walkers; i++)
            {
                _walkers[i] = MakeWalker();
                _walkers[i].x = Random.value * span;
                _walkers[i].seed = Random.value * 10f;
            }
            for (int i = 0; i < Fliers; i++)
            {
                _fliers[i] = MakeFlier();
                _fliers[i].x = Random.value * span;
                _fliers[i].y = Random.Range(0.25f, 0.9f) * h;
                _fliers[i].seed = Random.value * 10f;
            }

            MakeBeast();
            _nextBeast = Time.time + Random.Range(40f, 90f);

            _respawn = FindFirstObjectByType<PlayerRespawn>();
            if (_respawn != null) _respawn.Died += Scatter;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_respawn != null) _respawn.Died -= Scatter;
        }

        // ---------- what stands in each biome (roots sit at the base of the shape) ----------

        private Actor MakeLandmark(int i)
        {
            var a = new Actor { t = Root("Landmark") };
            switch (_biome)
            {
                case Biome.Hills:
                    Part(_circle, a.t, new Vector2(16f, 3.5f), new Vector2(0f, 0.2f), Ink(0.5f));
                    if (i % 3 == 0)
                    {
                        Part(_square, a.t, new Vector2(0.9f, 3.4f), new Vector2(0f, 2.8f), Ink(0.65f));
                        a.spin = Root("Sails", a.t, new Vector2(0f, 4.5f));
                        for (int s = 0; s < 4; s++)
                        {
                            Transform blade = Root("Sail", a.spin, Vector2.zero);
                            blade.localRotation = Quaternion.Euler(0f, 0f, s * 90f);
                            Part(_square, blade, new Vector2(0.28f, 1.9f), new Vector2(0f, 0.95f), Ink(0.65f));
                        }
                    }
                    else
                    {
                        Part(_square, a.t, new Vector2(0.22f, 1.5f), new Vector2(0f, 2.2f), Ink(0.65f));
                        Part(_circle, a.t, new Vector2(2.2f, 1.9f), new Vector2(0f, 3.5f), Ink(0.65f));
                        Part(_circle, a.t, new Vector2(1.5f, 1.3f), new Vector2(0.75f, 3f), Ink(0.65f));
                    }
                    break;

                case Biome.Shore:
                    Part(_square, a.t, new Vector2(14f, 0.5f), new Vector2(0f, 0.25f), Ink(0.55f));
                    if (i == 0)
                    {
                        Part(_square, a.t, new Vector2(0.9f, 5.5f), new Vector2(0f, 3f), Ink(0.7f));
                        a.lamp = Part(_circle, a.t, new Vector2(0.55f, 0.55f), new Vector2(0f, 6f), Color.clear);
                    }
                    else
                    {
                        Part(_square, a.t, new Vector2(0.18f, 1.6f), new Vector2(0f, 1.2f), Ink(0.7f));
                        a.lamp = Part(_circle, a.t, new Vector2(0.25f, 0.25f), new Vector2(0f, 2.1f), Color.clear);
                    }
                    break;

                case Biome.Burnt:
                    if (i % 2 == 0)
                    {
                        Part(_square, a.t, new Vector2(0.3f, 3.6f), new Vector2(0f, 1.8f), Ink(0.8f));
                        Limb(a.t, new Vector2(0.1f, 2.6f), 40f, 1.5f);
                        Limb(a.t, new Vector2(-0.1f, 3.1f), -50f, 1.2f);
                        Limb(a.t, new Vector2(0.05f, 1.8f), -35f, 1f);
                    }
                    else
                    {
                        Part(_square, a.t, new Vector2(1f, 3.2f), new Vector2(-1f, 1.6f), Ink(0.75f));
                        Part(_square, a.t, new Vector2(1f, 1.9f), new Vector2(0.2f, 0.95f), Ink(0.75f));
                        Part(_square, a.t, new Vector2(0.7f, 0.9f), new Vector2(1.2f, 0.45f), Ink(0.75f));
                        a.lamp = Part(_circle, a.t, new Vector2(0.5f, 0.3f), new Vector2(0.2f, 2f), Color.clear);
                    }
                    break;

                default:
                    Part(_circle, a.t, new Vector2(3.4f, 1f), Vector2.zero, Ink(0.7f));
                    Part(_circle, a.t, new Vector2(1.8f, 1.5f), new Vector2(0.1f, -0.45f), Ink(0.7f));
                    if (i % 2 == 0) a.lamp = Part(_circle, a.t, new Vector2(0.18f, 0.18f), new Vector2(0.3f, 0.6f), Color.clear);
                    a.y = Random.Range(2f, _cam.orthographicSize * 1.6f);
                    break;
            }
            return a;
        }

        private Actor MakeWalker()
        {
            var a = new Actor { t = Root("Walker") };
            float dir = Random.value < 0.5f ? -1f : 1f;
            a.speed = Random.Range(0.3f, 0.8f) * dir;
            switch (_biome)
            {
                case Biome.Hills: // a deer, grazing along
                    Part(_circle, a.t, new Vector2(1.1f, 0.5f), new Vector2(0f, 0.65f), Ink(0.75f));
                    Part(_square, a.t, new Vector2(0.09f, 0.5f), new Vector2(-0.35f, 0.25f), Ink(0.75f));
                    Part(_square, a.t, new Vector2(0.09f, 0.5f), new Vector2(0.35f, 0.25f), Ink(0.75f));
                    Limb(a.t, new Vector2(0.45f * dir, 0.75f), -35f * dir, 0.5f);
                    Part(_circle, a.t, new Vector2(0.3f, 0.22f), new Vector2(0.72f * dir, 1.15f), Ink(0.75f));
                    break;
                case Biome.Shore: // a small boat
                    Part(_square, a.t, new Vector2(1.5f, 0.3f), new Vector2(0f, 0.65f), Ink(0.8f));
                    Part(_square, a.t, new Vector2(0.06f, 1f), new Vector2(0f, 1.2f), Ink(0.8f));
                    a.lamp = Part(_circle, a.t, new Vector2(0.14f, 0.14f), new Vector2(0f, 1.75f), Color.clear);
                    a.speed *= 0.5f;
                    break;
                case Biome.Burnt: // someone fleeing with a torch
                    Part(_square, a.t, new Vector2(0.3f, 0.9f), new Vector2(0f, 0.45f), Ink(0.8f));
                    Part(_circle, a.t, new Vector2(0.28f, 0.28f), new Vector2(0.06f * dir, 1f), Ink(0.8f));
                    a.lamp = Part(_circle, a.t, new Vector2(0.2f, 0.28f), new Vector2(0.3f * dir, 1.05f), Color.clear);
                    break;
                default: // a lone drifter with a small light
                    Part(_circle, a.t, new Vector2(0.4f, 1f), new Vector2(0f, 0.5f), Ink(0.7f));
                    a.lamp = Part(_circle, a.t, new Vector2(0.12f, 0.12f), new Vector2(0f, 1.2f), Color.clear);
                    a.y = Random.Range(1f, _cam.orthographicSize);
                    a.speed *= 0.4f;
                    break;
            }
            return a;
        }

        private Actor MakeFlier()
        {
            var a = new Actor { t = Root("Flier"), speed = Random.Range(1.5f, 3f) };
            if (_biome == Biome.Hollow)
            {
                // Paper lanterns rising slowly; the "wings" are their two halves.
                a.speed = Random.Range(0.3f, 0.7f);
                a.wingA = Part(_circle, a.t, new Vector2(0.22f, 0.3f), Vector2.zero, new Color(1.5f, 0.9f, 0.5f, 0.7f));
                a.wingB = Part(_circle, a.t, new Vector2(0.08f, 0.12f), new Vector2(0f, -0.2f), new Color(1.5f, 0.9f, 0.5f, 0.4f));
                return a;
            }
            float span = _biome == Biome.Burnt ? 0.55f : 0.45f;
            a.wingA = Part(_square, a.t, new Vector2(span, 0.07f), new Vector2(-0.18f, 0f), Ink(0.85f));
            a.wingB = Part(_square, a.t, new Vector2(span, 0.07f), new Vector2(0.18f, 0f), Ink(0.85f));
            return a;
        }

        private void MakeBeast()
        {
            _beast = Root("Beast");
            Color eye = new Color(1.6f, 0.3f, 0.3f);
            switch (_biome)
            {
                case Biome.Hills: // a walking giant, head above the hills
                    Part(_circle, _beast, new Vector2(6f, 15f), Vector2.zero, Ink(0.8f));
                    Part(_circle, _beast, new Vector2(3f, 3f), new Vector2(0f, 8f), Ink(0.8f));
                    eye = new Color(1.8f, 1.4f, 0.6f);
                    Part(_circle, _beast, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 8.2f), eye);
                    break;
                case Biome.Shore: // a sea serpent's back breaking the water
                    for (int i = 0; i < 4; i++)
                        Part(_circle, _beast, new Vector2(5f, 6f - i), new Vector2(-i * 5.5f, -4f), Ink(0.8f));
                    Part(_circle, _beast, new Vector2(3.5f, 2.2f), new Vector2(4f, -0.6f), Ink(0.8f));
                    eye = new Color(0.6f, 1.8f, 1.6f);
                    Part(_circle, _beast, new Vector2(0.4f, 0.28f), new Vector2(4.8f, -0.3f), eye);
                    break;
                case Biome.Burnt: // a colossus of smoke with a burning core
                    Part(_circle, _beast, new Vector2(14f, 18f), new Vector2(0f, -1f), Ink(0.7f));
                    Part(_circle, _beast, new Vector2(9f, 8f), new Vector2(-3f, 6f), Ink(0.7f));
                    Part(_circle, _beast, new Vector2(1.4f, 1.4f), new Vector2(1f, 1f), new Color(2.2f, 0.7f, 0.2f, 0.8f));
                    break;
                default: // a leviathan swimming through the fog
                    Part(_circle, _beast, new Vector2(16f, 9f), Vector2.zero, Ink(0.75f));
                    Part(_circle, _beast, new Vector2(7f, 7f), new Vector2(6.5f, 2.5f), Ink(0.75f));
                    Part(_circle, _beast, new Vector2(0.5f, 0.32f), new Vector2(7.5f, 3f), eye);
                    Part(_circle, _beast, new Vector2(0.5f, 0.32f), new Vector2(5.7f, 3f), eye);
                    break;
            }
            _beast.gameObject.SetActive(false);
        }

        // ---------- motion ----------

        private void LateUpdate()
        {
            if (_cam == null) return;
            float dt = Time.deltaTime, h = _cam.orthographicSize, span = h * _cam.aspect * 2.4f;
            Vector2 c = _cam.transform.position;
            float floor = c.y - h;
            bool hollow = _biome == Biome.Hollow;
            _scatter = Mathf.MoveTowards(_scatter, 0f, dt * 0.5f);

            for (int i = 0; i < Landmarks; i++)
            {
                ref Actor a = ref _landmarks[i];
                float x = c.x - span / 2f + Mathf.Repeat(a.x - c.x * (1f - LandmarkParallax), span);
                float bob = hollow ? Mathf.Sin(Time.time * 0.35f + a.seed) * 0.4f : 0f;
                a.t.position = new Vector3(x, floor + a.y + bob, 0f);
                if (a.spin != null) a.spin.Rotate(0f, 0f, -18f * dt);
                if (a.lamp != null) a.lamp.color = LampColor(a.seed);
            }

            for (int i = 0; i < Walkers; i++)
            {
                ref Actor a = ref _walkers[i];
                a.x += a.speed * dt;
                float x = c.x - span / 2f + Mathf.Repeat(a.x - c.x * (1f - WalkerParallax), span);
                float wave = Mathf.Sin(Time.time * 2f + a.seed);
                a.t.position = new Vector3(x, floor + a.y + (_biome == Biome.Shore ? 0.1f + wave * 0.05f : Mathf.Abs(wave) * 0.04f), 0f);
                a.t.rotation = Quaternion.Euler(0f, 0f, wave * (_biome == Biome.Shore ? 3f : 1f));
                if (a.lamp != null) a.lamp.color = LampColor(a.seed);
            }

            for (int i = 0; i < Fliers; i++)
            {
                ref Actor a = ref _fliers[i];
                if (hollow)
                {
                    a.y += (a.speed * 0.4f + _scatter * 4f) * dt;
                    if (a.y > h * 1.1f) a.y = -h * 1.1f;
                    a.x += Mathf.Sin(Time.time * 0.4f + a.seed) * 0.15f * dt;
                }
                else
                {
                    a.x += a.speed * (1f + _scatter * 5f) * dt;
                    a.y += _scatter * 5f * dt;
                    if (_scatter <= 0f && a.y > h) a.y = Random.Range(0.25f, 0.9f) * h;
                    float flap = Mathf.Sin(Time.time * 9f + a.seed) * 28f;
                    a.wingA.transform.localRotation = Quaternion.Euler(0f, 0f, -flap);
                    a.wingB.transform.localRotation = Quaternion.Euler(0f, 0f, flap);
                }
                float x = c.x - span / 2f + Mathf.Repeat(a.x, span);
                a.t.position = new Vector3(x, c.y + a.y + Mathf.Sin(Time.time + a.seed) * 0.3f, 0f);
            }

            // Rare: something huge crosses behind the world, then leaves.
            if (_beast.gameObject.activeSelf)
            {
                _beastX += 1.6f * dt;
                _beast.position = new Vector3(c.x + _beastX, c.y - h * 0.15f + Mathf.Sin(Time.time * 0.4f), 0f);
                if (_beastX > h * _cam.aspect + 18f) _beast.gameObject.SetActive(false);
            }
            else if (Time.time >= _nextBeast) SummonBeast();
        }

        private Color LampColor(float seed)
        {
            float t = Time.time + seed;
            switch (_biome)
            {
                case Biome.Shore: return new Color(1.8f, 1.5f, 0.8f, 0.55f + 0.35f * Mathf.Sin(t * 0.7f));
                case Biome.Burnt: return new Color(2.2f, 0.7f, 0.2f, 0.55f + 0.3f * Mathf.PerlinNoise(t * 3f, seed));
                default: return new Color(0.7f, 1.2f, 1.6f, 0.45f + 0.3f * Mathf.Sin(t * 0.5f));
            }
        }

        // ---------- pieces ----------

        private Transform Root(string name) => Root(name, transform, Vector2.zero);

        private Transform Root(string name, Transform parent, Vector2 local)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }

        /// <summary>A thin bar growing from a point at an angle: branches, necks.</summary>
        private void Limb(Transform parent, Vector2 from, float degrees, float length)
        {
            Transform pivot = Root("Limb", parent, from);
            pivot.localRotation = Quaternion.Euler(0f, 0f, degrees);
            Part(_square, pivot, new Vector2(0.11f, length), new Vector2(0f, length / 2f), Ink(0.8f));
        }

        private SpriteRenderer Part(Sprite sprite, Transform parent, Vector2 size, Vector2 local, Color color)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingLayerID = _layer;
            r.sortingOrder = _order;
            return r;
        }

        private Color Ink(float alpha) => new Color(_ink.r, _ink.g, _ink.b, alpha);

        private static Sprite MakeSprite(bool round)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x - 15.5f, y - 15.5f).magnitude;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, !round || d < 15.5f ? 1f : 0f));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
