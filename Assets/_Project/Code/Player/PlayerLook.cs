using Tikal.Input;
using UnityEngine;

namespace Tikal.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform _cameraRoot;
        [SerializeField, Min(0)] private float _deltaSensitivity = 0.12f;
        [SerializeField, Min(0)] private float _rateSensitivity = 150f;
        [SerializeField, Range(-89, 0)] private float _minimumPitch = -89f;
        [SerializeField, Range(0, 89)] private float _maximumPitch = 89f;
        [SerializeField] private bool _invertY;
        [SerializeField, Min(0.01f)] private float _crouchCameraTransitionSpeed = 14f;

        [Header("Optional visual sprint feedback")]
        [SerializeField] private bool _enableSprintFov = true;
        [SerializeField, Range(1, 179)] private float _normalFov = 75f;
        [SerializeField, Range(1, 179)] private float _sprintFov = 78f;
        [SerializeField, Min(0.01f)] private float _fovTransitionSpeed = 10f;
        private Camera _camera;

        private float _yaw;
        private float _pitch;

        public bool IsConfigured => _cameraRoot != null;
        public Transform CameraRoot => _cameraRoot;
        public float Pitch => _pitch;
        public float MouseSensitivity => _deltaSensitivity;
        public float GamepadSensitivity => _rateSensitivity;
        public bool InvertY => _invertY;

        // TPS movement can turn Explorer while FPS Look is inactive. Resume from that facing.
        public void SynchronizePlayerYaw() => _yaw = transform.eulerAngles.y;

        public void ResetAfterRespawn(float eyeHeight, float maximumHeight)
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 0;
            if (_cameraRoot != null)
            {
                _cameraRoot.localRotation = Quaternion.identity;
                var position = _cameraRoot.localPosition;
                position.y = Mathf.Min(eyeHeight, maximumHeight);
                _cameraRoot.localPosition = position;
            }
            if (_camera != null) _camera.fieldOfView = _normalFov;
        }

        private void Awake()
        {
            if (_cameraRoot == null)
            {
                UnityEngine.Debug.LogError("PlayerLook requires a CameraRoot reference.", this);
                enabled = false;
                return;
            }
            _camera = _cameraRoot.GetComponentInChildren<Camera>();
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(Mathf.DeltaAngle(0, _cameraRoot.localEulerAngles.x), _minimumPitch, _maximumPitch);
        }

        public void Tick(Vector2 look, LookInputMode mode, float deltaTime)
        {
            if (!IsConfigured || deltaTime <= 0)
                return;

            var rotation = mode == LookInputMode.Delta
                ? look * _deltaSensitivity
                : Vector2.ClampMagnitude(look, 1f) * (_rateSensitivity * deltaTime);
            _yaw = Mathf.Repeat(_yaw + rotation.x, 360f);
            _pitch = Mathf.Clamp(_pitch + rotation.y * (_invertY ? 1f : -1f), _minimumPitch, _maximumPitch);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            _cameraRoot.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        public void ApplySettings(float mouseSensitivity, float gamepadSensitivity, bool invertY)
        {
            _deltaSensitivity = Mathf.Max(0, mouseSensitivity);
            _rateSensitivity = Mathf.Max(0, gamepadSensitivity);
            _invertY = invertY;
        }

        public void SetEyeHeight(float height, float maximumHeight, float deltaTime)
        {
            if (!IsConfigured)
                return;
            var position = _cameraRoot.localPosition;
            var blend = 1f - Mathf.Exp(-_crouchCameraTransitionSpeed * Mathf.Max(0, deltaTime));
            // Keep the viewpoint inside the capsule even when smoothing trails a crouch transition.
            position.y = Mathf.Min(Mathf.Lerp(position.y, height, blend), maximumHeight);
            _cameraRoot.localPosition = position;
        }

        public void TickSprintFeedback(bool sprinting, float deltaTime)
        {
            if (_camera == null || deltaTime <= 0)
                return;
            var target = _enableSprintFov && sprinting ? _sprintFov : _normalFov;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, target, 1f - Mathf.Exp(-_fovTransitionSpeed * deltaTime));
        }
    }
}
