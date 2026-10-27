using System;
using Rift.Core;
using Rift.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rift.Levels
{
    /// <summary>State of one attempt at a level: timer, deaths, completion and restart.</summary>
    public class LevelRun : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _player;
        [SerializeField] private PlayerRespawn _respawn;
        [SerializeField] private PlayerInputReader _playerInput;
        [SerializeField] private Goal _goal;
        [SerializeField] private InputActionReference _restart;
        [SerializeField] private InputActionReference _back;
        [SerializeField] private SceneFader _fader;
        [SerializeField] private int _titleSceneIndex = 0;
        [SerializeField] private InputActionReference _continue;
        [Tooltip("Build index loaded when the player continues after finishing (0 = back to title).")]
        [SerializeField] private int _nextSceneIndex = 0;
        [Tooltip("Seconds after finishing before continue is accepted, so the ending can play.")]
        [SerializeField] private float _continueDelay = 2.5f;

        private bool _started;
        private float _completedAt;

        public event Action<float, int> Completed;

        public float Elapsed { get; private set; }
        public int Deaths { get; private set; }
        public bool IsComplete { get; private set; }

        private void OnEnable()
        {
            _respawn.Died += OnDied;
            _goal.Reached += OnGoalReached;
            _restart.action.Enable();
            _restart.action.performed += OnRestart;
            _back.action.Enable();
            _back.action.performed += OnBack;
            _continue.action.Enable();
            _continue.action.performed += OnContinue;
        }

        private void OnDisable()
        {
            _respawn.Died -= OnDied;
            _goal.Reached -= OnGoalReached;
            _restart.action.performed -= OnRestart;
            _back.action.performed -= OnBack;
            _continue.action.performed -= OnContinue;
        }

        private void Update()
        {
            if (IsComplete) return;

            // The clock starts the first time the player moves, not when the scene loads.
            if (!_started && _player.Velocity.sqrMagnitude > 0.01f && _player.IsGrounded) _started = true;
            if (_started) Elapsed += Time.deltaTime;
        }

        private void OnDied() => Deaths++;

        private void OnGoalReached()
        {
            IsComplete = true;
            _completedAt = Time.unscaledTime;
            _playerInput.enabled = false;
            _player.SetMove(0f);
            _player.Freeze();
            Completed?.Invoke(Elapsed, Deaths);
        }

        private void OnRestart(InputAction.CallbackContext _) => _fader.ReloadCurrent();

        private void OnBack(InputAction.CallbackContext _) => _fader.FadeOutAndLoad(_titleSceneIndex);

        private void OnContinue(InputAction.CallbackContext _)
        {
            if (IsComplete && Time.unscaledTime - _completedAt >= _continueDelay)
                _fader.FadeOutAndLoad(_nextSceneIndex);
        }
    }
}
