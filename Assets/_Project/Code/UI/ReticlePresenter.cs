using Tikal.Input;
using UnityEngine;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class ReticlePresenter : MonoBehaviour
    {
        [SerializeField] private ReticleSettings _settings;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private GameObject _dot;
        [SerializeField] private GameObject _cross;
        private bool _subscribed;
        private bool _modalVisible;

        public bool IsVisible => isActiveAndEnabled &&
            ((_dot != null && _dot.activeSelf) || (_cross != null && _cross.activeSelf));

        private void OnEnable()
        {
            if (_settings == null || _inputReader == null || _dot == null || _cross == null)
            {
                Debug.LogError("Reticle requires settings, input availability and both visuals.", this);
                return;
            }
            // The reticle represents camera-center aim, never the shoulder-shifted player.
            Center((RectTransform)transform);
            Center((RectTransform)_dot.transform);
            Center((RectTransform)_cross.transform);
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
        }

        /// <summary>A future pause/menu flow may hide the reticle without changing its selected mode.</summary>
        public void SetModalVisible(bool visible)
        {
            _modalVisible = visible;
            Refresh();
        }

        private void OnModeChanged(ReticleMode mode) => Refresh();
        private void OnGameplayChanged(bool enabled) => Refresh();

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition3D = Vector3.zero;
        }

        private void Refresh()
        {
            bool visible = isActiveAndEnabled && _subscribed && _inputReader.GameplayEnabled && !_modalVisible;
            SetVisuals(visible && _settings.Mode == ReticleMode.Dot, visible && _settings.Mode == ReticleMode.Cross);
        }

        private void SetVisuals(bool dot, bool cross)
        {
            if (_dot != null) _dot.SetActive(dot);
            if (_cross != null) _cross.SetActive(cross);
        }
    }
}
