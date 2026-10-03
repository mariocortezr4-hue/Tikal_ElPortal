using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Tikal.Input
{
    [DisallowMultipleComponent]
    public sealed class InputDeviceManager : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 1f)] private float _stickThreshold = 0.25f;
        [SerializeField, Range(0.05f, 1f)] private float _triggerThreshold = 0.2f;
        [SerializeField, Min(0.01f)] private float _mouseDeltaThreshold = 1f;
        [SerializeField, Min(0.01f)] private float _scrollThreshold = 0.1f;
        private bool _subscribed;
        private InputDevice _activeSource;

        public InputDeviceFamily ActiveDevice { get; private set; } = InputDeviceFamily.KeyboardMouse;
        public event Action<InputDeviceFamily> ActiveDeviceChanged;

        private void OnEnable()
        {
            if (_subscribed) return;
            InputSystem.onEvent += OnInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            _subscribed = true;
            if (_activeSource == null || !_activeSource.added || !_activeSource.enabled) SelectFallback();
        }
        private void OnDisable()
        {
            if (!_subscribed) return;
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
            _subscribed = false;
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type) return;
            if (!device.enabled || !(device is Keyboard || device is Mouse || device is Gamepad)) return;
            foreach (var control in eventPtr.EnumerateChangedControls(device, 0.0001f))
            {
                if (!IsSignificant(control, eventPtr, device)) continue;
                Activate(device);
                break;
            }
        }
        private bool IsSignificant(InputControl control, InputEventPtr eventPtr, InputDevice device)
        {
            if (device is Mouse mouse)
            {
                if (control == mouse.delta || control.parent == mouse.delta)
                    return mouse.delta.ReadValueFromEvent(eventPtr, out var delta) && delta.sqrMagnitude >= _mouseDeltaThreshold * _mouseDeltaThreshold;
                if (control == mouse.scroll || control.parent == mouse.scroll)
                    return mouse.scroll.ReadValueFromEvent(eventPtr, out var scroll) && scroll.sqrMagnitude >= _scrollThreshold * _scrollThreshold;
                return control is ButtonControl mouseButton && mouseButton.ReadValueFromEvent(eventPtr, out var mouseValue) && mouseValue >= 0.5f;
            }
            if (device is Gamepad pad)
            {
                if (control == pad.leftTrigger || control == pad.rightTrigger)
                    return ((ButtonControl)control).ReadValueFromEvent(eventPtr, out var trigger) && trigger >= _triggerThreshold;
                var stick = control as StickControl ?? control.parent as StickControl;
                if (stick != null)
                    return stick.ReadValueFromEvent(eventPtr, out var value) && value.sqrMagnitude >= _stickThreshold * _stickThreshold;
            }
            return control is ButtonControl button && button.ReadValueFromEvent(eventPtr, out var pressed) && pressed >= 0.5f;
        }
        private void Activate(InputDevice device)
        {
            if (_activeSource == device) return;
            _activeSource = device;
            var family = Classify(device);
            if (family == ActiveDevice) return;
            ActiveDevice = family;
            ActiveDeviceChanged?.Invoke(family);
        }
        public static InputDeviceFamily Classify(InputDevice device)
        {
            if (!(device is Gamepad)) return InputDeviceFamily.KeyboardMouse;
            if (device is DualShockGamepad) return InputDeviceFamily.PlayStation;
            if (device is XInputController) return InputDeviceFamily.Xbox;
            var description = device.description;
            if (Contains(device.layout, "DualShock") || Contains(device.layout, "DualSense") || Contains(description.product, "DualShock") || Contains(description.product, "DualSense") || Contains(description.product, "PlayStation") ||
                (Contains(description.manufacturer, "Sony") && Contains(description.product, "Wireless Controller")))
                return InputDeviceFamily.PlayStation;
            if (Contains(device.layout, "XInput") || Contains(description.interfaceName, "XInput") || Contains(description.product, "Xbox"))
                return InputDeviceFamily.Xbox;
            return InputDeviceFamily.GenericGamepad;
        }
        private static bool Contains(string value, string token) => value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device == _activeSource && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled))
            {
                _activeSource = null;
                SelectFallback();
            }
        }
        private void SelectFallback()
        {
            InputDevice fallback = null;
            foreach (var device in InputSystem.devices)
            {
                if (!device.added || !device.enabled) continue;
                if (device is Keyboard) { fallback = device; break; }
                if (device is Mouse || (fallback == null && device is Gamepad)) fallback = device;
            }
            if (fallback != null) { Activate(fallback); return; }
            _activeSource = null;
            if (ActiveDevice == InputDeviceFamily.KeyboardMouse) return;
            ActiveDevice = InputDeviceFamily.KeyboardMouse;
            ActiveDeviceChanged?.Invoke(ActiveDevice);
        }
    }
}
