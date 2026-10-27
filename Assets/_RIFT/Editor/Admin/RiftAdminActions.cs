using System.Linq;
using Rift.Audio;
using Rift.Core;
using Rift.Enemies;
using Rift.Fx;
using Rift.Levels;
using Rift.Mindsets;
using Rift.Player;
using Rift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using R = Rift.EditorTools.AdminReflection;

namespace Rift.EditorTools
{
    /// <summary>
    /// Everything the admin panel's buttons do. One small method per action, grouped by feature.
    /// When a feature is added, its "try it" actions go here (see AGENTS.md → Admin panel).
    /// </summary>
    public static class RiftAdminActions
    {
        private const string Scenes = "Assets/_RIFT/Scenes/";

        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();
        static T[] FindAll<T>() where T : Object => Object.FindObjectsByType<T>();
        static PlayerMotor Player => Find<PlayerMotor>();
        static RiftLens Lens => Find<RiftLens>();

        // ---------- scenes ----------

        public static void OpenScene(string name)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Scenes + name + ".unity");
        }

        public static void PlayFromTitle()
        {
            OpenScene("Title");
            EditorApplication.isPlaying = true;
        }

        // ---------- teleport ----------

        public static Checkpoint[] Checkpoints() => FindAll<Checkpoint>().OrderBy(c => c.transform.position.x).ToArray();

        public static void TeleportTo(Vector3 position)
        {
            PlayerMotor p = Player;
            if (p == null) return;
            var body = p.GetComponent<Rigidbody2D>();
            body.position = position + Vector3.up * 0.6f;
            p.transform.position = position + Vector3.up * 0.6f;
            p.ResetMotion();
            Physics2D.SyncTransforms();
        }

        public static void TeleportToCheckpoint(Checkpoint c)
        {
            TeleportTo(c.transform.position);
            if (!c.IsActive) R.Call(c, "OnTriggerEnter2D", Player.GetComponent<Collider2D>());
        }

        public static void TeleportToStart()
        {
            var spawn = R.Get<Transform>(Player.GetComponent<PlayerRespawn>(), "_spawnPoint");
            if (spawn != null) TeleportTo(spawn.position - Vector3.up * 0.6f);
        }

        public static void TeleportToGoal()
        {
            Goal g = Find<Goal>();
            if (g != null) TeleportTo(g.transform.position + Vector3.left * 3f);
        }

        // ---------- lens ----------

        public static void LensFollowMouse() => Lens?.SetOverride(null);
        public static void LensAtPlayer() => Lens?.SetOverride(Player.transform.position + Vector3.right * 1.5f);
        public static void LensAtScreenCentre() => Lens?.SetOverride(Camera.main.transform.position);
        public static void LensRadius(float scale) => Lens?.SetRadiusScale(scale);

        private static float _sweepStart;
        public static void SweepLens()
        {
            _sweepStart = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= SweepTick;
            EditorApplication.update += SweepTick;
        }

        private static void SweepTick()
        {
            float t = (float)EditorApplication.timeSinceStartup - _sweepStart;
            Camera cam = Camera.main;
            if (Lens == null || cam == null || t > 2f) { EditorApplication.update -= SweepTick; LensFollowMouse(); return; }
            float halfW = cam.orthographicSize * cam.aspect;
            Vector3 c = cam.transform.position;
            Lens.SetOverride(new Vector2(c.x - halfW + 2f * halfW * (t / 2f), c.y + Mathf.Sin(t * 4f) * 1.5f));
        }

        // ---------- cheats ----------

        public static void SetGodMode(bool on)
        {
            foreach (KillZone k in FindAll<KillZone>()) k.GetComponent<Collider2D>().enabled = !on;
            foreach (GlitchEnemy g in FindAll<GlitchEnemy>())
            {
                Collider2D c = g.GetComponent<Collider2D>();
                R.Set(g.Presence, "_colliders", on ? new Collider2D[0] : new[] { c });
                c.enabled = !on && g.Presence.IsReal;
            }
        }

        private static bool _fly;
        public static bool FreeFly => _fly;
        public static void SetFreeFly(bool on)
        {
            _fly = on;
            PlayerMotor p = Player;
            if (p == null) return;
            p.enabled = !on;
            var body = p.GetComponent<Rigidbody2D>();
            body.gravityScale = on ? 0f : p.Config.baseGravityScale;
            EditorApplication.update -= FlyTick;
            if (on) EditorApplication.update += FlyTick;
        }

        private static void FlyTick()
        {
            PlayerMotor p = Player;
            Keyboard k = Keyboard.current;
            if (p == null || k == null || !EditorApplication.isPlaying) { EditorApplication.update -= FlyTick; _fly = false; return; }
            Vector2 dir = new((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            p.GetComponent<Rigidbody2D>().linearVelocity = dir * 12f;
        }

        // ---------- feature showcase ----------

        public static void Jump() { Player.PressJump(); EditorApplication.delayCall += () => Player?.ReleaseJump(); }
        public static void DieAndRespawn() => Player?.GetComponent<PlayerRespawn>().Respawn();
        public static void Dash() => Player?.PressDash();

        public static void DoubleJump()
        {
            Player.PressJump();
            EditorApplication.delayCall += () => { Player?.ReleaseJump(); EditorApplication.delayCall += () => { Player?.PressJump(); EditorApplication.delayCall += () => Player?.ReleaseJump(); }; };
        }

        // Ability toggles edit the shared PlayerConfig asset; the window restores them when play mode ends.
        public static bool Ability(string field) => Player != null && R.Get<bool>(Player.Config, field);
        public static void SetAbility(string field, bool on) { if (Player != null) R.Set(Player.Config, field, on); }

        public static void LightNextCheckpoint()
        {
            Checkpoint next = Checkpoints().FirstOrDefault(c => !c.IsActive);
            if (next != null) R.Call(next, "OnTriggerEnter2D", Player.GetComponent<Collider2D>());
        }

        public static void LightAllCheckpoints()
        {
            foreach (Checkpoint c in Checkpoints()) if (!c.IsActive) R.Call(c, "OnTriggerEnter2D", Player.GetComponent<Collider2D>());
        }

        public static void ReplayCheckpointPulse()
        {
            foreach (CheckpointGlow g in FindAll<CheckpointGlow>()) R.Call(g, "OnActivated");
        }

        public static void OpenGate()
        {
            GoalFx fx = Find<GoalFx>();
            if (fx != null) R.Set(fx, "_sealed", true);
        }

        public static void ResetGate()
        {
            GoalFx fx = Find<GoalFx>();
            if (fx != null) R.Set(fx, "_sealed", false);
        }

        public static void WakeGlitches()
        {
            foreach (GlitchEnemy g in FindAll<GlitchEnemy>()) Lens.SetOverride(g.Home);
        }

        public static void CrushNearestGlitch()
        {
            GlitchEnemy g = FindAll<GlitchEnemy>().Where(x => x.IsAlive)
                .OrderBy(x => Vector2.Distance(x.transform.position, Player.transform.position)).FirstOrDefault();
            if (g != null) R.Call(g, "Die");
        }

        public static void RewriteWallText()
        {
            foreach (WallText w in FindAll<WallText>())
            {
                R.Set(w, "_typed", 0f);
                R.Set(w, "_started", true);
            }
        }

        public static void RevealWallText()
        {
            foreach (WallText w in FindAll<WallText>()) w.RevealAll();
        }

        public static void SoulConfidence(float value)
        {
            PlayerSoulVisual v = Find<PlayerSoulVisual>();
            if (v == null) return;
            R.Set(v, "_confidence", value);
            R.Set(v, "_startDistance", 1e6f);   // freezes the automatic value near the override
        }

        public static void SoulGlance()
        {
            PlayerSoulVisual v = Find<PlayerSoulVisual>();
            if (v != null) R.Set(v, "_lookTarget", new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.4f)));
        }

        public static void ResolveRiftNow()
        {
            Goal g = Find<Goal>();
            if (g != null && !g.IsReached) R.Call(g, "OnTriggerEnter2D", Player.GetComponent<Collider2D>());
        }

        public static void ShowWinScreen()
        {
            LevelRun run = Find<LevelRun>();
            if (run == null) return;
            R.Set(run, "Elapsed", 83.4f);
            R.Set(run, "Deaths", 2);
            ResolveRiftNow();
        }

        public static void PlaySound(System.Func<SoundBank, Sound> pick)
        {
            SfxPlayer sfx = Find<SfxPlayer>();
            SoundBank bank = sfx != null ? R.Get<SoundBank>(Find<GameAudio>() ?? (Object)Find<MenuAudio>(), "_bank") : null;
            if (sfx != null && bank != null) sfx.Play(pick(bank));
        }

        public static void SwitchMusic()
        {
            MusicPlayer m = Find<MusicPlayer>();
            if (m == null) return;
            SoundBank bank = R.Get<SoundBank>(m, "_bank");
            AudioClip other = m.Source.clip == bank.titleMusic ? bank.levelMusic : bank.titleMusic;
            R.Call(m, "Request", other);
        }

        public static void Shake() => Find<CameraShake>()?.AddTrauma(0.6f);
        public static void HitStop() => R.Call(Find<HitStop>(), "OnDied");
        public static void DustBurst() => R.Call(Find<PlayerDust>(), "OnLanded", 22f);

        public static void ContinueToNext()
        {
            LevelRun run = Find<LevelRun>();
            if (run == null) return;
            Find<SceneFader>().FadeOutAndLoad(R.Get<int>(run, "_nextSceneIndex"));
        }

        public static void RestartLevel() => Find<SceneFader>()?.ReloadCurrent();
        public static void BackToTitle() => Find<SceneFader>()?.FadeOutAndLoad(0);
    }
}
