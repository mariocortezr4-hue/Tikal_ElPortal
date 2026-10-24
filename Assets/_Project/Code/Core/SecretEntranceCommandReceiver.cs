using Tikal.Input;
using Tikal.Interaction;
using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class SecretEntranceCommandReceiver : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputReader;
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _target;
        [SerializeField] private BoxCollider _zone;
        [SerializeField] private Door _door = null;
        // Existing scenes retain this reference until canonical authoring binds _door.
        [SerializeField, HideInInspector] private Collider[] _movingLeaf;
        [SerializeField] private LevelOutcomeController _outcome;
        [SerializeField,Min(.1f)] private float _range = 3;
        private IPlayerInputReader _input;
        private bool _subscribed;
        public float Range => _range;
        public Door MainDoor
        {
            get
            {
                if (_door != null) return _door;
                if (_movingLeaf != null)
                    foreach (var leaf in _movingLeaf)
                        if (leaf != null && leaf.GetComponentInParent<Door>() is Door door) return door;
                return null;
            }
        }
        public bool IsConfigured => _inputReader is IPlayerInputReader && _player != null &&
            _target != null && _zone != null && MainDoor != null && _outcome != null;
        public bool IsPlayerInZone
        {
            get
            {
                if (_player == null || _zone == null || !_zone.enabled || !_zone.gameObject.activeInHierarchy || !_zone.isTrigger || _target == null) return false;
                var position = _player.position + Vector3.up;
                var local = _zone.transform.InverseTransformPoint(position) - _zone.center;
                var half = _zone.size * .5f;
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z &&
                    (position - _target.position).sqrMagnitude <= _range * _range;
            }
        }
        public bool CanExecuteCommand => isActiveAndEnabled && IsConfigured && _input != null &&
            _input.GameplayEnabled && _outcome.State == LevelOutcome.Playing &&
            MainDoor.isActiveAndEnabled && MainDoor.State == Door.DoorState.Closed && IsPlayerInZone;
        private void Awake() => _input = _inputReader as IPlayerInputReader;
        private void OnEnable()
        {
            if(_input==null || !IsConfigured)
            { Debug.LogError("Secret entrance requires input, player, logical Door, stable target, zone and outcome references.",this); return; }
            if (_subscribed) return;
            _input.SecretCommandPressed += Receive;
            _subscribed=true;
        }
        private void OnDisable(){if(_subscribed)_input.SecretCommandPressed-=Receive;_subscribed=false;}
        private void Receive()
        {
            if (CanExecuteCommand) Tikal.Cinematics.Level01PlazaTransition.TriggerSecretVictory(_outcome);
        }
    }
}
