using Tikal.Input;
using UnityEngine;

namespace Tikal.Player
{
    public static class ThirdPersonCameraMath
    {
        // GameSettingsData's existing canonical rate setting is 150 at default.
        // This reference converts that preference to a multiplier, without another setting.
        public const float GamepadSensitivityReference = 150f;

        public static Vector2 LookDelta(Vector2 look, LookInputMode mode, float mouseSensitivity,
            float gamepadSensitivity, float yawRate, float pitchRate, float exponent, float deltaTime)
        {
            if (deltaTime <= 0) return Vector2.zero;
            if (mode == LookInputMode.Delta) return look * mouseSensitivity;
            var stick = Vector2.ClampMagnitude(look, 1f);
            var magnitude = stick.magnitude;
            if (magnitude <= 0) return Vector2.zero;
            // Radial curve only: the Input System has already applied its stick deadzone.
            var shaped = stick * (Mathf.Pow(magnitude, exponent) / magnitude);
            var scale = gamepadSensitivity / GamepadSensitivityReference * deltaTime;
            return new Vector2(shaped.x * yawRate, shaped.y * pitchRate) * scale;
        }

        public static Vector3 ConfiguredBoom(float distance, float shoulder) => new Vector3(shoulder, 0, -distance);

        public static Vector3 BoomOffset(Vector3 rotatedConfiguredBoom, float currentDistance)
        {
            var radius = rotatedConfiguredBoom.magnitude;
            return radius > 0 ? rotatedConfiguredBoom * (currentDistance / radius) : Vector3.zero;
        }

        public static float RecoverDistance(float current, float safeTarget, ref float velocity, float smoothTime, float deltaTime)
        {
            if (safeTarget <= current) { velocity = 0; return safeTarget; }
            return Mathf.Min(safeTarget, Mathf.SmoothDamp(current, safeTarget, ref velocity, smoothTime, Mathf.Infinity, deltaTime));
        }
    }
}
