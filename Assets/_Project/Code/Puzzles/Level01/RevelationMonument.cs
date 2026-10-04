using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class RevelationMonument : MonoBehaviour
    {
        [SerializeField] private RevelationPuzzle _puzzle;
        [SerializeField] private Transform _panel;
        [SerializeField] private Renderer _interior;
        private Vector3 _closedPosition;
        private bool _subscribed;
        public bool IsRevealed { get; private set; }
        private void Awake() { if (_panel != null) _closedPosition = _panel.localPosition; }
        private void OnEnable()
        {
            if (_puzzle == null || _panel == null || _interior == null)
            { Debug.LogError("Revelation monument requires puzzle, panel and interior.", this); return; }
            _puzzle.PuzzleSolved += Reveal;
            _subscribed = true;
            if (_puzzle.IsSolved) Reveal(_puzzle);
        }
        private void Reveal(PuzzleBase _)
        {
            if (IsRevealed) return;
            IsRevealed = true;
            _panel.localPosition = _closedPosition + Vector3.up * 1.15f;
            var block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", new Color(.65f, .35f, .1f));
            _interior.SetPropertyBlock(block);
        }
        private void OnDisable() { if (_subscribed && _puzzle != null) _puzzle.PuzzleSolved -= Reveal; _subscribed = false; }
    }
}