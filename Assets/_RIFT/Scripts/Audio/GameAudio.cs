using Rift.Enemies;
using Rift.Levels;
using Rift.Player;
using UnityEngine;

namespace Rift.Audio
{
    /// <summary>Plays level sounds in reaction to gameplay events. Gameplay never calls into audio.</summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] private SoundBank _bank;
        [SerializeField] private SfxPlayer _sfx;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerRespawn _respawn;
        [SerializeField] private Goal _goal;
        [SerializeField] private Checkpoint[] _checkpoints;
        [SerializeField] private GlitchEnemy[] _glitches;
        [Tooltip("Landing speed that plays the land sound at full volume.")]
        [SerializeField] private float _hardLandingSpeed = 18f;
        [SerializeField] private float _minLandingSpeed = 3f;

        private void OnEnable()
        {
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
            _respawn.Died += OnDied;
            _respawn.Respawned += OnRespawned;
            _goal.Reached += OnGoal;
            foreach (Checkpoint c in _checkpoints) c.Activated += OnCheckpoint;
            foreach (GlitchEnemy g in _glitches)
            {
                g.Presence.Materialized += OnGlitchMaterialized;
                g.Killed += OnGlitchCrushed;
            }
        }

        private void OnDisable()
        {
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
            _respawn.Died -= OnDied;
            _respawn.Respawned -= OnRespawned;
            _goal.Reached -= OnGoal;
            foreach (Checkpoint c in _checkpoints) c.Activated -= OnCheckpoint;
            foreach (GlitchEnemy g in _glitches)
            {
                // On scene unload a glitch may already be destroyed before we are.
                if (g == null) continue;
                g.Presence.Materialized -= OnGlitchMaterialized;
                g.Killed -= OnGlitchCrushed;
            }
        }

        private void OnJumped() => _sfx.Play(_bank.jump);

        private void OnLanded(float speed)
        {
            if (speed < _minLandingSpeed) return;
            _sfx.Play(_bank.land, Mathf.Clamp01(speed / _hardLandingSpeed));
        }

        private void OnDied()
        {
            _sfx.Play(_bank.deathShatter);
            _sfx.Play(_bank.deathThump);
        }

        private void OnRespawned() => _sfx.Play(_bank.respawn);
        private void OnCheckpoint() => _sfx.Play(_bank.checkpoint);
        private void OnGoal() => _sfx.Play(_bank.goalSeal);
        private void OnGlitchMaterialized() => _sfx.Play(_bank.glitchMaterialize);
        private void OnGlitchCrushed() => _sfx.Play(_bank.glitchCrushed);
    }
}
