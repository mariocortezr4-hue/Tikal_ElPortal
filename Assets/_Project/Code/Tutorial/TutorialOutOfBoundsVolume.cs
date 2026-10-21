using Tikal.Interaction;
using Tikal.Player;
using UnityEngine;

namespace Tikal.Tutorial
{
    // Local safety only: no checkpoint service, persistence or Tutorial progression calls.
    [DefaultExecutionOrder(-200), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialOutOfBoundsVolume : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Vector3 _spawnPosition;
        [SerializeField] private Vector3 _spawnEuler;
        [SerializeField] private Vector2 _allowedX = new Vector2(-7.5f, 7.5f);
        [SerializeField] private Vector2 _allowedZ = new Vector2(-6.8f, 51f);
        [SerializeField] private float _minimumY = -2f;
        private CharacterController _capsule;
        private PlayerMovement _movement;
        private PlayerLook _look;
        private PlayerCameraController _camera;
        private Vector3 _safePosition;
        private Quaternion _safeRotation;
        private int _recoverFrame = -1;
        public int RecoveryCount { get; private set; }
        public Vector3 SafePosition => _safePosition;

        private void Awake()
        {
            _safePosition = _spawnPosition; _safeRotation = Quaternion.Euler(_spawnEuler);
            if (_player == null) { enabled = false; return; }
            _capsule = _player.GetComponent<CharacterController>();
            _movement = _player.GetComponent<PlayerMovement>();
            _look = _player.GetComponent<PlayerLook>();
            _camera = _player.GetComponent<PlayerCameraController>();
        }
        private bool Outside(Vector3 p) => p.y < _minimumY || p.x < _allowedX.x || p.x > _allowedX.y || p.z < _allowedZ.x || p.z > _allowedZ.y;
        private void Update()
        {
            // Runs before Tutorial and movement Update so a fall/teleport is not counted as progress.
            if (_player != null && Outside(_player.transform.position)) Recover();
        }
        private void LateUpdate()
        {
            if (_player == null || _capsule == null || !_capsule.enabled || !_capsule.isGrounded || _recoverFrame == Time.frameCount) return;
            var p = _player.transform.position;
            if (Mathf.Abs(p.x) < 6.1f && p.z > -5 && p.z < 46 && p.y > -.1f && p.y < 1)
            {
                _safePosition = p;
                _safeRotation = _player.transform.rotation;
            }
        }
        private void OnTriggerEnter(Collider other) => CheckTrigger(other);
        private void OnTriggerStay(Collider other) => CheckTrigger(other);
        private void CheckTrigger(Collider other)
        {
            if (_player != null && (other.transform == _player.transform || other.transform.IsChildOf(_player.transform)) && Outside(_player.transform.position)) Recover();
        }
        private void Recover()
        {
            if (_capsule == null || !_capsule.enabled || _recoverFrame == Time.frameCount) return;
            _recoverFrame = Time.frameCount;
            _player.GetComponent<PlayerInteraction>()?.ClearFocusForRespawn();
            _capsule.enabled = false;
            try
            {
                _player.transform.SetPositionAndRotation(_safePosition, _safeRotation);
                _movement.ResetForRespawn();
                _look.ResetAfterRespawn(_movement.EyeHeight, _movement.MaximumEyeHeight);
            }
            finally { _capsule.enabled = true; }
            Physics.SyncTransforms();
            _camera?.SnapAfterTeleport();
            _player.GetComponent<PlayerAnimationController>()?.ResetForRespawn();
            RecoveryCount++;
        }
    }
}
