using UnityEngine;

namespace Tikal.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Horizontal movement")]
        [SerializeField, Min(0)] private float _walkSpeed = 4f;
        [SerializeField, Min(0)] private float _sprintSpeed = 6.5f;
        [SerializeField, Min(0)] private float _crouchSpeed = 2f;
        [SerializeField, Min(0)] private float _acceleration = 28f;
        [SerializeField, Min(0)] private float _deceleration = 32f;
        [SerializeField, Range(0, 1)] private float _airControl = 0.35f;

        [Header("Vertical movement")]
        [SerializeField, Min(0)] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -24f;
        [SerializeField, Min(0)] private float _groundStickSpeed = 2f;
        [SerializeField, Min(0)] private float _terminalFallSpeed = 55f;

        [Header("Crouch and eye height")]
        [SerializeField, Min(0.1f)] private float _standingHeight = 1.8f;
        [SerializeField, Min(0.1f)] private float _crouchingHeight = 1f;
        [SerializeField, Min(0)] private float _heightChangeSpeed = 4f;
        [SerializeField, Min(0)] private float _standingEyeHeight = 1.62f;
        [SerializeField, Min(0)] private float _crouchingEyeHeight = 0.85f;
        [SerializeField, Min(0)] private float _standingStepOffset = 0.3f;
        [SerializeField, Min(0)] private float _crouchingStepOffset = 0.15f;
        [SerializeField] private LayerMask _headroomMask = ~0;
        [SerializeField, Min(0)] private float _headroomClearance = 0.03f;

        private readonly Collider[] _headroomHits = new Collider[16];
        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _jumpRequested;

        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching => _controller != null && !Mathf.Approximately(_controller.height, _standingHeight);
        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public float VerticalVelocity => _verticalVelocity;
        public float EyeHeight => _controller == null ? _standingEyeHeight : Mathf.Lerp(
            _crouchingEyeHeight, _standingEyeHeight,
            Mathf.InverseLerp(_crouchingHeight, _standingHeight, _controller.height));

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            ApplyHeight(_standingHeight);
        }

        public void RequestJump() => _jumpRequested = true;

        public void StopMotion()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0;
            _jumpRequested = false;
            IsSprinting = false;
        }

        public void Tick(Vector2 move, bool sprintHeld, bool crouchHeld, float deltaTime)
        {
            if (_controller == null || !_controller.enabled || deltaTime <= 0)
                return;

            IsGrounded = _controller.isGrounded && _verticalVelocity <= 0;
            UpdateCrouch(crouchHeld, deltaTime);
            IsSprinting = sprintHeld && IsGrounded && !IsCrouching && move.sqrMagnitude > 0;
            var speed = IsCrouching ? _crouchSpeed : IsSprinting ? _sprintSpeed : _walkSpeed;
            var clampedMove = Vector2.ClampMagnitude(move, 1f);
            var targetVelocity = (transform.right * clampedMove.x + transform.forward * clampedMove.y) * speed;
            var slowingDown = targetVelocity.sqrMagnitude < _horizontalVelocity.sqrMagnitude;
            var response = slowingDown ? _deceleration : _acceleration;
            if (!IsGrounded)
                response *= _airControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, response * deltaTime);

            if (IsGrounded)
                _verticalVelocity = -_groundStickSpeed;
            if (_jumpRequested && IsGrounded && !IsCrouching)
            {
                _verticalVelocity = Mathf.Sqrt(-2f * _gravity * _jumpHeight);
                IsGrounded = false;
                IsSprinting = false;
            }
            _jumpRequested = false;
            _verticalVelocity = Mathf.Max(_verticalVelocity + _gravity * deltaTime, -_terminalFallSpeed);

            var collisions = _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0)
                _verticalVelocity = 0;
            IsGrounded = (collisions & CollisionFlags.Below) != 0 && _verticalVelocity <= 0;
            if (IsGrounded)
                _verticalVelocity = -_groundStickSpeed;
        }

        private void UpdateCrouch(bool crouchHeld, float deltaTime)
        {
            var mustStayCrouched = crouchHeld || (IsCrouching && !CanStand());
            var targetHeight = mustStayCrouched ? _crouchingHeight : _standingHeight;
            ApplyHeight(Mathf.MoveTowards(_controller.height, targetHeight, _heightChangeSpeed * deltaTime));
        }

        private void ApplyHeight(float height)
        {
            _controller.height = height;
            _controller.center = new Vector3(0, height * 0.5f, 0);
            _controller.stepOffset = Mathf.Min(
                IsCrouching ? _crouchingStepOffset : _standingStepOffset,
                Mathf.Max(0, height - _controller.radius * 2f));
        }

        private bool CanStand()
        {
            var radius = _controller.radius;
            var bottom = transform.position + Vector3.up * (radius + _headroomClearance);
            var top = transform.position + Vector3.up * (_standingHeight - radius + _headroomClearance);
            var count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _headroomHits,
                _headroomMask, QueryTriggerInteraction.Ignore);
            if (count == _headroomHits.Length)
                return false;
            for (var index = 0; index < count; index++)
            {
                var hit = _headroomHits[index];
                if (hit != null && hit != _controller && !hit.transform.IsChildOf(transform))
                    return false;
            }
            return true;
        }

        private void OnValidate()
        {
            _gravity = Mathf.Min(_gravity, -0.01f);
            _sprintSpeed = Mathf.Max(_sprintSpeed, _walkSpeed);
            var capsule = GetComponent<CharacterController>();
            var minimumHeight = capsule == null ? 0.1f : capsule.radius * 2f + capsule.skinWidth;
            _standingHeight = Mathf.Max(_standingHeight, minimumHeight);
            _crouchingHeight = Mathf.Clamp(_crouchingHeight, minimumHeight, _standingHeight);
            _standingEyeHeight = Mathf.Clamp(_standingEyeHeight, 0, _standingHeight);
            _crouchingEyeHeight = Mathf.Clamp(_crouchingEyeHeight, 0, _crouchingHeight);
        }
    }
}
