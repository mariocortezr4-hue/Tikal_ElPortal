using UnityEngine;

namespace Tikal.Tutorial
{
    public sealed class TutorialMovementStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField, Min(.5f)] private float _requiredDistance = 4f;
        private Vector3 _previousPosition;
        private float _travelled;

        public override bool HasRequiredReferences => _player != null;
        protected override void OnActivated() => _previousPosition = _player.position;

        protected override void OnStepTick(float deltaTime)
        {
            var position = _player.position;
            var delta = position - _previousPosition;
            delta.y = 0;
            _travelled += Mathf.Min(delta.magnitude, 1f);
            _previousPosition = position;
            if (_travelled >= _requiredDistance) Complete();
        }
    }
}
