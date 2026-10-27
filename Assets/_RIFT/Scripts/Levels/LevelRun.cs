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

        private bool _started;

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
        }

        private void OnDisable()
        {
            _respawn.Died -= OnDied;
            _goal.Reached -= OnGoalReached;
            _restart.action.performed -= OnRestart;
            _back.action.performed -= OnBack;
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
            _playerInput.enabled = false;
            _player.SetMove(0f);
            _player.ResetMotion();
            Completed?.Invoke(Elapsed, Deaths);
        }

        private void OnRestart(InputAction.CallbackContext _) => _fader.ReloadCurrent();

        private void OnBack(InputAction.CallbackContext _) => _fader.FadeOutAndLoad(_titleSceneIndex);
    }
}
