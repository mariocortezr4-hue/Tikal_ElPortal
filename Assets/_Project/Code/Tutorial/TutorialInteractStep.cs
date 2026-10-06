using UnityEngine;

namespace Tikal.Tutorial
{
    public sealed class TutorialInteractStep : TutorialStep
    {
        [SerializeField] private TutorialExitGate _gate;

        public override bool HasRequiredReferences => _gate != null;

        protected override void OnActivated()
        {
            _gate.Interacted += OnInteracted;
            _gate.SetInteractionAvailable(true);
        }

        protected override void OnDeactivated() => _gate.Interacted -= OnInteracted;
        private void OnInteracted() => Complete();
    }
}
