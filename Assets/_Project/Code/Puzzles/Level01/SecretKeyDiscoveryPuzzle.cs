using Tikal.Interaction;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class SecretKeyDiscoveryPuzzle : PuzzleBase, IInteractable
    {
        public string InteractionText => "Examinar";
        public bool CanInteract => isActiveAndEnabled && (State==PuzzleState.Available || State==PuzzleState.Active);
        public void Interact()
        {
            if(!CanInteract)return;
            if(State==PuzzleState.Available&&!BeginPuzzle())return;
            TrySolve();
        }
    }
}