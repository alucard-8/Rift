using System;
using System.Collections.Generic;
using System.Linq;
using Rift.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using A = Rift.EditorTools.RiftAdminActions;

namespace Rift.EditorTools
{
    /// <summary>
    /// Window ▸ RIFT ▸ Admin Panel. Buttons to jump around, poke every feature and run the automated checks.
    /// Layout only — what each button does lives in RiftAdminActions / RiftChecks.
    /// </summary>
    public class RiftAdminWindow : EditorWindow
    {
        private Vector2 _scroll;
        private bool _scenes = true, _teleport = true, _lens = true, _cheats, _showcase = true, _checks = true;
        private bool _god;
        private float _radius = 1f, _timeScale = 1f, _confidence = 0.5f;
        private readonly List<CheckResult> _results = new();
        private string _lastRun = "";

        [MenuItem("Window/RIFT/Admin Panel")]
        public static void Open() => GetWindow<RiftAdminWindow>("RIFT Admin").minSize = new Vector2(340, 400);

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        private void OnInspectorUpdate() => Repaint();

        private Dictionary<string, bool> _savedAbilities;

        private void RememberAbilities()
        {
            if (_savedAbilities != null) return;
            _savedAbilities = new Dictionary<string, bool>();
            foreach (string f in new[] { "allowDoubleJump", "allowDash", "allowWallJump" }) _savedAbilities[f] = A.Ability(f);
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode && _savedAbilities != null)
            {
                foreach (var kv in _savedAbilities) A.SetAbility(kv.Key, kv.Value);
                _savedAbilities = null;
            }
            _god = false; _radius = 1f; _timeScale = 1f;
        }

        private void OnGUI()
        {
            DrawStatus();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            Section(ref _scenes, "Scenes", () =>
            {
                Row(("Title", () => A.OpenScene("Title")), ("Level01", () => A.OpenScene("Level01")), ("Level02", () => A.OpenScene("Level02")), ("Sandbox", () => A.OpenScene("LensSandbox")));
                Row(("▶ Play from Title", A.PlayFromTitle), ("▶ Play this scene", () => EditorApplication.isPlaying = true));
            });

            Section(ref _teleport, "Teleport", () =>
            {
                if (!PlayingLevel()) return;
                var buttons = new List<(string, Action)> { ("Start", A.TeleportToStart) };
                Checkpoint[] cps = A.Checkpoints();
                for (int i = 0; i < cps.Length; i++) { Checkpoint c = cps[i]; buttons.Add(($"CP {i + 1}", () => A.TeleportToCheckpoint(c))); }
                buttons.Add(("Goal", A.TeleportToGoal));
                Row(buttons.ToArray());
            });

            Section(ref _lens, "Lens", () =>
            {
                if (!PlayingLevel()) return;
                Row(("Follow mouse", A.LensFollowMouse), ("Pin at player", A.LensAtPlayer), ("Pin centre", A.LensAtScreenCentre), ("Sweep", A.SweepLens));
                float r = EditorGUILayout.Slider("Radius scale", _radius, 0f, 4f);
                if (!Mathf.Approximately(r, _radius)) { _radius = r; A.LensRadius(r); }
            });

            Section(ref _cheats, "Cheats", () =>
            {
                if (!PlayingLevel()) return;
                bool god = EditorGUILayout.Toggle("God mode (no kill zones / glitch hits)", _god);
                if (god != _god) { _god = god; A.SetGodMode(god); }
                bool fly = EditorGUILayout.Toggle("Free fly (WASD in Game view)", A.FreeFly);
                if (fly != A.FreeFly) A.SetFreeFly(fly);
                float ts = EditorGUILayout.Slider("Time scale", _timeScale, 0.1f, 2f);
                if (!Mathf.Approximately(ts, _timeScale)) { _timeScale = ts; Time.timeScale = ts; }
            });

            Section(ref _showcase, "Feature showcase", () =>
            {
                if (!EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Enter play mode to try features.", MessageType.Info); return; }
                bool inLevel = A.Checkpoints().Length > 0;
                Feature("Audio", ("Jump sfx", () => A.PlaySound(b => b.jump)), ("Death sfx", () => A.PlaySound(b => b.deathShatter)), ("Checkpoint sfx", () => A.PlaySound(b => b.checkpoint)), ("Switch music", A.SwitchMusic));
                if (!inLevel) return;
                Feature("Lens", ("Sweep across screen", A.SweepLens));
                Feature("Player", ("Jump", A.Jump), ("Double jump", A.DoubleJump), ("Dash", A.Dash), ("Die / respawn", A.DieAndRespawn));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel, GUILayout.Width(110));
                foreach (var (label, field) in new[] { ("Double jump", "allowDoubleJump"), ("Dash", "allowDash"), ("Wall jump", "allowWallJump") })
                {
                    bool on = A.Ability(field);
                    bool next = GUILayout.Toggle(on, label, "Button");
                    if (next != on) { RememberAbilities(); A.SetAbility(field, next); }
                }
                EditorGUILayout.EndHorizontal();
                Feature("Checkpoint shard", ("Light next", A.LightNextCheckpoint), ("Light all", A.LightAllCheckpoints), ("Replay pulse", A.ReplayCheckpointPulse));
                Feature("Rift gate", ("Open", A.OpenGate), ("Reset", A.ResetGate));
                Feature("Glitches", ("Wake (lens on)", A.WakeGlitches), ("Crush nearest", A.CrushNearestGlitch));
                Feature("Wall text", ("Rewrite all", A.RewriteWallText), ("Reveal all", A.RevealWallText));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Soul", EditorStyles.boldLabel, GUILayout.Width(110));
                float c = EditorGUILayout.Slider(_confidence, 0f, 1f);
                if (!Mathf.Approximately(c, _confidence)) { _confidence = c; A.SoulConfidence(c); }
                if (GUILayout.Button("Glance", GUILayout.Width(70))) A.SoulGlance();
                EditorGUILayout.EndHorizontal();
                Feature("Ending", ("Resolve rift now", A.ResolveRiftNow), ("Show win screen", A.ShowWinScreen));
                Feature("Gadgets", ("Go to gadget room", A.TeleportToGadgets), ("Pad launch", A.LaunchFromPad), ("Crumble all", A.CrumbleAll), ("Lens on switch", A.HoldSwitches));
                Feature("Fx", ("Shake", A.Shake), ("Hit-stop", A.HitStop), ("Dust", A.DustBurst));
                Feature("Level flow", ("Continue", A.ContinueToNext), ("Restart", A.RestartLevel), ("Title", A.BackToTitle));
            });

            Section(ref _checks, "Automated checks", () =>
            {
                Row(("Movement", () => Report("Movement", RiftChecks.Movement())),
                    ("Level run-through", () => Report("Run-through", RiftChecks.RunLevel())),
                    ("Glitch crush", () => Report("Glitch crush", RiftChecks.GlitchCrush())),
                    ("Gadgets", () => Report("Gadgets", RiftChecks.Gadgets())));
                Row(("Text overlap", () => Report("Text overlap", RiftChecks.TextOverlap())),
                    ("Scene links", () => Report("Scene links", RiftChecks.SceneReferences())),
                    ("Run all", RunAll));
                if (_results.Count == 0) return;
                int passed = _results.Count(r => r.Passed);
                EditorGUILayout.LabelField($"{_lastRun}: {passed}/{_results.Count} passed", EditorStyles.boldLabel);
                foreach (CheckResult r in _results)
                {
                    GUI.color = r.Passed ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.55f);
                    EditorGUILayout.LabelField((r.Passed ? "✔ " : "✘ ") + r.Name, EditorStyles.wordWrappedLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField("     " + r.Detail, EditorStyles.wordWrappedMiniLabel);
                }
            });

            EditorGUILayout.EndScrollView();
        }

