using System;
using Tikal.Input;
using Tikal.Player;
using UnityEngine;

namespace Tikal.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("A component implementing IPlayerInputReader.")]
        [SerializeField] private MonoBehaviour _inputSource;
        [SerializeField] private Camera _viewCamera;
        [Tooltip("Stable logical chest target used for third-person reach, independent of visual bones.")]
        [SerializeField] private Transform _interactionOrigin = null;
        [SerializeField, Min(0)] private float _interactionDistance = 3f;
        [Tooltip("Include solid occluders as well as interactables to prevent interaction through walls.")]
        [SerializeField] private LayerMask _interactionLayers = ~0;

        private IPlayerInputReader _input;
        private PlayerCameraController _playerCamera;
        private CharacterController _capsule;
        private bool _subscribed;
        private int _pendingInteractions;
        private Collider _lastCollider;
        private IInteractable _resolved;
        private UnityEngine.Object _resolvedObject;

        public IInteractable CurrentInteractable { get; private set; }
        public Transform InteractionOrigin => _interactionOrigin;
        public Camera ViewCamera => _viewCamera;
        public bool UsesThirdPersonAim => _playerCamera != null && _playerCamera.OwnsLook;
        public bool HasResolvedAimPoint { get; private set; }
        public Vector3 ResolvedAimPoint { get; private set; }
        public float InteractionDistance => _interactionDistance;
        public bool HasThirdPersonConfiguration => _interactionOrigin != null && _interactionOrigin.IsChildOf(transform);
        public event Action<IInteractable> FocusChanged;

        private void Awake()
        {
            _input = _inputSource as IPlayerInputReader;
            _playerCamera = GetComponent<PlayerCameraController>();
            _capsule = GetComponent<CharacterController>();
            if (_input == null || _viewCamera == null)
            {
                UnityEngine.Debug.LogError("PlayerInteraction requires an IPlayerInputReader and a view camera.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_input == null || _subscribed) return;
            _input.InteractPressed += OnInteractPressed;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _input.InteractPressed -= OnInteractPressed;
                _subscribed = false;
            }
            ClearFocusForRespawn();
        }

        public void ClearFocusForRespawn()
        {
            _pendingInteractions = 0;
            _lastCollider = null;
            _resolved = null;
            _resolvedObject = null;
            HasResolvedAimPoint = false;
            ResolvedAimPoint = Vector3.zero;
            SetFocus(null);
        }

        // Runs after the player's movement/look so focus uses the final camera pose.
        private void LateUpdate()
        {
            RefreshFocus();
            var requests = _pendingInteractions;
            _pendingInteractions = 0;
            for (var index = 0; index < requests; index++)
            {
                if (!isActiveAndEnabled || !_input.GameplayEnabled || !IsAvailable(CurrentInteractable)) break;
                CurrentInteractable.Interact();
            }
        }

        private void RefreshFocus()
        {
            HasResolvedAimPoint = false;
            ResolvedAimPoint = Vector3.zero;
            if (_input == null || !_input.GameplayEnabled || _viewCamera == null)
            {
                SetFocus(null);
                return;
            }
            if (!TryGetInteractionHit(out var hit))
            {
                _lastCollider = null;
                _resolved = null;
                _resolvedObject = null;
                SetFocus(null);
                return;
            }
            if (hit.collider != _lastCollider || (_resolved != null && _resolvedObject == null))
            {
                _lastCollider = hit.collider;
                _resolved = hit.collider.GetComponentInParent<IInteractable>();
                _resolvedObject = _resolved as UnityEngine.Object;
            }
            SetFocus(IsAvailable(_resolved) ? _resolved : null);
        }

        private bool TryGetInteractionHit(out RaycastHit hit)
        {
            if (_playerCamera == null || !_playerCamera.OwnsLook)
            {
                // Preserve the approved FPS ray, range, layers and trigger behavior exactly.
                var ray = new Ray(_viewCamera.transform.position, _viewCamera.transform.forward);
                return Physics.Raycast(ray, out hit, _interactionDistance, _interactionLayers, QueryTriggerInteraction.Ignore);
            }

            hit = default;
            if (!HasThirdPersonConfiguration || _interactionDistance <= 0) return false;
            var origin = GetInteractionPosition();
            // An origin embedded in world geometry must not raycast out through that solid.
            foreach (var overlap in Physics.OverlapSphere(origin, .001f, _interactionLayers, QueryTriggerInteraction.Ignore))
                if (!overlap.transform.IsChildOf(transform)) return false;

            var aimRay = _viewCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            // This is only a visual search bound: all points within player reach are included,
            // regardless of camera distance. It never increases the physical interaction range.
            var aimDistance = Vector3.Distance(aimRay.origin, origin) + _interactionDistance;
            if (!TryNearestWorldHit(aimRay, aimDistance, out var aimHit)) return false;
            // Publish the existing visual hit even when reach rejects interaction.
            SetResolvedAimPoint(aimHit.point);
            var candidate = aimHit.collider.GetComponentInParent<IInteractable>();
            if (candidate == null) return false;
            var towardAim = aimHit.point - origin;
            if (towardAim.sqrMagnitude <= 0 || towardAim.sqrMagnitude > _interactionDistance * _interactionDistance)
                return false;
            // Free orbit does not give permission to interact behind the character's facing.
            if (Vector3.Dot(transform.forward, towardAim) < 0) return false;
            var reachRay = new Ray(origin, towardAim.normalized);
            if (!TryNearestWorldHit(reachRay, _interactionDistance, out hit)) return false;
            // The first physical hit wins visually too, including a blocking wall.
            SetResolvedAimPoint(hit.point);
            // First solid hit must belong to the visually selected interactable. Walls and
            // other interactables block it; the prompt cannot select a different hidden target.
            return ReferenceEquals(hit.collider.GetComponentInParent<IInteractable>(), candidate);
        }

        private void SetResolvedAimPoint(Vector3 point)
        {
            ResolvedAimPoint = point;
            HasResolvedAimPoint = true;
        }

        private Vector3 GetInteractionPosition()
        {
            var position = _interactionOrigin.position;
            if (_capsule == null) return position;
            // Keep the logical chest below the capsule's top while crouching, without
            // modifying the authored target or any movement/crouch settings.
            var local = transform.InverseTransformPoint(position);
            local.y = Mathf.Min(local.y, _capsule.center.y + _capsule.height * .5f - _capsule.radius);
            return transform.TransformPoint(local);
        }

        private bool TryNearestWorldHit(Ray ray, float distance, out RaycastHit nearest)
        {
            nearest = default;
            var found = false;
            foreach (var hit in Physics.RaycastAll(ray, distance, _interactionLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (found && hit.distance >= nearest.distance) continue;
                nearest = hit;
                found = true;
            }
            return found;
        }

        private bool IsAvailable(IInteractable interactable) =>
            interactable != null && _resolvedObject != null && interactable.CanInteract;

        private void SetFocus(IInteractable interactable)
        {
            if (ReferenceEquals(CurrentInteractable, interactable)) return;
            CurrentInteractable = interactable;
            FocusChanged?.Invoke(interactable);
        }

        private void OnInteractPressed()
        {
            // Consume input after movement/look and the frame's raycast, avoiding stale focus.
            if (isActiveAndEnabled && _input.GameplayEnabled) _pendingInteractions++;
        }
    }
}
