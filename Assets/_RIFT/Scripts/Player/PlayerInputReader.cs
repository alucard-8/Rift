using UnityEngine;
using UnityEngine.InputSystem;

namespace Rift.Player
{
    /// <summary>The only player script that knows about input. Forwards actions to the motor.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _jump;

        private PlayerMotor _motor;

        private void Awake() => _motor = GetComponent<PlayerMotor>();

        private void OnEnable()
        {
            _move.action.Enable();
            _jump.action.Enable();
            _jump.action.performed += OnJumpPressed;
            _jump.action.canceled += OnJumpReleased;
        }

        private void OnDisable()
        {
            _jump.action.performed -= OnJumpPressed;
            _jump.action.canceled -= OnJumpReleased;
        }

        private void Update() => _motor.SetMove(_move.action.ReadValue<float>());

        private void OnJumpPressed(InputAction.CallbackContext _) => _motor.PressJump();
        private void OnJumpReleased(InputAction.CallbackContext _) => _motor.ReleaseJump();
    }
}
