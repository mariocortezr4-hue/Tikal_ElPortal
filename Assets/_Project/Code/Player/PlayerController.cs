using Tikal.Input;
using UnityEngine;

namespace Tikal.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement), typeof(PlayerLook))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerLook _look;

        private IPlayerInputReader _input;
        private bool _jumpSubscribed;
        private bool _gameplayActive;
        private PlayerCameraController _playerCamera;
        private bool _wasThirdPerson;
        public bool GameplayActive => _gameplayActive;

        private void Awake()
        {
            if (_inputReader == null) _inputReader = GetComponent<PlayerInputReader>();
            if (_movement == null) _movement = GetComponent<PlayerMovement>();
            if (_look == null) _look = GetComponent<PlayerLook>();
            _input = _inputReader;
            _playerCamera = GetComponent<PlayerCameraController>();
            if (_input == null || _movement == null || _look == null || !_look.IsConfigured)
            {
                UnityEngine.Debug.LogError("PlayerController requires input, movement and a configured look component.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_input == null)
                return;
            if (!_jumpSubscribed)
            {
                _input.JumpPressed += OnJumpPressed;
                _jumpSubscribed = true;
            }
            SetGameplayActive(true);
        }

        private void OnDisable()
        {
            if (_jumpSubscribed)
            {
                _input.JumpPressed -= OnJumpPressed;
                _jumpSubscribed = false;
            }
            SetGameplayActive(false);
        }

        private void Update()
        {
            if (!_gameplayActive || !_input.GameplayEnabled)
                return;
            var thirdPerson = _playerCamera != null && _playerCamera.OwnsLook;
            if (_wasThirdPerson && !thirdPerson) _look.SynchronizePlayerYaw();
            _wasThirdPerson = thirdPerson;
            if (thirdPerson)
                _playerCamera.TickLook(_input.Look, _input.LookMode, Time.deltaTime);
            else
                _look.Tick(_input.Look, _input.LookMode, Time.deltaTime);
            if (thirdPerson)
                _movement.TickThirdPerson(_input.Move, _input.SprintRequested, _input.CrouchHeld,
                    Time.deltaTime, _playerCamera.MovementOrientation);
            else
                _movement.Tick(_input.Move, _input.SprintRequested, _input.CrouchHeld, Time.deltaTime);
            _look.SetEyeHeight(_movement.EyeHeight, _movement.MaximumEyeHeight, Time.deltaTime);
            if (!thirdPerson)
                _look.TickSprintFeedback(_movement.IsSprinting, Time.deltaTime);
        }

        /// <summary>Allows a future pause flow to gate gameplay and release the cursor without disabling Global.</summary>
        public void SetGameplayActive(bool active)
        {
            _gameplayActive = active && isActiveAndEnabled && _input != null;
            if (_gameplayActive)
            {
                _input.EnableGameplay();
            }
            else
            {
                _input?.DisableGameplay();
                if (_movement != null) _movement.StopMotion();
            }
            if (Application.isPlaying)
            {
                Cursor.lockState = _gameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !_gameplayActive;
            }
        }

        private void OnJumpPressed()
        {
            if (_gameplayActive)
                _movement.RequestJump();
        }
    }
}
