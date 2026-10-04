using Tikal.Interaction;
using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class RevelationStele : MonoBehaviour, IInteractable
    {
        [SerializeField] private RevelationPuzzle _puzzle;
        [SerializeField] private string _patternId;
        [SerializeField] private Renderer[] _symbols;
        private MaterialPropertyBlock _block;
        public RevelationPuzzle Puzzle => _puzzle;
        public string PatternId => _patternId;
        public bool HasVisual => _symbols != null && _symbols.Length > 0 && System.Array.TrueForAll(_symbols, r => r != null);
        public bool IsSelected { get; private set; }
        public string InteractionText => "Activar";
        public bool CanInteract => isActiveAndEnabled && HasVisual && _puzzle != null && _puzzle.CanSelect(this);
        public void Interact() { if (CanInteract) _puzzle.Select(this); }
        internal void ShowSelection(bool selected, bool error)
        {
            IsSelected = selected;
            if (!HasVisual) return;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor("_EmissionColor", error ? new Color(.2f, .055f, .025f) : selected ? new Color(.65f, .38f, .1f) : Color.black);
            foreach (var symbol in _symbols) symbol.SetPropertyBlock(_block);
        }
    }
}