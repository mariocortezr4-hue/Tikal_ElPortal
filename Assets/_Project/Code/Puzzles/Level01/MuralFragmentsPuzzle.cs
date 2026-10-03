using System.Collections.Generic;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class MuralFragmentsPuzzle : PuzzleBase
    {
        [SerializeField] private MuralFragment[] _fragments = new MuralFragment[4];
        [SerializeField] private int[] _solution = new int[4];
        private bool _valid;
        private double _beganAt;
        private double _solvedSeconds;
        public int RotationCount { get; private set; }
        public double ElapsedSeconds => IsSolved ? _solvedSeconds : State==PuzzleState.Active ? Time.timeAsDouble-_beganAt : 0;
        private void OnEnable()
        {
            _valid=ValidConfiguration();
            if(!_valid) { Debug.LogError("Mural requires four distinct fragments and four solution indices in [0,3].",this); return; }
            foreach(var fragment in _fragments) fragment.RotationFinished+=CheckSolution;
            CheckSolution(null);
        }
        private void OnDisable()
        {
            if(_fragments!=null) foreach(var fragment in _fragments) if(fragment!=null) fragment.RotationFinished-=CheckSolution;
        }
        private bool ValidConfiguration()
        {
            if(_fragments==null||_solution==null||_fragments.Length!=4||_solution.Length!=4) return false;
            var unique=new HashSet<MuralFragment>();
            for(int i=0;i<4;i++) if(_fragments[i]==null||!unique.Add(_fragments[i])||_solution[i]<0||_solution[i]>3) return false;
            return true;
        }
        internal bool CanRotate(MuralFragment fragment)
        {
            if(!_valid||!isActiveAndEnabled||(State!=PuzzleState.Available&&State!=PuzzleState.Active)) return false;
            foreach(var piece in _fragments) if(piece==fragment) return true;
            return false;
        }
        internal void Rotate(MuralFragment fragment)
        {
            if(!CanRotate(fragment)||fragment.IsRotating) return;
            if(State==PuzzleState.Available&&!BeginPuzzle()) return;
            fragment.StartRotation();
        }
        private void CheckSolution(MuralFragment changed)
        {
            if(!_valid||State!=PuzzleState.Active||!isActiveAndEnabled) return;
            if(changed!=null) RotationCount++;
            for(int i=0;i<4;i++)
                if(!_fragments[i].isActiveAndEnabled||_fragments[i].IsRotating||_fragments[i].CurrentOrientation!=_solution[i]) return;
            TrySolve();
        }
        protected override void OnPuzzleBegan() { RotationCount=0; _beganAt=Time.timeAsDouble; _solvedSeconds=0; }
        protected override void OnPuzzleSolved()
        {
            _solvedSeconds=Time.timeAsDouble-_beganAt;
            if(Debug.isDebugBuild) Debug.Log($"MURAL_METRICS rotations={RotationCount} elapsedSeconds={_solvedSeconds:F2}",this);
        }
        protected override void OnPuzzleReset()
        {
            foreach(var fragment in _fragments) fragment.ResetOrientation();
            RotationCount=0; _solvedSeconds=0;
        }
    }
}
