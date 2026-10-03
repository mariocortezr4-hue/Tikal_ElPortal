using UnityEngine;

namespace Tikal.Interaction
{
    public sealed class Door : InteractableBase
    {
        public enum DoorState { Closed, Opening, Open, Closing }

        [SerializeField] private Transform _pivot;
        [SerializeField, Range(-180, 180)] private float _openAngle = 90f;
        [SerializeField, Min(0.01f)] private float _openSpeed = 120f;
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        private Quaternion _closedRotation;
        private float _currentAngle;
        private bool _targetOpen;

        public DoorState State { get; private set; } = DoorState.Closed;
        public override bool CanInteract => base.CanInteract && _pivot != null;
        public override string InteractionText => _targetOpen ? "Cerrar" : "Abrir";

        private void Awake()
        {
            if (_pivot == null)
            {
                UnityEngine.Debug.LogError("Door requires a hinge pivot.", this);
                enabled = false;
                return;
            }
            _closedRotation = _pivot.localRotation;
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            _targetOpen = !_targetOpen;
            UpdateState();
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (_pivot == null) return;
            var target = _targetOpen ? _openAngle : 0f;
            if (Mathf.Approximately(_currentAngle, target)) return;
            _currentAngle = Mathf.MoveTowards(_currentAngle, target, _openSpeed * deltaTime);
            var axis = _rotationAxis.sqrMagnitude > 0.0001f ? _rotationAxis.normalized : Vector3.up;
            _pivot.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle, axis);
            UpdateState();
        }

        private void UpdateState()
        {
            var target = _targetOpen ? _openAngle : 0f;
            State = Mathf.Approximately(_currentAngle, target)
                ? (_targetOpen ? DoorState.Open : DoorState.Closed)
                : (_targetOpen ? DoorState.Opening : DoorState.Closing);
        }
    }
}
