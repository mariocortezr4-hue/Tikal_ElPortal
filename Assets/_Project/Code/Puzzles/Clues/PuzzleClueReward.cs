using UnityEngine;
namespace Tikal.Puzzles.Clues
{
    [DisallowMultipleComponent]
    public sealed class PuzzleClueReward : MonoBehaviour
    {
        [SerializeField] private PuzzleBase _puzzle;
        [SerializeField] private ClueTracker _tracker;
        [SerializeField] private ClueDefinition _clue;
        private bool _subscribed;
        private void OnEnable()
        {
            if (_subscribed) return;
            if (_puzzle == null || _tracker == null || _clue == null)
            {
                Debug.LogError("PuzzleClueReward requires puzzle, tracker and clue references.", this);
                return;
            }
            _puzzle.PuzzleSolved += OnSolved;
            _subscribed = true;
            // Catch up after being disabled during completion. Tracker de-duplicates by stable ID.
            if (_puzzle.IsSolved) OnSolved(_puzzle);
        }
        private void OnDisable()
        {
            if (_subscribed && _puzzle != null) _puzzle.PuzzleSolved -= OnSolved;
            _subscribed = false;
        }
        private void OnSolved(PuzzleBase _) { if (_tracker != null) _tracker.Discover(_clue); }
    }
}
