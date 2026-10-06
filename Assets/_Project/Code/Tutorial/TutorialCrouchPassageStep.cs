using Tikal.Player;
using UnityEngine;

namespace Tikal.Tutorial
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialCrouchPassageStep : TutorialStep
    {
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private Transform _player;
        private bool _inside;

        public override bool HasRequiredReferences => _movement != null && _player != null && GetComponent<BoxCollider>() != null;
        protected override void OnStepTick(float deltaTime)
        {
            if (_inside && _movement.IsCrouching) Complete();
        }

        private void OnTriggerEnter(Collider other) { if (IsPlayer(other)) _inside = true; }
        private void OnTriggerExit(Collider other) { if (IsPlayer(other)) _inside = false; }
        private bool IsPlayer(Collider other) => other.transform == _player || other.transform.IsChildOf(_player);
    }
}
