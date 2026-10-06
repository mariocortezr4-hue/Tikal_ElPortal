using System;
using Tikal.Interaction;
using UnityEngine;

namespace Tikal.Tutorial
{
    public sealed class TutorialExitGate : InteractableBase
    {
        [SerializeField] private Transform _movingStone;
        [SerializeField] private Vector3 _openOffset = new Vector3(0, 2.2f, 0);
        [SerializeField, Min(.01f)] private float _openSpeed = 2f;
        private Vector3 _closedPosition;
        private bool _opened;
        public event Action Interacted;
        public override string InteractionText => _opened ? "Paso abierto" : "Abrir paso";

        private void Awake()
        {
            if (_movingStone == null) _movingStone = transform;
            _closedPosition = _movingStone.localPosition;
        }

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
    }
}
