#if UNITY_EDITOR
using Tikal.Interaction;
namespace Tikal.Tests.Support
{
    public sealed class InteractionTestProbe : InteractableBase
    {
        public int InteractionCount { get; private set; }
        public override void Interact()
        {
            if (CanInteract) InteractionCount++;
        }
    }
}
#endif
