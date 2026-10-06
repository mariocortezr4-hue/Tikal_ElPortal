using Tikal.Player;
using UnityEngine;

namespace Tikal.Tutorial
{
    public sealed class TutorialLookStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerLook _look;
        [SerializeField, Min(10)] private float _requiredDegrees = 45f;
        private float _lastYaw;
        private float _lastPitch;
        private float _turned;
        private PlayerCameraController _cameraController;
        private bool _wasThirdPerson;

        private bool IsThirdPerson => _cameraController != null && _cameraController.OwnsLook;
        private float CurrentYaw => IsThirdPerson ? _cameraController.Yaw : _player.eulerAngles.y;
        private float CurrentPitch => IsThirdPerson ? _cameraController.Pitch : _look.Pitch;

        public override bool HasRequiredReferences => _player != null && _look != null;

        protected override void OnActivated()
        {
            _cameraController = _player.GetComponent<PlayerCameraController>();
            _wasThirdPerson = IsThirdPerson;
            _lastYaw = CurrentYaw;
            _lastPitch = CurrentPitch;
        }

        protected override void OnStepTick(float deltaTime)
        {
            var yaw = CurrentYaw;
            var pitch = CurrentPitch;
            if (_wasThirdPerson != IsThirdPerson)
            {
                _wasThirdPerson = IsThirdPerson;
                _lastYaw = yaw;
                _lastPitch = pitch;
                return; // Switching modes alone must not complete Look.
            }
            _turned += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) + Mathf.Abs(pitch - _lastPitch);
            _lastYaw = yaw;
            _lastPitch = pitch;
            if (_turned >= _requiredDegrees) Complete();
        }
    }
}
