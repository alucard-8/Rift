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

        // A tool can resend the same command while a long check is still running; the editor doesn't tick between
        // the two, so a second run would start from the first run's end state. Same frame + same check = cached result.
        private static readonly Dictionary<string, (int frame, List<CheckResult> results)> Cache = new();

        private static List<CheckResult> Once(string key, Func<List<CheckResult>> run)
        {
            string id = key + "@" + SceneManager.GetActiveScene().name;
            if (EditorApplication.isPlaying && Cache.TryGetValue(id, out var hit) && hit.frame == Time.frameCount) return hit.results;
            List<CheckResult> results = run();
            if (EditorApplication.isPlaying) Cache[id] = (Time.frameCount, results);
            return results;
        }

        public static List<CheckResult> RunLevel() => Once("run", RunLevelUncached);

        private static List<CheckResult> RunLevelUncached()
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
            // Room 6: Static pad (lens on it) up to the ledge, hold the lens on the switch, run through the door.
            h.WalkUntil(1, 99.4f, 200); h.Step(5);
            h.Lens(101f, 0.4f); h.Step(3);
            h.WalkUntil(1, 100.6f, 60);
            for (int i = 0; i < 90; i++) { if (h.X > 106f) h.Motor.SetMove(0); else h.Motor.SetMove(1); h.Step(1); }
            h.Motor.SetMove(0); h.Step(20);
            h.Check("Room 6 bounce pad onto the ledge", h.Y > 7.2f);
            h.Lens(107.5f, 8.6f); h.Step(45);
            h.WalkUntil(1, 123f, 260);
            h.Check("Room 6 switch held, through the door", h.X > 120f);
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
            h.Lens(40f, 0.6f); h.Step(3); h.Climb(1, 38.5f); h.Check("Shaft ledge 1", h.Y > -0.4f);
            h.Climb(1, 40.1f); h.Check("Shaft ledge 2", h.Y > 2.0f);
            h.Lens(40f, 3.0f); h.Step(3); h.Climb(-1, 39.9f); h.Check("Shaft ledge 3", h.Y > 4.4f);
            h.Lens(40f, 5.4f); h.Step(3); h.Climb(1, 40.1f); h.Check("Shaft ledge 4", h.Y > 6.8f);
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
            // Room 6: hop across crumbling stones before they fall, ride the Static mover with the lens on it.
            h.WalkUntil(1, 89.6f, 120); h.Step(5);
            h.Hop(1, 92.6f); h.Hop(1, 96.6f); h.Hop(1, 100.6f);
            h.Check("Crumbling stones", h.Y > 21.2f);
            var mover = UnityEngine.Object.FindAnyObjectByType<Rift.Gadgets.MovingPlatform>().GetComponent<Rigidbody2D>();
            for (int i = 0; i < 600 && mover.position.x > 105.4f; i++) { h.Lens(mover.position.x, mover.position.y); h.Step(1); }
            h.Motor.PressJump(); h.Motor.SetMove(1);
            for (int i = 0; i < 70; i++) { h.Lens(mover.position.x, mover.position.y); h.Step(1); if (h.X > mover.position.x - 0.2f) h.Motor.SetMove(0); }
            h.Motor.ReleaseJump(); h.Motor.SetMove(0);
            for (int i = 0; i < 600 && mover.position.x < 112.6f; i++) { h.Lens(mover.position.x, mover.position.y); h.Step(1); }
            h.Check("Ride the Static mover", h.Y > 21.2f && h.X > 110f);
            h.Hop(1, 116.5f); h.Lens(0, 60); h.Step(10);
            h.Check("Final ledge", h.X > 115f && h.Y > 21.2f);
            h.Walk(1, 80);
            h.Check("Goal reached", h.Run.IsComplete);
            return h.Finish();
        }

        /// <summary>Double jump, dash and wall jump behave as configured. Run on a fresh play session.</summary>
        public static List<CheckResult> Movement() => Once("Movement", MovementUncached);

        private static List<CheckResult> MovementUncached()
        {
            if (!EditorApplication.isPlaying) return Fail("Movement", "Enter play mode in a level first.");
            var h = new Harness();
            var cfg = h.Motor.Config;
            Vector2 start = h.Motor.transform.position;
            h.Lens(0, 60); h.Step(20);

            float y0 = h.Y, peak = y0;
            h.Motor.PressJump(); for (int i = 0; i < 50; i++) { h.Step(1); peak = Mathf.Max(peak, h.Y); } h.Motor.ReleaseJump(); h.Step(30);
            float single = peak - y0;
            h.Check($"Single jump ≈ {cfg.jumpHeight:F1}", Mathf.Abs(single - cfg.jumpHeight) < 0.6f);

            y0 = h.Y; peak = y0;
            h.Motor.PressJump(); h.Step(14); h.Motor.ReleaseJump(); h.Motor.PressJump();
            for (int i = 0; i < 60; i++) { h.Step(1); peak = Mathf.Max(peak, h.Y); } h.Motor.ReleaseJump(); h.Step(40);
            h.Check($"Double jump goes higher ({peak - y0:F1} > {single:F1})", !cfg.allowDoubleJump || peak - y0 > single + 0.5f);

            float x0 = h.X; h.Motor.SetMove(1); h.Step(1); h.Motor.SetMove(0); R.Set(h.Motor, "_nextDash", 0f); h.Motor.PressDash(); h.Step(Mathf.RoundToInt(cfg.dashTime / 0.02f)); R.Set(h.Motor, "_dashEnd", float.NegativeInfinity); h.Step(2);
            h.Check($"Dash covers distance ({Mathf.Abs(h.X - x0):F1} units)", !cfg.allowDash || Mathf.Abs(h.X - x0) > 1.8f);
            h.Step(30);

            RiftSolid wall = h.Solids.FirstOrDefault(s => s.name == "Left Wall");
            if (wall != null && cfg.allowWallJump)
            {
                h.Tp(wall.Bounds.max.x + 0.31f, wall.Bounds.center.y + 1.5f);
                h.Motor.SetMove(-1); h.Step(20);
                bool sliding = h.Motor.IsWallSliding;
                // Frozen harness clock keeps coyote time open forever; clear it so this is a real wall jump.
                R.Set(h.Motor, "_lastGroundedTime", float.NegativeInfinity);
                h.Motor.PressJump(); h.Step(3); h.Motor.ReleaseJump(); h.Motor.SetMove(0);
                bool kicked = h.Motor.Velocity.x > 2f;
                h.Check("Wall slide, then wall jump kicks away", sliding && kicked);
            }
            h.Tp(start.x, start.y); h.Step(10);
            return h.Finish();
        }

        /// <summary>Each gadget in the level does its job: pads launch, floors crumble and come back, switches open doors.</summary>
        public static List<CheckResult> Gadgets() => Once("Gadgets", GadgetsUncached);

        private static List<CheckResult> GadgetsUncached()
        {
            if (!EditorApplication.isPlaying) return Fail("Gadgets", "Enter play mode in a level first.");
            var h = new Harness();
            var results = new List<CheckResult>();
            foreach (var pad in UnityEngine.Object.FindObjectsByType<Rift.Gadgets.BouncePad>())
            {
                Vector2 at = pad.transform.position;
                h.Lens(at.x, at.y + 0.4f); h.Step(3); h.Tp(at.x, at.y + 1.2f);
                float y0 = h.Y, peak = y0; for (int i = 0; i < 80; i++) { h.Step(1); peak = Mathf.Max(peak, h.Y); }
                results.Add(new CheckResult { Passed = peak - y0 > 5f, Name = $"Bounce pad '{pad.name}' launches", Detail = $"rose {peak - y0:F1} units" });
            }
            foreach (var floor in UnityEngine.Object.FindObjectsByType<Rift.Gadgets.CrumblingFloor>())
            {
                h.Lens(0, 60); Bounds b = floor.GetComponent<RiftSolid>().Bounds; h.Tp(b.center.x, b.max.y + 0.6f);
                h.Step(45); bool fell = floor.Current == Rift.Gadgets.CrumblingFloor.State.Fallen;
                h.Step(170); bool back = floor.Current == Rift.Gadgets.CrumblingFloor.State.Solid;
                results.Add(new CheckResult { Passed = fell && back, Name = $"Crumbling floor '{floor.name}' falls then returns", Detail = $"fell={fell} back={back}" });
            }
            foreach (var door in UnityEngine.Object.FindObjectsByType<Rift.Gadgets.SwitchDoor>())
            {
                var sw = R.Get<Rift.Gadgets.LensSwitch>(door, "_switch");
                h.Lens(sw.transform.position.x, sw.transform.position.y); h.Step(110);
                results.Add(new CheckResult { Passed = sw.IsOn && door.IsOpen, Name = $"Lens switch opens '{door.name}'", Detail = $"on={sw.IsOn} open={door.IsOpen}" });
            }
            h.Finish();
            if (results.Count == 0) results.Add(new CheckResult { Passed = true, Name = "Gadgets", Detail = "no gadgets in this level" });
            return results;
        }

        public static List<CheckResult> GlitchCrush() => Once("GlitchCrush", GlitchCrushUncached);

        private static List<CheckResult> GlitchCrushUncached()
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
            private readonly MonoBehaviour[] _gadgets;
            private readonly RiftPresence[] _gadgetPresences;
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
                _gadgets = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Where(m => m.GetType().Namespace == "Rift.Gadgets").ToArray();
                _gadgetPresences = UnityEngine.Object.FindObjectsByType<RiftPresence>().Where(p => p.GetComponent<GlitchEnemy>() == null).ToArray();
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
                    foreach (RiftPresence p in _gadgetPresences) R.Call(p, "FixedUpdate");
                    foreach (MonoBehaviour g in _gadgets) if (g.isActiveAndEnabled) R.Call(g, "FixedUpdate");
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
                // Harness steps don't advance Time.time, so clear any dash/wall-jump timers it started.
                Motor.ResetMotion();
                _input.enabled = true;
                LensRef.SetOverride(null);
                return _results;
            }
        }
    }
}
