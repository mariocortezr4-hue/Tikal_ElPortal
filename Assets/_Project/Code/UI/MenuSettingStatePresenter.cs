using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    // Read-only presentation of existing controls; owns no settings or input actions.
    [DisallowMultipleComponent]
    public sealed class MenuSettingStatePresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _choice = null;
        [SerializeField] private Toggle _toggle = null;
        [SerializeField] private Slider _slider = null;
        [SerializeField] private Image[] _optionFills = Array.Empty<Image>();
        [SerializeField] private GameObject[] _activeMarkers = Array.Empty<GameObject>();
        [SerializeField] private TMP_Text _valueText = null;
        [SerializeField] private bool _percentage = false;
        private int _lastChoice = int.MinValue;
        private float _lastValue = float.NaN;
        private bool _lastToggle;
        public bool IsConfigured => (_choice != null && _optionFills.Length == _choice.options.Count &&
            _activeMarkers.Length == _optionFills.Length && Array.TrueForAll(_optionFills, f => f != null) &&
            Array.TrueForAll(_activeMarkers, m => m != null)) || ((_toggle != null || _slider != null) && _valueText != null);

        private void OnEnable() => RefreshVisual();
        private void LateUpdate()
        {
            // Also observes SetValueWithoutNotify when Settings loads its saved values.
            if ((_choice != null && _lastChoice != _choice.value) || (_toggle != null && _lastToggle != _toggle.isOn) ||
                (_slider != null && _lastValue != _slider.value)) RefreshVisual();
        }
        public void RefreshVisual()
        {
            if (_choice != null)
            {
                _lastChoice = _choice.value;
                for (int i = 0; i < _optionFills.Length; i++)
                {
                    bool active = i == _choice.value;
                    if (_optionFills[i] != null) _optionFills[i].color = active ? MenuVisualPalette.Jade : MenuVisualPalette.Stone;
                    if (i < _activeMarkers.Length && _activeMarkers[i] != null) _activeMarkers[i].SetActive(active);
                }
            }
            if (_toggle != null)
            {
                _lastToggle = _toggle.isOn;
                if (_valueText != null) _valueText.text = _lastToggle ? "ACTIVADO" : "DESACTIVADO";
            }
            if (_slider != null)
            {
                _lastValue = _slider.value;
                if (_valueText != null) _valueText.text = _percentage ? Mathf.RoundToInt(_lastValue * 100) + "%" :
                    _lastValue.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
    }
}
