using System;
using UnityEngine;
namespace Tikal.Puzzles
{
    public abstract class PuzzleBase : MonoBehaviour
    {
        [SerializeField] private PuzzleDefinition _definition;
        [SerializeField, Tooltip("Only Inactive or Available are valid authored initial states.")] private PuzzleState _initialState = PuzzleState.Inactive;
        [SerializeField] private bool _allowReset = true;
        private PuzzleState _state = PuzzleState.Inactive;
        private bool _transitioning;
        private string _puzzleId = string.Empty;
        public string PuzzleId => _puzzleId;
        public PuzzleState State => _state;
        public bool IsSolved => _state == PuzzleState.Solved;
        public bool CanReset => _allowReset && _state == PuzzleState.Active && isActiveAndEnabled && !_transitioning;
        public event Action<PuzzleBase> PuzzleStarted;
        public event Action<PuzzleBase> PuzzleSolved;
        public event Action<PuzzleBase> PuzzleReset;

        protected void Awake()
        {
            if (_definition == null || string.IsNullOrWhiteSpace(_definition.Id))
            {
                Debug.LogError("Puzzle requires a definition with a stable ID.", this);
                enabled = false;
                return;
            }
            _puzzleId = _definition.Id;
            _state = _initialState == PuzzleState.Available ? PuzzleState.Available : PuzzleState.Inactive;
        }
        public bool MakeAvailable()
        {
            if (!CanTransition(PuzzleState.Inactive)) return false;
            _state = PuzzleState.Available;
            return true;
        }
        public bool BeginPuzzle()
        {
            if (!CanTransition(PuzzleState.Available)) return false;
            Transition(PuzzleState.Active, OnPuzzleBegan, PuzzleStarted);
            return true;
        }
        // Concrete conditions own completion; external systems cannot assign State or solve arbitrary puzzles.
        protected bool TrySolve()
        {
            if (!CanTransition(PuzzleState.Active)) return false;
            Transition(PuzzleState.Solved, OnPuzzleSolved, PuzzleSolved);
            return true;
        }
        public bool ResetPuzzle()
        {
            if (!CanReset) return false;
            Transition(PuzzleState.Available, OnPuzzleReset, PuzzleReset);
            return true;
        }
        public bool TryInteract(string elementId)
        {
            if (!CanTransition(PuzzleState.Active)) return false;
            OnPuzzleInteraction(elementId);
            return true;
        }
        private bool CanTransition(PuzzleState expected) => !_transitioning && isActiveAndEnabled && _state == expected;
        private void Transition(PuzzleState target, Action hook, Action<PuzzleBase> notification)
        {
            _transitioning = true;
            _state = target;
            try { hook(); notification?.Invoke(this); }
            finally { _transitioning = false; }
        }
        protected virtual void OnPuzzleBegan() { }
        protected virtual void OnPuzzleSolved() { }
        protected virtual void OnPuzzleReset() { }
        protected virtual void OnPuzzleInteraction(string elementId) { }
    }
}