        private void RunAll()
        {
            var all = new List<CheckResult>();
            all.AddRange(RiftChecks.SceneReferences());
            all.AddRange(RiftChecks.TextOverlap());
            if (EditorApplication.isPlaying)
            {
                all.AddRange(RiftChecks.Movement());
                all.AddRange(RiftChecks.RunLevel());
                all.AddRange(RiftChecks.GlitchCrush());
            }
            Report("Run all" + (EditorApplication.isPlaying ? "" : " (edit mode: static checks only)"), all);
        }

        private void Report(string name, List<CheckResult> results)
        {
            _lastRun = $"{name} · {DateTime.Now:HH:mm:ss}";
            _results.Clear();
            _results.AddRange(results);
            foreach (CheckResult r in results)
                Debug.Log($"[RIFT check] {(r.Passed ? "PASS" : "FAIL")} {r.Name} — {r.Detail}");
        }

        private static void DrawStatus()
        {
            string scene = SceneManager.GetActiveScene().name;
            string mode = EditorApplication.isPlaying ? "▶ playing" : "editing";
            var player = UnityEngine.Object.FindAnyObjectByType<Player.PlayerMotor>();
            var lens = UnityEngine.Object.FindAnyObjectByType<RiftLens>();
            string pos = player != null ? $"player ({player.transform.position.x:F1}, {player.transform.position.y:F1})" : "no player";
            string ln = lens != null && EditorApplication.isPlaying ? $"lens ({lens.Center.x:F1}, {lens.Center.y:F1}) r={lens.Radius:F1}" : "";
            EditorGUILayout.HelpBox($"{scene} · {mode}\n{pos}   {ln}", MessageType.None);
        }

        private static bool PlayingLevel()
        {
            if (EditorApplication.isPlaying && UnityEngine.Object.FindAnyObjectByType<Player.PlayerMotor>() != null) return true;
            EditorGUILayout.HelpBox("Play a level (Level01 / Level02) to use this.", MessageType.Info);
            return false;
        }

        private static void Section(ref bool open, string title, Action body)
        {
            open = EditorGUILayout.BeginFoldoutHeaderGroup(open, title);
            if (open) { EditorGUI.indentLevel++; body(); EditorGUI.indentLevel--; }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);
        }

        private static void Row(params (string label, Action action)[] buttons)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var (label, action) in buttons)
                if (GUILayout.Button(label)) action();
            EditorGUILayout.EndHorizontal();
        }

        private static void Feature(string name, params (string label, Action action)[] buttons)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(110));
            foreach (var (label, action) in buttons)
                if (GUILayout.Button(label)) action();
            EditorGUILayout.EndHorizontal();
        }
    }
}
