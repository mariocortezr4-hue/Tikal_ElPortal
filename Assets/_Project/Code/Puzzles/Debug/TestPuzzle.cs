using UnityEngine;
namespace Tikal.Puzzles.Debugging
{
    // Testing fixture only. Never attached to Level01 or a production puzzle anchor.
    public sealed class TestPuzzle : PuzzleBase
    {
        public int InteractionCount { get; private set; }
        public string LastElementId { get; private set; }
        public bool CompleteForTesting() => TrySolve();
        protected override void OnPuzzleInteraction(string elementId)
        {
            LastElementId = elementId;
            InteractionCount++;
            if (InteractionCount >= 2) TrySolve();
        }
        protected override void OnPuzzleReset() { InteractionCount = 0; LastElementId = null; }
    }
}
