using Tikal.Input;
using Tikal.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tikal.UI
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private InputDeviceManager _deviceManager;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private TMP_Text _label;
        private InputAction _action;
        private InputPromptData _control;
        private bool _subscribed;
        private IInteractable _lastTarget;
        private string _lastText;
        private bool _lastVisible;

        private void OnEnable()
        {
            if (_subscribed) return;
            if (_interaction == null || _deviceManager == null || _inputReader == null || _promptRoot == null || _label == null)
            {
                if (_promptRoot != null) _promptRoot.SetActive(false);
                UnityEngine.Debug.LogError("InteractionPromptPresenter requires interaction, device, input and visual references.", this);
                enabled = false;
                return;
            }
            _action = _inputReader.InteractAction;
            _interaction.FocusChanged += OnFocusChanged;
            _deviceManager.ActiveDeviceChanged += OnDeviceChanged;
            InputSystem.onActionChange += OnActionChange;
            _subscribed = true;
            ResolveControl();
        }
        private void OnDisable()
        {
            if (_subscribed)
            {
                _interaction.FocusChanged -= OnFocusChanged;
                _deviceManager.ActiveDeviceChanged -= OnDeviceChanged;
                InputSystem.onActionChange -= OnActionChange;
                _subscribed = false;
            }
            if (_promptRoot != null) _promptRoot.SetActive(false);
            _lastTarget = null;
            _lastText = null;
            _lastVisible = false;
        }
        private void OnFocusChanged(IInteractable _) => RefreshContent(false);
        private void OnDeviceChanged(InputDeviceFamily _) => ResolveControl();
        private void LateUpdate() => RefreshContent(false);
        private void OnActionChange(object source, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged || _action == null) return;
            if (ReferenceEquals(source, _action) || ReferenceEquals(source, _action.actionMap) || ReferenceEquals(source, _action.actionMap.asset)) ResolveControl();
        }
        private void ResolveControl()
        {
            _action = _inputReader.InteractAction;
            var resolved = InputPromptResolver.Resolve(_action, _deviceManager.ActiveDevice);
            var changed = resolved.DisplayText != _control.DisplayText || resolved.ControlPath != _control.ControlPath;
            _control = resolved;
            RefreshContent(changed);
        }
        private void RefreshContent(bool controlChanged)
        {
            var target = _interaction.CurrentInteractable;
            var owner = target as Object;
            var visible = owner != null && target.CanInteract && !string.IsNullOrEmpty(_control.DisplayText);
            var text = visible ? target.InteractionText : null;
            if (!controlChanged && ReferenceEquals(target, _lastTarget) && text == _lastText && visible == _lastVisible) return;
            _lastTarget = target;
            _lastText = text;
            _lastVisible = visible;
            if (_promptRoot.activeSelf != visible) _promptRoot.SetActive(visible);
            if (!visible) return;
            _label.text = "[ " + _control.DisplayText + " ]  " + text;
        }
    }
}
