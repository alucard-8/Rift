using System.IO;
using Rift.Player;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rift.EditorTools
{
    /// <summary>
    /// Records short trailer clips (MP4, 1080p60) into RIFT/Recordings. Each shot opens a scene, plays it and
    /// drives the player and lens itself: the title gets a lens orbiting the logo, each level gets an
    /// auto-run with the lens riding just ahead. Survives the play-mode domain reload through SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class TrailerRecorder
    {
        private struct Shot { public string Scene; public string Name; public float Seconds; public bool Run; }

        private static readonly Shot[] Shots =
        {
            new Shot { Scene = "Title",   Name = "01_title",   Seconds = 8f,  Run = false },
            new Shot { Scene = "Level01", Name = "02_level01", Seconds = 12f, Run = true },
            new Shot { Scene = "Level02", Name = "03_level02", Seconds = 12f, Run = true },
            new Shot { Scene = "Level03", Name = "04_level03", Seconds = 12f, Run = true },
            new Shot { Scene = "Level04", Name = "05_level04", Seconds = 12f, Run = true },
        };

        private const string IndexKey = "rift.trailer.index";
        private static RecorderController _controller;
        private static double _startedAt;
        private static float _stuckSince;
        private static PlayerMotor _motor;
        private static RiftLens _lens;

        public static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        private static int Index { get => SessionState.GetInt(IndexKey, -1); set => SessionState.SetInt(IndexKey, value); }

        static TrailerRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += Tick;
        }

        [MenuItem("Window/RIFT/Record Trailer Clips")]
        public static void RecordAll()
        {
            Directory.CreateDirectory(Folder);
            Index = 0;
            BeginShot();
        }

        private static void BeginShot()
        {
            if (Index < 0 || Index >= Shots.Length) { Index = -1; Debug.Log($"[RIFT] Trailer clips saved to {Folder}"); return; }
            EditorSceneManager.OpenScene($"Assets/_RIFT/Scenes/{Shots[Index].Scene}.unity");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (Index < 0) return;
            if (change == PlayModeStateChange.EnteredPlayMode) StartRecording();
            if (change == PlayModeStateChange.EnteredEditMode) { Index++; EditorApplication.delayCall += BeginShot; }
        }

        private static void StartRecording()
        {
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = Shots[Index].Name;
            movie.Enabled = true;
            movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = Path.Combine(Folder, Shots[Index].Name);

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = 60f;

            _controller = new RecorderController(settings);
            _controller.PrepareRecording();
            _controller.StartRecording();
            _startedAt = EditorApplication.timeSinceStartup;
            _motor = Object.FindAnyObjectByType<PlayerMotor>();
            _lens = Object.FindAnyObjectByType<RiftLens>();
            if (_motor != null && Shots[Index].Run) _motor.GetComponent<PlayerInputReader>().enabled = false;
        }

        private static void Tick()
        {
            if (Index < 0 || _controller == null || !EditorApplication.isPlaying) return;
            float t = (float)(EditorApplication.timeSinceStartup - _startedAt);
            Shot shot = Shots[Index];

            if (shot.Run && _motor != null) Drive(t);
            else if (_lens != null)
            {
                // Orbit the lens over the logo so it glitches and reveals.
                Vector3 c = Camera.main.transform.position;
                _lens.SetOverride(new Vector2(c.x + Mathf.Sin(t * 0.9f) * 5f, c.y + 2.5f + Mathf.Sin(t * 1.7f) * 1.5f));
            }

            if (t < shot.Seconds) return;
            _controller.StopRecording();
            _controller = null;
            EditorApplication.isPlaying = false;
        }

        /// <summary>Run right with the lens just ahead; hop when blocked, and every so often anyway.</summary>
        private static void Drive(float t)
        {
            Vector2 p = _motor.transform.position;
            _lens.SetOverride(new Vector2(p.x + 1.6f + Mathf.Sin(t * 1.3f) * 0.4f, p.y + 0.7f));
            if (t < 1f) return;
            _motor.SetMove(1f);

            float vx = _motor.GetComponent<Rigidbody2D>().linearVelocity.x;
            if (vx > 1f) _stuckSince = t;
            bool hop = t - _stuckSince > 0.25f || Mathf.Repeat(t, 2.2f) < 0.02f;
            if (hop) _motor.PressJump();
            else if (Mathf.Repeat(t, 2.2f) > 0.5f) _motor.ReleaseJump();
        }
    }
}
