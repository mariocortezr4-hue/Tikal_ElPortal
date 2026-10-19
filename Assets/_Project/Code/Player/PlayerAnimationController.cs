using Tikal.Core;
using UnityEngine;

namespace Tikal.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerAnimationController : MonoBehaviour, IRespawnResettable
    {
        private PlayerMovement _movement;
        private Animator _animator;
        private CheckpointManager _checkpoints;
        private bool _wasGrounded = true, _wasVisible, _resetPending;
        private float _launchVelocity = 1f, _landRemaining;
        private const float LandSeconds = .18f;
        public Animator CurrentAnimator => _animator;

        private void Awake() => _movement = GetComponent<PlayerMovement>();
        private void Start()
        {
            _checkpoints = FindAnyObjectByType<CheckpointManager>();
            if (_checkpoints != null) _checkpoints.RespawnCompleted += ResetForRespawn;
        }
        private void OnDestroy()
        {
            if (_checkpoints != null) _checkpoints.RespawnCompleted -= ResetForRespawn;
        }
        public void Attach(Animator animator)
        {
            if (_animator == animator) return;
            _animator = animator;
            if (_animator != null) _animator.applyRootMotion = false;
            ResetForRespawn();
        }
        public void OnPlayerRespawn() => ResetForRespawn();
        public void ResetForRespawn()
        {
            _landRemaining = 0;
            _wasGrounded = true;
            _wasVisible = false;
            _resetPending = true;
        }
        private void LateUpdate()
        {
            if (_movement == null || _animator == null || _animator.runtimeAnimatorController == null) return;
            var grounded = _movement.IsGrounded;
            if (_wasGrounded && !grounded) _launchVelocity = Mathf.Max(1f, _movement.VerticalVelocity);
            if (!_wasGrounded && grounded) _landRemaining = LandSeconds;
            _wasGrounded = grounded;
            _landRemaining = Mathf.Max(0, _landRemaining - Time.deltaTime);
            var visible = _animator.gameObject.activeInHierarchy;
            if (!visible) { _wasVisible = false; return; }
            if (_resetPending || !_wasVisible)
            {
                _animator.Rebind();
                _resetPending = false;
            }
            var speed = Mathf.Clamp01(_movement.CurrentSpeed / Mathf.Max(.01f, _movement.SprintSpeed));
            _animator.SetFloat("Speed", speed);
            _animator.SetBool("IsGrounded", grounded);
            _animator.SetBool("IsCrouching", _movement.IsCrouching);
            _animator.SetFloat("VerticalVelocity", _movement.VerticalVelocity);
            _animator.SetBool("IsLanding", grounded && _landRemaining > 0 && !_movement.IsCrouching);
            // One real full-jump clip: the physical state selects its ascent/fall/landing pose.
            var phase = grounded ? 1f - .25f * _landRemaining / LandSeconds
                : _movement.VerticalVelocity > 0 ? .35f + .15f * (1f - Mathf.Clamp01(_movement.VerticalVelocity / _launchVelocity))
                : .5f + .25f * Mathf.Clamp01(-_movement.VerticalVelocity / _launchVelocity);
            _animator.SetFloat("JumpPhase", phase);
            _animator.SetFloat("CrouchPose", .25f);
            if (!_wasVisible) _animator.Update(0);
            _wasVisible = true;
        }
    }
}
