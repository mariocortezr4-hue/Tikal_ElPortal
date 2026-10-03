#if UNITY_EDITOR
using Tikal.Puzzles;
namespace Tikal.Tests.Support
{
    public sealed class PuzzleLifecycleProbe : PuzzleBase
    {
        public int BeginHooks { get; private set; }
        public int SolveHooks { get; private set; }
        public int ResetHooks { get; private set; }
        public bool Complete() => TrySolve();
        protected override void OnPuzzleBegan() { BeginHooks++; }
        protected override void OnPuzzleSolved() { SolveHooks++; }
        protected override void OnPuzzleReset() { ResetHooks++; }
        protected override void OnPuzzleInteraction(string _) { TrySolve(); }
    }
}
#endif
