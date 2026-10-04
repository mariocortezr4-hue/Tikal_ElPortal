using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class RevelationPuzzle : PuzzleBase
    {
        [SerializeField] private RevelationStele[] _steles = new RevelationStele[4];
        private RevelationStele _first;
        private bool _valid, _evaluating;
        private Coroutine _error;
        public bool IsEvaluating => _evaluating;
        public event Action<RevelationPuzzle> SelectionChanged;
        public int SelectionCount { get; private set; }
        public int IncorrectAttempts { get; private set; }
        public float SecondsToSolve { get; private set; }
        private float _startedAt = -1;
        private void OnEnable()
        {
            _valid = _steles != null && _steles.Length == 4 &&
                _steles.All(s => s != null && s.Puzzle == this && s.HasVisual && !string.IsNullOrWhiteSpace(s.PatternId)) &&
                _steles.Distinct().Count() == 4 &&
                _steles.GroupBy(s => s.PatternId, StringComparer.Ordinal).Select(g => g.Count()).OrderBy(n => n).SequenceEqual(new[] { 1, 1, 2 });
            if (!_valid) Debug.LogError("Revelation requires four owned steles with exactly one logical matching pair.", this);
        }
        internal bool CanSelect(RevelationStele stele) => _valid && !_evaluating && stele != null &&
            isActiveAndEnabled && stele.isActiveAndEnabled && !stele.IsSelected &&
            (State == PuzzleState.Available || State == PuzzleState.Active) && Array.IndexOf(_steles, stele) >= 0;
        internal void Select(RevelationStele stele)
        {
            if (!CanSelect(stele)) return;
            _evaluating = true;
            try
            {
                if (State == PuzzleState.Available && !BeginPuzzle()) return;
                if (Application.isEditor || Debug.isDebugBuild)
                {
                    if (_startedAt < 0) _startedAt = Time.realtimeSinceStartup;
                    SelectionCount++;
                }
                stele.ShowSelection(true, false);
                if (_first == null)
                {
                    _first = stele;
                    SelectionChanged?.Invoke(this);
                    return;
                }
                if (string.Equals(_first.PatternId, stele.PatternId, StringComparison.Ordinal))
                {
                    if (Application.isEditor || Debug.isDebugBuild)
                    {
                        SecondsToSolve = Time.realtimeSinceStartup - _startedAt;
                        Debug.Log($"REVELATION_METRIC selections={SelectionCount} incorrect={IncorrectAttempts} seconds={SecondsToSolve:F3}");
                    }
                    TrySolve();
                    SelectionChanged?.Invoke(this);
                }
                else
                {
                    if (Application.isEditor || Debug.isDebugBuild)
                    {
                        IncorrectAttempts++;
                    }
                    _first.ShowSelection(true, true);
                    stele.ShowSelection(true, true);
                    _error = StartCoroutine(ClearWrongPair());
                    SelectionChanged?.Invoke(this);
                }
            }
            finally { if (_error == null) _evaluating = false; }
        }
        private IEnumerator ClearWrongPair()
        {
            yield return new WaitForSecondsRealtime(.18f);
            _error = null;
            ClearSelection();
        }
        private void ClearSelection()
        {
            foreach (var stele in _steles) if (stele != null) stele.ShowSelection(false, false);
            _first = null;
            _evaluating = false;
            SelectionChanged?.Invoke(this);
        }
        protected override void OnPuzzleReset()
        {
            if (_error != null) StopCoroutine(_error);
            _error = null;
            ClearSelection();
        }
        private void OnDisable()
        {
            if (_error != null) StopCoroutine(_error);
            _error = null;
            if (_valid && !IsSolved) ClearSelection();
        }
    }
}