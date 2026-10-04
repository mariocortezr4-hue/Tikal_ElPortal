using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class GlyphSequencePuzzle : PuzzleBase
    {
        [SerializeField] private GlyphStone[] _stones = new GlyphStone[7];
        private bool _valid;
        private bool _receiving;
        public int ExpectedNextValue { get; private set; } = 1;
        public int Progress => ExpectedNextValue - 1;
        public event Action<GlyphSequencePuzzle> ProgressChanged;
        public event Action<GlyphSequencePuzzle> WrongSelection;

        private void OnEnable()
        {
            _valid = ValidateConfiguration();
            if (!_valid) Debug.LogError("Glyph puzzle requires seven unique values 1-7 with valid stone references.", this);
            else RefreshStones();
        }

        private bool ValidateConfiguration()
        {
            if (_stones == null || _stones.Length != 7) return false;
            var values = new HashSet<int>();
            foreach (var stone in _stones)
                if (stone == null || stone.Puzzle != this || !stone.HasVisual ||
                    stone.SequenceValue < 1 || stone.SequenceValue > 7 || !values.Add(stone.SequenceValue))
                    return false;
            return values.Count == 7;
        }

        internal bool CanActivate(GlyphStone stone) => _valid && !_receiving && isActiveAndEnabled &&
            stone.isActiveAndEnabled && (State == PuzzleState.Available || State == PuzzleState.Active) &&
            Array.IndexOf(_stones, stone) >= 0;

        internal void Receive(GlyphStone stone)
        {
            if (!CanActivate(stone)) return;
            _receiving = true;
            try
            {
                if (State == PuzzleState.Available && !BeginPuzzle()) return;
                if (stone.SequenceValue != ExpectedNextValue)
                {
                    ClearProgress();
                    WrongSelection?.Invoke(this);
                    return;
                }
                ExpectedNextValue++;
                RefreshStones();
                if (Progress == 7) TrySolve();
                ProgressChanged?.Invoke(this);
            }
            finally { _receiving = false; }
        }

        private void RefreshStones()
        {
            foreach (var stone in _stones)
                stone.SetActivated(stone.SequenceValue < ExpectedNextValue);
        }

        private void ClearProgress()
        {
            ExpectedNextValue = 1;
            RefreshStones();
            ProgressChanged?.Invoke(this);
        }

        protected override void OnPuzzleReset() => ClearProgress();
    }
}
