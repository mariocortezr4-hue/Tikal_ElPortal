using System;
using Tikal.Input;
using Tikal.Player;
using UnityEngine;

namespace Tikal.Tutorial
{
    public enum TutorialStepState { Inactive, Active, Completed }
    public enum TutorialPromptAction { Move, Look, Jump, Crouch, Sprint, Interact }

    public abstract class TutorialStep : MonoBehaviour
    {
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _instruction;
        [SerializeField] private TutorialPromptAction _promptAction;
        public string Title => _title;
        public string Instruction => _instruction;
        public TutorialPromptAction PromptAction => _promptAction;
        public TutorialStepState State { get; private set; }
        public event Action<TutorialStep> Completed;

        internal void ActivateStep()
        {
            if (State != TutorialStepState.Inactive) return;
            State = TutorialStepState.Active;
            OnActivated();
        }

        internal void TickStep(float deltaTime)
        {
            if (State == TutorialStepState.Active) OnStepTick(deltaTime);
        }

        protected virtual void OnActivated() { }
        protected virtual void OnStepTick(float deltaTime) { }
        protected virtual void OnDeactivated() { }
        protected void Complete()
        {
            if (State != TutorialStepState.Active) return;
            State = TutorialStepState.Completed;
            OnDeactivated();
            Completed?.Invoke(this);
        }
    }

    public sealed class TutorialMovementStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField, Min(.5f)] private float _requiredDistance = 4f;
        private Vector3 _previousPosition;
        private float _travelled;
        protected override void OnActivated() => _previousPosition = _player.position;
        protected override void OnStepTick(float deltaTime)
        {
            var position = _player.position;
            var delta = position - _previousPosition;
            delta.y = 0;
            _travelled += Mathf.Min(delta.magnitude, 1f);
            _previousPosition = position;
            if (_travelled >= _requiredDistance) Complete();
        }
    }

    public sealed class TutorialLookStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerLook _look;
        [SerializeField, Min(10)] private float _requiredDegrees = 45f;
        private float _lastYaw;
        private float _lastPitch;
        private float _turned;
        protected override void OnActivated()
        {
            _lastYaw = _player.eulerAngles.y;
            _lastPitch = _look.Pitch;
        }
        protected override void OnStepTick(float deltaTime)
        {
            var yaw = _player.eulerAngles.y;
            var pitch = _look.Pitch;
            _turned += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) + Mathf.Abs(pitch - _lastPitch);
            _lastYaw = yaw;
            _lastPitch = pitch;
            if (_turned >= _requiredDegrees) Complete();
        }
    }

    public sealed class TutorialJumpStep : TutorialStep
    {
        [SerializeField] private PlayerInputReader _input;
        protected override void OnActivated() => _input.JumpPressed += OnJump;
        protected override void OnDeactivated() => _input.JumpPressed -= OnJump;
        private void OnJump() => Complete();
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialCrouchPassageStep : TutorialStep
    {
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private Transform _player;
        private bool _inside;
        protected override void OnActivated() => _inside = false;
        protected override void OnStepTick(float deltaTime)
        {
            if (_inside && _movement.IsCrouching) Complete();
        }
        private void OnTriggerEnter(Collider other) { if (other.transform == _player) _inside = true; }
        private void OnTriggerExit(Collider other) { if (other.transform == _player) _inside = false; }
    }

    public sealed class TutorialSprintStep : TutorialStep
    {
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField, Min(1)] private float _requiredDistance = 6f;
        private Vector3 _previousPosition;
        private float _travelled;
        protected override void OnActivated() => _previousPosition = _player.position;
        protected override void OnStepTick(float deltaTime)
        {
            var position = _player.position;
            var delta = position - _previousPosition;
            delta.y = 0;
            if (_movement.IsSprinting) _travelled += Mathf.Min(delta.magnitude, 1f);
            _previousPosition = position;
            if (_travelled >= _requiredDistance) Complete();
        }
    }

    public sealed class TutorialInteractStep : TutorialStep
    {
        [SerializeField] private TutorialExitGate _gate;
        protected override void OnActivated()
        {
            _gate.Interacted += OnInteracted;
            _gate.SetInteractionAvailable(true);
        }
        protected override void OnDeactivated() => _gate.Interacted -= OnInteracted;
        private void OnInteracted() => Complete();
    }
}
