using System;
using Tikal.Interaction;
using UnityEngine;

namespace Tikal.Tutorial
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialExitGate : InteractableBase
    {
        [SerializeField] private Transform _movingStone;
        [SerializeField] private Transform _player;
        [SerializeField] private Vector3 _openOffset = new Vector3(0, 2.2f, 0);
        [SerializeField, Min(.01f)] private float _openSpeed = 2f;
        [SerializeField] private bool _exitAvailable;
        private Vector3 _closedPosition;
        private bool _opened;
        private bool _exitTriggered;

        public bool ExitAvailable => _exitAvailable;
        public bool HasPlayerExited => _exitTriggered;
        public bool HasRequiredReferences => _movingStone != null && _player != null && GetComponent<BoxCollider>() != null;
        public event Action Interacted;
        public event Action ExitTriggered;
        public override string InteractionText => _opened ? "Paso abierto" : "Abrir paso";

        private void Awake()
        {
            if (_movingStone == null) _movingStone = transform;
            _closedPosition = _movingStone.localPosition;
            var trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        public void SetExitAvailable(bool available) => _exitAvailable = available && _opened;

        public override void Interact()
        {
            if (!CanInteract || _opened) return;
            _opened = true;
            SetInteractionAvailable(false);
            Interacted?.Invoke();
        }

        private void Update()
        {
            if (_opened && _movingStone != null)
                _movingStone.localPosition = Vector3.MoveTowards(_movingStone.localPosition, _closedPosition + _openOffset, _openSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other) => TryTriggerExit(other);
        private void OnTriggerStay(Collider other) => TryTriggerExit(other);

        private void TryTriggerExit(Collider other)
        {
            if (!_exitAvailable || _exitTriggered || _player == null) return;
            if (other.transform != _player && !other.transform.IsChildOf(_player)) return;
            _exitTriggered = true;
            _exitAvailable = false;
            ExitTriggered?.Invoke();
        }
    }
}
