using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Rift.Extras
{
    /// <summary>Soul cloaks, unlocked by the souls found across every run. Saved in PlayerPrefs.</summary>
    public static class SoulSkins
    {
        public readonly struct Skin
        {
            public readonly string Name;
            public readonly int Cost;
            public readonly Color Body, Rim, Eye;
            public Skin(string name, int cost, Color body, Color rim, Color eye) { Name = name; Cost = cost; Body = body; Rim = rim; Eye = eye; }
        }

        public static readonly Skin[] All =
        {
            new("HOLLOW", 0, new Color(0.07f, 0.045f, 0.11f), new Color(1.1f, 1f, 1.25f), new Color(1.8f, 1.7f, 1.5f)),
            new("EMBER", 4, new Color(0.16f, 0.04f, 0.02f), new Color(2.6f, 0.9f, 0.2f), new Color(3f, 1.8f, 0.5f)),
            new("STATIC", 8, new Color(0.01f, 0.07f, 0.12f), new Color(0.2f, 2.2f, 2.8f), new Color(0.6f, 3f, 3f)),
            new("GILDED", 12, new Color(0.12f, 0.08f, 0.01f), new Color(3f, 2.2f, 0.6f), new Color(3.2f, 3f, 2f)),
            new("RIFTBORN", 16, new Color(0.02f, 0.01f, 0.03f), Color.white, Color.white), // rim and eyes cycle hue
        };

        public const int RiftbornIndex = 4;
        const string TotalKey = "rift.souls.total", SkinKey = "rift.skin";

        public static int TotalSouls => PlayerPrefs.GetInt(TotalKey, 0);
        public static int Selected => Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, All.Length - 1);
        public static float LastUnlockTime { get; private set; } = float.NegativeInfinity;
        public static string LastUnlock { get; private set; }

        public static bool Unlocked(int i) => TotalSouls >= All[i].Cost;
        public static bool Found(string id) => PlayerPrefs.GetInt(id, 0) == 1;
        public static string IdFor(string soulName) => $"rift.soul.{SceneManager.GetActiveScene().name}.{soulName}";

        public static void Save(string id)
        {
            if (Found(id)) return;
            PlayerPrefs.SetInt(id, 1);
            PlayerPrefs.SetInt(TotalKey, TotalSouls + 1);
            PlayerPrefs.Save();
            foreach (Skin s in All)
                if (s.Cost == TotalSouls) { LastUnlock = s.Name; LastUnlockTime = Time.unscaledTime; }
        }

        /// <summary>Next unlocked cloak, wrapping round.</summary>
        public static void Cycle()
        {
            int i = Selected;
            do i = (i + 1) % All.Length; while (!Unlocked(i));
            PlayerPrefs.SetInt(SkinKey, i);
            PlayerPrefs.Save();
        }

        public static void Grant(int count) { PlayerPrefs.SetInt(TotalKey, count); PlayerPrefs.Save(); }

        /// <summary>Forgets every found soul in all levels, plus the cloak choice.</summary>
        public static void Reset()
        {
            for (int l = 1; l <= 4; l++)
                for (int s = 1; s <= 3; s++)
                    PlayerPrefs.DeleteKey($"rift.soul.Level0{l}.Soul {s}");
            for (int l = 1; l <= 4; l++)
                PlayerPrefs.DeleteKey($"rift.soul.Level0{l}.Hidden Soul");
            PlayerPrefs.DeleteKey(TotalKey);
            PlayerPrefs.DeleteKey(SkinKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Counts the souls collected in this level. One per level; the HUD reads it, nothing else needs to.
    /// </summary>
    public class SoulTracker : MonoBehaviour
    {
        [SerializeField] private int _total = 3;
        [SerializeField] private int _collected;

        public static SoulTracker Instance { get; private set; }

        public int Total => _total;
        public int Collected => _collected;
        public bool AllCollected => _collected >= _total;
        public int HiddenCollected { get; private set; }
        public const int HiddenTotal = 1;

        /// <summary>(collected, total)</summary>
        public event Action<int, int> Changed;

        private void Awake()
        {
            Instance = this;
            if (_total <= 0) _total = GetComponentsInChildren<SoulPickup>(true).Length;
            _collected = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void CollectHidden() => HiddenCollected = HiddenTotal;

        public void Collect()
        {
            if (AllCollected) return;
            _collected++;
            Changed?.Invoke(_collected, _total);
        }
    }
}
