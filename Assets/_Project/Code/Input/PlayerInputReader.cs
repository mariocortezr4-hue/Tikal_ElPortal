using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tikal.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputDeviceManager))]
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputReader
    {
        [SerializeField, Range(0.01f, 0.5f)] private float _gamepadSprintNeutralThreshold = 0.15f;
        private InputDeviceManager _deviceManager;
        private bool _gamepadSprintLatched;
        private TikalInputActions _actions;
        private bool _callbacksSubscribed;

        public InputAction SecretCommandAction => _actions == null ? null : _actions.Player.SecretCommand;
        public InputAction MoveAction => _actions == null ? null : _actions.Player.Move;
        public InputAction LookAction => _actions == null ? null : _actions.Player.Look;
        public InputAction JumpAction => _actions == null ? null : _actions.Player.Jump;
        public InputAction SprintAction => _actions == null ? null : _actions.Player.Sprint;
        public InputAction CrouchAction => _actions == null ? null : _actions.Player.Crouch;
        public InputAction InteractAction => _actions == null ? null : _actions.Player.Interact;
        public InputAction JournalAction => _actions == null ? null : _actions.Global.Journal;
        public bool GameplayEnabled => _actions != null && _actions.Player.enabled;

        public Vector2 Move => GameplayEnabled ? _actions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => GameplayEnabled ? _actions.Player.Look.ReadValue<Vector2>() : Vector2.zero;
        public LookInputMode LookMode => _actions != null && _actions.Player.Look.activeControl is DeltaControl
            ? LookInputMode.Delta : LookInputMode.Rate;
        /// <summary>Raw semantic button hold; gameplay consumes SprintRequested instead.</summary>
        public bool SprintHeld => GameplayEnabled && _actions.Player.Sprint.IsPressed();
        public bool GamepadSprintLatched => _gamepadSprintLatched;
        public float GamepadSprintNeutralThreshold => _gamepadSprintNeutralThreshold;
        public bool SprintRequested
        {
            get
            {
                if (!GameplayEnabled) return false;
                if (_deviceManager.ActiveDevice != InputDeviceFamily.KeyboardMouse)
                    return _gamepadSprintLatched;
                // Read the controls bound to the semantic action, including future rebindings.
                // A held gamepad button must not become a keyboard hold after a device switch.
                foreach (var control in _actions.Player.Sprint.controls)
                    if (control is ButtonControl button &&
                        InputDeviceManager.Classify(control.device) == InputDeviceFamily.KeyboardMouse && button.isPressed)
                        return true;
                return false;
            }
        }
        public bool CrouchHeld => GameplayEnabled && _actions.Player.Crouch.IsPressed();

        public event Action<bool> GameplayEnabledChanged;
        public event Action JumpPressed;
        public event Action InteractPressed;
        public event Action SecretCommandPressed;
        public event Action PausePressed;
        public event Action JournalPressed;

        private void Awake()
        {
            _deviceManager = GetComponent<InputDeviceManager>();
            // Support older serialized test scenes authored before this dependency was required.
            if (_deviceManager == null) _deviceManager = gameObject.AddComponent<InputDeviceManager>();
            if (_actions == null)
                _actions = new TikalInputActions();
        }

        private void OnEnable()
        {
            if (_actions == null)
                return;

            SubscribeCallbacks();
            _actions.Global.Enable();
            EnableGameplay();
        }

        private void OnDisable()
        {
            if (_actions == null)
                return;

            DisableGameplay();
            _actions.Global.Disable();
            UnsubscribeCallbacks();
        }

        private void OnDestroy()
        {
            if (_actions == null)
                return;

            OnDisable();
            _actions.Dispose();
            _actions = null;
        }

        /// <summary>Enables player actions only while this component is active.</summary>
        public void EnableGameplay()
        {
            if (isActiveAndEnabled && _actions != null && !_actions.Player.enabled)
            {
                _gamepadSprintLatched = false;
                _actions.Player.Enable();
                GameplayEnabledChanged?.Invoke(true);
            }
        }

        /// <summary>Disables player actions while leaving global pause and journal available.</summary>
        public void DisableGameplay()
        {
            _gamepadSprintLatched = false;
            if (_actions != null && _actions.Player.enabled)
            {
                _actions.Player.Disable();
                GameplayEnabledChanged?.Invoke(false);
            }
        }

        private void SubscribeCallbacks()
        {
            if (_callbacksSubscribed)
                return;

            _deviceManager.ActiveDeviceChanged += OnDeviceChanged;
            _actions.Player.Move.performed += OnMoveChanged;
            _actions.Player.Move.canceled += OnMoveChanged;
            _actions.Player.Sprint.performed += OnSprintPerformed;
            _actions.Player.Jump.performed += OnJumpPerformed;
            _actions.Player.Interact.performed += OnInteractPerformed;
            _actions.Player.SecretCommand.performed += OnSecretCommandPerformed;
            _actions.Global.Pause.performed += OnPausePerformed;
            _actions.Global.Journal.performed += OnJournalPerformed;
            _callbacksSubscribed = true;
        }

        private void UnsubscribeCallbacks()
        {
            if (!_callbacksSubscribed)
                return;

            _deviceManager.ActiveDeviceChanged -= OnDeviceChanged;
            _actions.Player.Move.performed -= OnMoveChanged;
            _actions.Player.Move.canceled -= OnMoveChanged;
            _actions.Player.Sprint.performed -= OnSprintPerformed;
            _actions.Player.Jump.performed -= OnJumpPerformed;
            _actions.Player.Interact.performed -= OnInteractPerformed;
            _actions.Player.SecretCommand.performed -= OnSecretCommandPerformed;
            _actions.Global.Pause.performed -= OnPausePerformed;
            _actions.Global.Journal.performed -= OnJournalPerformed;
            _callbacksSubscribed = false;
        }

        private void OnDeviceChanged(InputDeviceFamily family) => _gamepadSprintLatched = false;

        private void OnMoveChanged(InputAction.CallbackContext context)
        {
            if (context.ReadValue<Vector2>().sqrMagnitude <= _gamepadSprintNeutralThreshold * _gamepadSprintNeutralThreshold)
                _gamepadSprintLatched = false;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            if (GameplayEnabled && InputDeviceManager.Classify(context.control.device) != InputDeviceFamily.KeyboardMouse &&
                _deviceManager.ActiveDevice != InputDeviceFamily.KeyboardMouse &&
                Move.sqrMagnitude > _gamepadSprintNeutralThreshold * _gamepadSprintNeutralThreshold)
                _gamepadSprintLatched = true;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context) => JumpPressed?.Invoke();
        private void OnInteractPerformed(InputAction.CallbackContext context) => InteractPressed?.Invoke();
        private void OnSecretCommandPerformed(InputAction.CallbackContext context) => SecretCommandPressed?.Invoke();
        private void OnJournalPerformed(InputAction.CallbackContext context) => JournalPressed?.Invoke();
        private void OnPausePerformed(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
