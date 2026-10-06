using Tikal.Player;
using UnityEngine;

namespace Tikal.Tutorial
{
    public sealed class TutorialSprintStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField, Min(1)] private float _requiredDistance = 6f;
        private Vector3 _previousPosition;
        private float _travelled;

        public override bool HasRequiredReferences => _player != null && _movement != null;
        protected override void OnActivated() => _previousPosition = _player.position;

        protected override void OnStepTick(float deltaTime)
        {
            var position = _player.position;
            var delta = position - _previousPosition;
            delta.y = 0;
            if (_movement.IsSprinting) _travelled += Mathf.Min(delta.magnitude, 1f);
            _previousPosition = position;
            if (_travelled >= _requiredDistance) Complete();
        }
    }
}
