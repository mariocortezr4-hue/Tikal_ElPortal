using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class ObservationAlignmentPuzzle : PuzzleBase
    {
        [SerializeField] private ObservationPoint _point;
        [SerializeField] private Transform _target;
        [SerializeField] private Renderer[] _fragments;
        [SerializeField, Range(.9f,1)] private float _minimumDot = .97f;
        [SerializeField, Min(.1f)] private float _holdSeconds = .35f;
        private float _held, _pulse, _lastGlow = -1;
        private MaterialPropertyBlock _block;
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        public float MinimumDot => _minimumDot;
        public float HoldSeconds => _holdSeconds;
        private void OnEnable()
        {
            if(_point == null || _point.Camera == null || _target == null || _fragments == null || _fragments.Length == 0)
            {Debug.LogError("Observation requires point, camera, target and visual fragments.",this);enabled=false;}
        }
        private void LateUpdate() => Evaluate(Time.deltaTime);
        private void Evaluate(float deltaTime)
        {
            if(IsSolved)
            {
                if(_pulse>0){_pulse=Mathf.Max(0,_pulse-deltaTime);Glow(.2f+.6f*_pulse/.6f);}
                return;
            }
            if(!isActiveAndEnabled || !_point.IsOccupied || (State != PuzzleState.Available && State != PuzzleState.Active))
            {_held=0;Glow(0);return;}
            var camera=_point.Camera.transform;
            var direction=_target.position-camera.position;
            float dot=direction.sqrMagnitude>.001f?Vector3.Dot(camera.forward,direction.normalized):-1;
            Glow(Mathf.Clamp01((dot-.9f)/.1f)*.15f);
            if(dot<_minimumDot){_held=0;return;}
            if(State==PuzzleState.Available&&!BeginPuzzle())return;
            _held+=Mathf.Max(0,deltaTime);
            if(_held>=_holdSeconds&&TrySolve()){_pulse=.6f;Glow(.8f);}
        }
        private void Glow(float strength)
        {
            if(Mathf.Abs(strength-_lastGlow)<.002f)return;
            _lastGlow=strength;_block ??=new MaterialPropertyBlock();
            _block.SetColor(Emission,new Color(.8f,.64f,.35f)*strength);
            foreach(var visual in _fragments)if(visual!=null)visual.SetPropertyBlock(_block);
        }
        protected override void OnPuzzleReset(){_held=0;Glow(0);}
        private void OnDisable(){_held=0;}
    }
}