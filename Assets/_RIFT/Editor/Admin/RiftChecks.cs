using System;
using System.Collections.Generic;
using System.Linq;
using Rift.Enemies;
using Rift.Levels;
using Rift.Mindsets;
using Rift.Player;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using R = Rift.EditorTools.AdminReflection;

namespace Rift.EditorTools
{
    /// <summary>One line of a check report.</summary>
    public struct CheckResult
    {
        public bool Passed;
        public string Name;
        public string Detail;
    }

    /// <summary>
    /// Automated checks for the admin panel. Run-throughs drive the real gameplay components frame by frame
    /// (manual physics stepping), so they work even when the editor window is not focused.
    /// </summary>
    public static class RiftChecks
    {
        // ---------- run-throughs (play mode) ----------

        public static List<CheckResult> RunLevel()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (!EditorApplication.isPlaying) return Fail("Run-through", "Enter play mode in a level first.");
            return scene switch
            {
                "Level01" => RunLevel01(),
                "Level02" => RunLevel02(),
                _ => Fail("Run-through", $"No run-through for scene '{scene}'. Open Level01 or Level02.")
            };
        }

        private static List<CheckResult> RunLevel01()
        {
            var h = new Harness();
            h.Lens(0, 40); h.Step(10);
            h.Tp(16f, 0.6f); h.Step(10);
            h.CarryLens(32f, -0.3f);
            h.Check("Room 2 bridge (lens carried)", h.X > 32f);
            // The glitch buried in the wall: reveal it, pull the lens away so the rock crushes it, then cut through.
            h.Tp(36f, 0.6f); h.Step(10); h.Lens(38.95f, 1.4f); h.Step(4); h.Lens(0, 40); h.Step(4);
            h.Lens(38.95f, 1.2f); h.Step(3); h.Walk(1, 60); h.Lens(0, 40); h.Step(10);
            h.Check("Room 3 crush the wall glitch and cut through", h.X > 40f);
            h.Lens(52.5f, 2.8f); h.Step(5); h.Tp(48.8f, 0.6f); h.Step(10);
            h.Hop(1, 50.6f); h.Lens(53f, 3f); h.Step(3); h.Hop(1, 53.4f); h.Lens(54f, 3.6f); h.Step(3); h.Hop(1, 57.4f);
            h.Check("Room 4 climb static steps", h.Y > 6f);
            h.Tp(70f, 0.6f); h.Step(10); h.Lens(72.4f, 1.2f); h.WalkUntil(1, 73.6f, 60);
            // Keep the lens low so the bridge glitch above stays asleep.
            h.CarryLens(87f, -0.8f);
            h.Check("Room 5 wall + bridge (glitch left asleep)", h.X > 87f);
            h.Walk(1, 60);
            h.Check("Goal reached", h.Run.IsComplete);
            return h.Finish();
        }

        private static List<CheckResult> RunLevel02()
        {
            var h = new Harness();
            h.Lens(0, 40); h.Step(10);
            h.WalkUntil(1, 16f, 200); h.Step(15);
            h.Check("Room 1 descend", h.X > 15f && h.Y < -2f);
            h.CarryLens(33f, -3.3f);
            h.Check("Room 2 bridge", h.X > 32f);
            h.Tp(37.5f, -2.4f); h.Step(10);
            h.Lens(40f, 0.6f); h.Step(3); h.Climb(1, 38.8f); h.Check("Shaft ledge 1", h.Y > -0.4f);
            h.Climb(1, 40.3f); h.Check("Shaft ledge 2", h.Y > 2.0f);
            h.Lens(40f, 3.0f); h.Step(3); h.Climb(-1, 39.7f); h.Check("Shaft ledge 3", h.Y > 4.4f);
            h.Lens(40f, 5.4f); h.Step(3); h.Climb(1, 40.3f); h.Check("Shaft ledge 4", h.Y > 6.8f);
            h.Climb(1, 44.2f); h.Lens(0, 40); h.Step(10);
            h.Check("Shaft exit", h.X > 43.5f && h.Y > 9f);
            h.WalkUntil(1, 57.5f, 150); h.Step(10);
            h.Lens(58.5f, 10.8f); h.Step(4); h.Jump(30); h.Lens(0, 40); h.Step(30);
            h.Check("Cut the ceiling and climb onto it", h.Y > 11.5f);
            h.WalkUntil(1, 65.3f, 150); h.Step(15);
            for (int i = 0; i < 4; i++) h.StairHop(67.8f + i * 3f);
            h.Check("Gold stairs", h.Y > 18.1f);
            h.StairHop(80.5f); h.Lens(0, 40); h.Step(10);
            h.Check("Summit", h.Y > 19.2f);
            h.Walk(1, 80);
            h.Check("Goal reached", h.Run.IsComplete);
            return h.Finish();
        }

