using Tikal.Input;
using UnityEngine;

namespace Tikal.Tutorial
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialJumpStep : TutorialStep
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Transform _player;
        private bool _insideObstacleZone;

        public bool IsInsideObstacleZone => _insideObstacleZone;
        public override bool HasRequiredReferences => _input != null && _player != null && GetComponent<BoxCollider>() != null;

        protected override void OnActivated() => _input.JumpPressed += OnJump;
        protected override void OnDeactivated() => _input.JumpPressed -= OnJump;

        private void OnJump()
        {
            if (_insideObstacleZone) Complete();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other)) _insideObstacleZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) _insideObstacleZone = false;
        }

        private bool IsPlayer(Collider other) => other.transform == _player || other.transform.IsChildOf(_player);
    }
}
