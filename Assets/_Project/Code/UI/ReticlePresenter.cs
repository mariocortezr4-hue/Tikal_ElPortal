using Tikal.Input;
using Tikal.Interaction;
using UnityEngine;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Consume the final camera pose and interaction result.
    public sealed class ReticlePresenter : MonoBehaviour
    {
        [SerializeField] private ReticleSettings _settings;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private GameObject _dot;
        [SerializeField] private GameObject _cross;
        [SerializeField] private PlayerInteraction _interaction = null;
        [SerializeField] private Camera _viewCamera = null;
        private bool _subscribed;
        private bool _modalVisible;
        public PlayerInteraction AimSource => _interaction != null ? _interaction :
            (_inputReader != null ? _inputReader.GetComponent<PlayerInteraction>() : null);
        public Camera ViewCamera => _viewCamera != null ? _viewCamera : (AimSource != null ? AimSource.ViewCamera : null);
        public Vector2 ScreenOffset => ((RectTransform)transform).anchoredPosition;

        public bool IsVisible => isActiveAndEnabled &&
            ((_dot != null && _dot.activeSelf) || (_cross != null && _cross.activeSelf));

        private void OnEnable()
        {
            if (_settings == null || _inputReader == null || _dot == null || _cross == null)
            {
                Debug.LogError("Reticle requires settings, input availability and both visuals.", this);
                return;
            }
            // Keep center anchors; TPS applies only a transient screen-space offset.
            Center((RectTransform)transform);
            Center((RectTransform)_dot.transform);
            Center((RectTransform)_cross.transform);
            _interaction = AimSource;
            _viewCamera = ViewCamera;
            _settings.ModeChanged += OnModeChanged;
            _inputReader.GameplayEnabledChanged += OnGameplayChanged;
            _subscribed = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _settings.ModeChanged -= OnModeChanged;
                _inputReader.GameplayEnabledChanged -= OnGameplayChanged;
                _subscribed = false;
            }
            SetVisuals(false, false);
            ((RectTransform)transform).anchoredPosition = Vector2.zero;
        }

        /// <summary>A future pause/menu flow may hide the reticle without changing its selected mode.</summary>
        public void SetModalVisible(bool visible)
        {
            _modalVisible = visible;
            Refresh();
        }

        private void OnModeChanged(ReticleMode mode) => Refresh();
        private void OnGameplayChanged(bool enabled) => Refresh();
        private void LateUpdate() => UpdatePosition();

        private void UpdatePosition()
        {
            var rect = (RectTransform)transform;
            var offset = Vector2.zero;
            if (_subscribed && _inputReader.GameplayEnabled && !_modalVisible && _interaction != null &&
                _interaction.isActiveAndEnabled && _interaction.UsesThirdPersonAim && _interaction.HasResolvedAimPoint)
                TryGetScreenOffset(_viewCamera, rect, _interaction.ResolvedAimPoint, out offset);
            rect.anchoredPosition = offset;
        }

        /// <summary>Project world aim into centered parent coordinates, including CanvasScaler.</summary>
        public static bool TryGetScreenOffset(Camera viewCamera, RectTransform reticle, Vector3 point, out Vector2 offset)
        {
            offset = Vector2.zero;
            if (viewCamera == null || reticle == null || !Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
            var canvas = reticle.GetComponentInParent<Canvas>();
            var parent = reticle.parent as RectTransform;
            if (canvas == null || parent == null) return false;
            var screen = viewCamera.WorldToScreenPoint(point);
            if (screen.z <= 0 || !Finite(screen.x) || !Finite(screen.y) || !Finite(screen.z)) return false;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var local)) return false;
            var result = local - parent.rect.center;
            if (!Finite(result.x) || !Finite(result.y)) return false;
            offset = result;
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition3D = Vector3.zero;
        }

        private void Refresh()
        {
            bool visible = isActiveAndEnabled && _subscribed && _inputReader.GameplayEnabled && !_modalVisible;
            SetVisuals(visible && _settings.Mode == ReticleMode.Dot, visible && _settings.Mode == ReticleMode.Cross);
            UpdatePosition();
        }

        private void SetVisuals(bool dot, bool cross)
        {
            if (_dot != null) _dot.SetActive(dot);
            if (_cross != null) _cross.SetActive(cross);
        }
    }
}
