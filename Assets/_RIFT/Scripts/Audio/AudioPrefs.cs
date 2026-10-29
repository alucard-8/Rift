using System;
using UnityEngine;

namespace Rift.Audio
{
    /// <summary>Player's music / sound-effect mute switches, saved between sessions.</summary>
    public static class AudioPrefs
    {
        private const string MusicKey = "rift.music";
        private const string SfxKey = "rift.sfx";

        public static event Action Changed;

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set { PlayerPrefs.SetInt(SfxKey, value ? 1 : 0); Changed?.Invoke(); }
        }
    }
}
