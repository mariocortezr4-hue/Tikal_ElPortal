using System;
using System.Collections;
using Tikal.Interaction;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class PathGuardian : MonoBehaviour, IInteractable
    {
        [SerializeField] private PathGuardiansPuzzle _puzzle;
        [SerializeField] private Transform _visual;
        [SerializeField] private GuardianSymbol _symbol;
        public GuardianSymbol Symbol => _symbol;
        [SerializeField, Range(0,3)] private int _initialOrientation;
        [SerializeField, Range(.30f,.40f)] private float _rotationDuration = .35f;
        private Coroutine _rotation;
        private int _targetOrientation;
        private int _rotationVersion;
        private float _elapsed;
        public int CurrentOrientation { get; private set; }
        public bool IsRotating { get; private set; }
        public bool CanInteract => isActiveAndEnabled && _visual != null && _puzzle != null && _puzzle.CanRotate(this) && !IsRotating;
        public string InteractionText => "Girar";
        public event Action<PathGuardian> OrientationChanged;
        public event Action<PathGuardian> RotationFinished;
        public event Action<PathGuardian> RotationStarted;
        private void Awake() => ResetOrientation();
        public void Interact() { if(CanInteract) _puzzle.Rotate(this); }
        // Owner is the only authority that starts a turn. Index commits at its end.
        internal bool StartRotation()
        {
            if(!CanInteract) return false;
            Cancel();
            _targetOrientation=(CurrentOrientation+1)%4;
            _elapsed=0;
            IsRotating=true;
            RotationStarted?.Invoke(this);
            int version=++_rotationVersion;
            _rotation=StartCoroutine(Animate(version));
            return true;
        }
        private IEnumerator Animate(int version)
        {
            while(IsRotating && version==_rotationVersion)
            {
                yield return null;
                if(version==_rotationVersion) AdvanceRotation(Time.deltaTime);
            }
            if(version==_rotationVersion) _rotation=null;
        }
        private void AdvanceRotation(float deltaTime)
        {
            if(!IsRotating) return;
            _elapsed+=Mathf.Max(0,deltaTime);
            float t=Mathf.Clamp01(_elapsed/Mathf.Clamp(_rotationDuration,.30f,.40f));
            float eased=t*t*(3-2*t);
            _visual.localRotation=Quaternion.AngleAxis((CurrentOrientation+eased)*90f,Vector3.up);
            if(t<1) return;
            CurrentOrientation=_targetOrientation;
            IsRotating=false;
            Snap();
            OrientationChanged?.Invoke(this);
            RotationFinished?.Invoke(this);
        }
        internal void ResetOrientation()
        {
            Cancel(); CurrentOrientation=Mathf.Clamp(_initialOrientation,0,3); Snap();
        }
        private void Cancel()
        {
            if(_rotation!=null) StopCoroutine(_rotation);
            _rotation=null; _rotationVersion++; IsRotating=false;
        }
        private void Snap() { if(_visual!=null) _visual.localRotation=Quaternion.AngleAxis(CurrentOrientation*90f,Vector3.up); }
        private void OnDisable() { Cancel(); Snap(); }
    }
}
