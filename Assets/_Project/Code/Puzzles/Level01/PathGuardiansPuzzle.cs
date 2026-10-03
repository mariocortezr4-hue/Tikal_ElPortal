using System.Collections.Generic;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class PathGuardiansPuzzle : PuzzleBase
    {
        [SerializeField] private PathGuardian[] _guardians = new PathGuardian[3];
        [SerializeField] private int[] _solution = new int[3];
        [SerializeField] private DestinationMarker[] _destinations = new DestinationMarker[3];
        private bool _valid;
        public int AlignedCount
        {
            get
            {
                if(!_valid) return 0;
                int count=0;
                for(int i=0;i<3;i++) if(_guardians[i].isActiveAndEnabled&&!_guardians[i].IsRotating&&_guardians[i].CurrentOrientation==_solution[i]) count++;
                return count;
            }
        }
        private double _beganAt;
        private double _solvedSeconds;
        public int RotationCount { get; private set; }
        public double ElapsedSeconds => IsSolved ? _solvedSeconds : State==PuzzleState.Active ? Time.timeAsDouble-_beganAt : 0;
        private void OnEnable()
        {
            _valid=ValidConfiguration();
            if(!_valid) { Debug.LogError("Guardians require three unique symbols, guardians, matching destinations and valid solution indices.",this); return; }
            foreach(var fragment in _guardians) fragment.RotationFinished+=CheckSolution;
            CheckSolution(null);
        }
        private void OnDisable()
        {
            if(_guardians!=null) foreach(var fragment in _guardians) if(fragment!=null) fragment.RotationFinished-=CheckSolution;
        }
        private bool ValidConfiguration()
        {
            if(_guardians==null||_solution==null||_guardians.Length!=3||_solution.Length!=3) return false;
            if(_destinations==null||_destinations.Length!=3) return false;
            var symbols=new HashSet<GuardianSymbol>();
            var uniqueMarkers=new HashSet<DestinationMarker>();
            for(int i=0;i<3;i++)
                if(_guardians[i]==null||!symbols.Add(_guardians[i].Symbol)||_destinations[i]==null||!uniqueMarkers.Add(_destinations[i])||_destinations[i].Symbol!=_guardians[i].Symbol) return false;
            var unique=new HashSet<PathGuardian>();
            for(int i=0;i<3;i++) if(_guardians[i]==null||!unique.Add(_guardians[i])||_solution[i]<0||_solution[i]>3) return false;
            return true;
        }
        internal bool CanRotate(PathGuardian fragment)
        {
            if(!_valid||!isActiveAndEnabled||(State!=PuzzleState.Available&&State!=PuzzleState.Active)) return false;
            foreach(var piece in _guardians) if(piece==fragment) return true;
            return false;
        }
        internal void Rotate(PathGuardian fragment)
        {
            if(!CanRotate(fragment)||fragment.IsRotating) return;
            if(State==PuzzleState.Available&&!BeginPuzzle()) return;
            fragment.StartRotation();
        }
        private void CheckSolution(PathGuardian changed)
        {
            if(!_valid||State!=PuzzleState.Active||!isActiveAndEnabled) return;
            if(changed!=null) RotationCount++;
            for(int i=0;i<3;i++)
                if(!_guardians[i].isActiveAndEnabled||_guardians[i].IsRotating||_guardians[i].CurrentOrientation!=_solution[i]) return;
            TrySolve();
        }
        protected override void OnPuzzleBegan() { RotationCount=0; _beganAt=Time.timeAsDouble; _solvedSeconds=0; }
        protected override void OnPuzzleSolved()
        {
            _solvedSeconds=Time.timeAsDouble-_beganAt;
            if(Debug.isDebugBuild) Debug.Log($"GUARDIANS_METRICS rotations={RotationCount} elapsedSeconds={_solvedSeconds:F2}",this);
        }
        protected override void OnPuzzleReset()
        {
            foreach(var fragment in _guardians) fragment.ResetOrientation();
            RotationCount=0; _solvedSeconds=0;
        }
    }
}