        public static List<CheckResult> GlitchCrush()
        {
            if (!EditorApplication.isPlaying) return Fail("Glitch crush", "Enter play mode in a level first.");
            var h = new Harness();
            var results = new List<CheckResult>();
            foreach (GlitchEnemy g in h.Glitches.Where(g => g.isActiveAndEnabled))
            {
                RiftSolid host = h.Solids.FirstOrDefault(s => s.World == RiftWorld.Ember && s.Bounds.Contains(new Vector3(g.Home.x, g.Home.y, s.Bounds.center.z)));
                if (host == null) continue;
                h.Lens(g.Home.x, g.Home.y); h.Step(5);
                bool woke = g.Presence.IsReal;
                h.Lens(0, 60); h.Step(4);
                results.Add(new CheckResult { Passed = woke && !g.IsAlive, Name = $"Glitch in '{host.name}' wakes then gets crushed", Detail = $"woke={woke} crushed={!g.IsAlive}" });
            }
            h.Finish();
            if (results.Count == 0) results.Add(new CheckResult { Passed = true, Name = "Glitch crush", Detail = "no glitch buried in rock in this level" });
            return results;
        }

        // ---------- static checks (edit or play mode) ----------

        public static List<CheckResult> TextOverlap()
        {
            var solids = UnityEngine.Object.FindObjectsByType<RiftSolid>()
                .Select(s => s.GetComponent<SpriteRenderer>().bounds).ToList();
            var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>()
                .Where(t => t.GetComponent<WallText>() != null || t.name.StartsWith("HintSign")).ToList();
            var bad = new List<string>();
            foreach (TMP_Text t in texts)
            {
                t.ForceMeshUpdate();
                Bounds b = t.textBounds;
                Vector3 c = t.transform.TransformPoint(b.center);
                Vector3 size = Vector3.Scale(b.size, t.transform.lossyScale);
                if (solids.Any(s => Mathf.Abs(c.x - s.center.x) < (size.x + s.size.x) * 0.5f + 0.2f &&
                                    Mathf.Abs(c.y - s.center.y) < (size.y + s.size.y) * 0.5f + 0.2f))
                    bad.Add(t.text.Replace("\n", " "));
            }
            return new List<CheckResult>
            {
                new() { Passed = bad.Count == 0, Name = $"World text clear of platforms ({texts.Count} texts)", Detail = bad.Count == 0 ? "0 overlaps" : string.Join(" | ", bad) }
            };
        }

        /// <summary>Fields that are allowed to be empty on purpose.</summary>
        private static readonly HashSet<string> OptionalFields = new() { "_lensEdge", "_caret", "_burst", "_hideOnReveal" };

