using Tikal.Interaction;
using UnityEngine;
namespace Tikal.Puzzles
{
    [DisallowMultipleComponent]
    public sealed class PuzzleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleBase _puzzle;
        [SerializeField] private string _elementId = "default";
        [SerializeField] private string _beginText = "Iniciar";
        [SerializeField] private string _activeText = "Interactuar";
        public bool CanInteract => isActiveAndEnabled && _puzzle != null && _puzzle.isActiveAndEnabled &&
            (_puzzle.State == PuzzleState.Available || _puzzle.State == PuzzleState.Active);
        public string InteractionText => _puzzle != null && _puzzle.State == PuzzleState.Available ? _beginText : _activeText;
        public void Interact()
        {
            if (!CanInteract) return;
            if (_puzzle.State == PuzzleState.Available) _puzzle.BeginPuzzle();
            else _puzzle.TryInteract(_elementId);
        }
    }
}
