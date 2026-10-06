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

        public override bool HasRequiredReferences => _player != null && _look != null;

        protected override void OnActivated()
        {
            _lastYaw = _player.eulerAngles.y;
            _lastPitch = _look.Pitch;
        }

        protected override void OnStepTick(float deltaTime)
        {
            var yaw = _player.eulerAngles.y;
            var pitch = _look.Pitch;
            _turned += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) + Mathf.Abs(pitch - _lastPitch);
            _lastYaw = yaw;
            _lastPitch = pitch;
            if (_turned >= _requiredDegrees) Complete();
        }
    }
}
