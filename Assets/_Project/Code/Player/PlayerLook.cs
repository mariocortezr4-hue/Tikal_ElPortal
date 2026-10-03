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

        private float _yaw;
        private float _pitch;

        public bool IsConfigured => _cameraRoot != null;
        public float Pitch => _pitch;

        private void Awake()
        {
            if (_cameraRoot == null)
            {
                UnityEngine.Debug.LogError("PlayerLook requires a CameraRoot reference.", this);
                enabled = false;
                return;
            }
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
            _pitch = Mathf.Clamp(_pitch - rotation.y, _minimumPitch, _maximumPitch);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            _cameraRoot.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        public void SetEyeHeight(float height)
        {
            if (!IsConfigured)
                return;
            var position = _cameraRoot.localPosition;
            position.y = height;
            _cameraRoot.localPosition = position;
        }
    }
}
