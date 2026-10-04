using Tikal.Interaction;
using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class GlyphStone : MonoBehaviour, IInteractable
    {
        [SerializeField] private GlyphSequencePuzzle _puzzle;
        [SerializeField, Range(1, 7)] private int _sequenceValue = 1;
        [SerializeField] private Renderer _visual;
        private MaterialPropertyBlock _block;
        public GlyphSequencePuzzle Puzzle => _puzzle;
        public int SequenceValue => _sequenceValue;
        public bool HasVisual => _visual != null;
        public bool IsActivated { get; private set; }
        public string InteractionText => "Activar";
        public bool CanInteract => isActiveAndEnabled && HasVisual && _puzzle != null && _puzzle.CanActivate(this);

        public void Interact()
        {
            if (CanInteract) _puzzle.Receive(this);
        }

        internal void SetActivated(bool activated)
        {
            IsActivated = activated;
            if (_visual == null) return;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor("_EmissionColor", activated ? new Color(.22f, .13f, .045f) : Color.black);
            _visual.SetPropertyBlock(_block);
        }

        private void OnValidate() => _sequenceValue = Mathf.Clamp(_sequenceValue, 1, 7);
    }
}
