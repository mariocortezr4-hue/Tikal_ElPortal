using Tikal.Input;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class ObservationPoint : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private BoxCollider _zone;
        [SerializeField] private MonoBehaviour _inputReader;
        private IPlayerInputReader _input;
        public Camera Camera => _camera;
        public bool IsOccupied => isActiveAndEnabled && _input != null && _input.GameplayEnabled &&
            _player != null && _zone != null && _zone.enabled && _zone.gameObject.activeInHierarchy &&
            _zone.bounds.Contains(_player.position + Vector3.up);
        private void Awake() => _input = _inputReader as IPlayerInputReader;
    }
}