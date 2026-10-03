using UnityEngine;

namespace Tikal.Interaction
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool _interactionAvailable = true;
        [SerializeField] private string _interactionText = "Interactuar";

        public virtual bool CanInteract => isActiveAndEnabled && _interactionAvailable;
        public virtual string InteractionText => _interactionText;

        public void SetInteractionAvailable(bool available) => _interactionAvailable = available;
        public abstract void Interact();
    }
}
