using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tikal.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputReader
    {
        private TikalInputActions _actions;
        private bool _callbacksSubscribed;

        public bool GameplayEnabled => _actions != null && _actions.Player.enabled;

        public Vector2 Move => GameplayEnabled ? _actions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => GameplayEnabled ? _actions.Player.Look.ReadValue<Vector2>() : Vector2.zero;
        public LookInputMode LookMode => _actions != null && _actions.Player.Look.activeControl is DeltaControl
            ? LookInputMode.Delta : LookInputMode.Rate;
        public bool SprintHeld => GameplayEnabled && _actions.Player.Sprint.IsPressed();
        public bool CrouchHeld => GameplayEnabled && _actions.Player.Crouch.IsPressed();

        public event Action JumpPressed;
        public event Action InteractPressed;
        public event Action SecretCommandPressed;
        public event Action PausePressed;

        private void Awake()
        {
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

            _actions.Player.Disable();
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
                _actions.Player.Enable();
        }

        /// <summary>Disables player actions while leaving global pause available.</summary>
        public void DisableGameplay()
        {
            if (_actions != null && _actions.Player.enabled)
                _actions.Player.Disable();
        }

        private void SubscribeCallbacks()
        {
            if (_callbacksSubscribed)
                return;

            _actions.Player.Jump.performed += OnJumpPerformed;
            _actions.Player.Interact.performed += OnInteractPerformed;
            _actions.Player.SecretCommand.performed += OnSecretCommandPerformed;
            _actions.Global.Pause.performed += OnPausePerformed;
            _callbacksSubscribed = true;
        }

        private void UnsubscribeCallbacks()
        {
            if (!_callbacksSubscribed)
                return;

            _actions.Player.Jump.performed -= OnJumpPerformed;
            _actions.Player.Interact.performed -= OnInteractPerformed;
            _actions.Player.SecretCommand.performed -= OnSecretCommandPerformed;
            _actions.Global.Pause.performed -= OnPausePerformed;
            _callbacksSubscribed = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context) => JumpPressed?.Invoke();
        private void OnInteractPerformed(InputAction.CallbackContext context) => InteractPressed?.Invoke();
        private void OnSecretCommandPerformed(InputAction.CallbackContext context) => SecretCommandPressed?.Invoke();
        private void OnPausePerformed(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