        public static List<CheckResult> SceneReferences()
        {
            var empty = new List<string>();
            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.GetType().Namespace == null || !mb.GetType().Namespace.StartsWith("Rift")) continue;
                SerializedProperty p = new SerializedObject(mb).GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.name == "m_Script") continue;
                    if (p.objectReferenceValue == null && !OptionalFields.Contains(p.name.Split('.')[0]) && !OptionalFields.Contains(p.propertyPath.Split('.')[0]))
                        empty.Add($"{mb.gameObject.name}/{mb.GetType().Name}.{p.propertyPath}");
                }
            }
            return new List<CheckResult>
            {
                new() { Passed = empty.Count == 0, Name = "No empty links in scene components", Detail = empty.Count == 0 ? "all wired" : string.Join(" | ", empty.Take(8)) }
            };
        }

        private static List<CheckResult> Fail(string name, string why) =>
            new() { new CheckResult { Passed = false, Name = name, Detail = why } };

        // ---------- harness ----------

        /// <summary>Drives the player, lens, platforms and glitches with manual physics steps.</summary>
        private class Harness
        {
            public readonly PlayerMotor Motor;
            public readonly PlayerRespawn Respawn;
            public readonly RiftLens LensRef;
            public readonly LevelRun Run;
            public readonly RiftSolid[] Solids;
            public readonly GlitchEnemy[] Glitches;
            private readonly Rigidbody2D _body;
            private readonly PlayerInputReader _input;
            private readonly SimulationMode2D _previousMode;
            private readonly List<CheckResult> _results = new();

            public float X => Motor.transform.position.x;
            public float Y => Motor.transform.position.y;

            public Harness()
            {
                Motor = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
                Respawn = Motor.GetComponent<PlayerRespawn>();
                LensRef = UnityEngine.Object.FindAnyObjectByType<RiftLens>();
                Run = UnityEngine.Object.FindAnyObjectByType<LevelRun>();
                Solids = UnityEngine.Object.FindObjectsByType<RiftSolid>();
                Glitches = UnityEngine.Object.FindObjectsByType<GlitchEnemy>();
                _body = Motor.GetComponent<Rigidbody2D>();
                _input = Motor.GetComponent<PlayerInputReader>();
                _input.enabled = false;
                _previousMode = Physics2D.simulationMode;
                Physics2D.simulationMode = SimulationMode2D.Script;
            }

            public void Step(int frames)
            {
                for (int i = 0; i < frames; i++)
                {
                    R.Call(LensRef, "Update");
                    foreach (RiftSolid s in Solids) R.Call(s, "FixedUpdate");
                    foreach (GlitchEnemy g in Glitches)
                    {
                        if (!g.gameObject.activeInHierarchy) continue;
                        R.Call(g.Presence, "FixedUpdate");
                        R.Call(g, "FixedUpdate");
                    }
                    R.Call(Motor, "FixedUpdate");
                    R.Call(Respawn, "FixedUpdate");
                    Physics2D.Simulate(0.02f);
                    if (Run != null) R.Call(Run, "Update");
                }
            }

            public void Lens(float x, float y) => LensRef.SetOverride(new Vector2(x, y));

            public void Tp(float x, float y)
            {
                _body.position = new Vector2(x, y);
                Motor.transform.position = new Vector3(x, y, 0f);
                Motor.ResetMotion();
                Physics2D.SyncTransforms();
            }

            public void Walk(float dir, int frames) { Motor.SetMove(dir); Step(frames); Motor.SetMove(0); }

            public void WalkUntil(float dir, float x, int max)
            {
                Motor.SetMove(dir);
                for (int i = 0; i < max && (dir > 0 ? X < x : X > x); i++) Step(1);
                Motor.SetMove(0);
            }

            public void Jump(int frames) { Motor.PressJump(); Step(frames); Motor.ReleaseJump(); }

            public void CarryLens(float untilX, float lensY)
            {
                for (int i = 0; i < 250 && X < untilX; i++) { Lens(X + 1.2f, lensY); Motor.SetMove(1); Step(1); }
                Motor.SetMove(0); Lens(0, 60); Step(10);
            }

            public void Hop(float dir, float stopX)
            {
                Motor.PressJump(); Motor.SetMove(dir);
                for (int i = 0; i < 70; i++) { Step(1); if (dir > 0 ? X > stopX : X < stopX) Motor.SetMove(0); }
                Motor.ReleaseJump(); Motor.SetMove(0); Step(15);
            }

            /// <summary>Jump straight up first, steer near the top (how you climb between stacked ledges).</summary>
            public void Climb(float dir, float stopX)
            {
                Motor.PressJump(); Step(14); Motor.SetMove(dir);
                for (int i = 0; i < 60; i++) { Step(1); if (dir > 0 ? X > stopX : X < stopX) Motor.SetMove(0); }
                Motor.ReleaseJump(); Motor.SetMove(0); Step(15);
            }

            /// <summary>Hop forward with the lens riding just ahead of and above the player.</summary>
            public void StairHop(float stopX)
            {
                Motor.PressJump(); Motor.SetMove(1);
                for (int i = 0; i < 70; i++) { Lens(X + 1.2f, Y + 0.6f); Step(1); if (X > stopX) Motor.SetMove(0); }
                Motor.ReleaseJump(); Motor.SetMove(0); Step(10);
            }

            public void Check(string name, bool ok) =>
                _results.Add(new CheckResult { Passed = ok, Name = name, Detail = $"player ({X:F1}, {Y:F1}) · deaths {Run?.Deaths}" });

            public List<CheckResult> Finish()
            {
                Physics2D.simulationMode = _previousMode;
                _input.enabled = true;
                LensRef.SetOverride(null);
                return _results;
            }
        }
    }
}
