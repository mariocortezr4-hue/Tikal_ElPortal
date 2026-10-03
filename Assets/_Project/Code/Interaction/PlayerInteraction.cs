using System;
using Tikal.Input;
using UnityEngine;

namespace Tikal.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("A component implementing IPlayerInputReader.")]
        [SerializeField] private MonoBehaviour _inputSource;
        [SerializeField] private Camera _viewCamera;
        [SerializeField, Min(0)] private float _interactionDistance = 3f;
        [Tooltip("Include solid occluders as well as interactables to prevent interaction through walls.")]
        [SerializeField] private LayerMask _interactionLayers = ~0;

        private IPlayerInputReader _input;
        private bool _subscribed;
        private int _pendingInteractions;
        private Collider _lastCollider;
        private IInteractable _resolved;
        private UnityEngine.Object _resolvedObject;

        public IInteractable CurrentInteractable { get; private set; }
        public event Action<IInteractable> FocusChanged;

        private void Awake()
        {
            _input = _inputSource as IPlayerInputReader;
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
            _pendingInteractions = 0;
            _lastCollider = null;
            _resolved = null;
            _resolvedObject = null;
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
            if (_input == null || !_input.GameplayEnabled || _viewCamera == null)
            {
                SetFocus(null);
                return;
            }
            var ray = new Ray(_viewCamera.transform.position, _viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _interactionDistance, _interactionLayers, QueryTriggerInteraction.Ignore))
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
