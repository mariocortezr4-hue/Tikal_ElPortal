using System;
using UnityEngine.InputSystem;

namespace Tikal.Input
{
    public static class InputPromptResolver
    {
        public static InputPromptData Resolve(InputAction action, InputDeviceFamily family)
        {
            if (action == null || action.actionMap?.asset == null) return default;
            if (string.Equals(action.name, "Move", StringComparison.Ordinal))
                return new InputPromptData(family == InputDeviceFamily.KeyboardMouse ? "WASD" : "STICK IZQUIERDO", "<Player>/move");
            if (string.Equals(action.name, "Look", StringComparison.Ordinal))
                return new InputPromptData(family == InputDeviceFamily.KeyboardMouse ? "MOUSE" : "STICK DERECHO", "<Player>/look");
            if (string.Equals(action.name, "Navigate", StringComparison.Ordinal))
                return new InputPromptData(family == InputDeviceFamily.KeyboardMouse
                    ? "WASD / FLECHAS" : "STICK / CRUCETA", "<UI>/navigate");
            var schemeName = family == InputDeviceFamily.KeyboardMouse ? "KeyboardMouse" : "Gamepad";
            var schemes = action.actionMap.asset.controlSchemes;
            var group = string.Empty;
            for (var index = 0; index < schemes.Count; index++)
                if (schemes[index].name == schemeName) { group = schemes[index].bindingGroup; break; }
            if (string.IsNullOrEmpty(group)) return default;
            var mask = InputBinding.MaskByGroup(group);
            var bindings = action.bindings;
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                if (binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath) || !mask.Matches(binding)) continue;
                var display = action.GetBindingDisplayString(index, out _, out var control,
                    InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (family != InputDeviceFamily.KeyboardMouse) display = FaceButtonText(control, family) ?? display;
                return new InputPromptData(display, binding.effectivePath);
            }
            return default;
        }
        private static string FaceButtonText(string control, InputDeviceFamily family)
        {
            if (string.Equals(control, "select", StringComparison.OrdinalIgnoreCase))
                return family == InputDeviceFamily.Xbox ? "View" : family == InputDeviceFamily.PlayStation ? "Share" : "Select";
            if (string.Equals(control, "buttonWest", StringComparison.OrdinalIgnoreCase))
                return family == InputDeviceFamily.Xbox ? "X" : family == InputDeviceFamily.PlayStation ? "□" : "Botón oeste";
            if (string.Equals(control, "buttonNorth", StringComparison.OrdinalIgnoreCase))
                return family == InputDeviceFamily.Xbox ? "Y" : family == InputDeviceFamily.PlayStation ? "△" : "Botón norte";
            if (string.Equals(control, "buttonSouth", StringComparison.OrdinalIgnoreCase))
                return family == InputDeviceFamily.Xbox ? "A" : family == InputDeviceFamily.PlayStation ? "×" : "Botón sur";
            if (string.Equals(control, "buttonEast", StringComparison.OrdinalIgnoreCase))
                return family == InputDeviceFamily.Xbox ? "B" : family == InputDeviceFamily.PlayStation ? "○" : "Botón este";
            return null;
        }
    }
}
