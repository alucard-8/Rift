using System;
using UnityEngine;

namespace Rift.Audio
{
    /// <summary>A clip with its mix settings.</summary>
    [Serializable]
    public class Sound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Range(0f, 0.5f)] public float pitchJitter = 0.06f;
    }

    /// <summary>Every sound in the game, in one place. Swap a clip here and it changes everywhere.</summary>
    [CreateAssetMenu(menuName = "RIFT/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        [Header("Player")]
        public Sound jump;
        public Sound land;
        public Sound deathShatter;
        public Sound deathThump;
        public Sound respawn;

        [Header("World")]
        public Sound checkpoint;
        public Sound goalSeal;
        public Sound glitchMaterialize;
        public Sound glitchCrushed;

        [Header("Menu")]
        public Sound uiHover;
        public Sound uiStart;

        [Header("Loops")]
        public AudioClip lensHum;
        public AudioClip titleMusic;
        public AudioClip levelMusic;
        [Range(0f, 1f)] public float musicVolume = 0.45f;

        public AudioClip ClipFor(MusicTrack track) => track == MusicTrack.Title ? titleMusic : levelMusic;
    }
}
