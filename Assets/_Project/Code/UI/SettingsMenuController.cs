using Tikal.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private Slider _masterVolume;
        [SerializeField] private Slider _musicVolume;
        [SerializeField] private Slider _sfxVolume;
        [SerializeField] private Slider _mouseSensitivity;
        [SerializeField] private Slider _gamepadSensitivity;
        [SerializeField] private Toggle _invertY;
        [SerializeField] private TMP_Dropdown _cameraMode;
        [SerializeField] private TMP_Dropdown _reticleMode;
        [SerializeField] private ReticleSettings _previewReticle;
        [SerializeField] private PersistentSettingsApplier _runtimeApplier = null;

        private GamePersistenceService _persistence;
        private GameSettingsData _settings;
        private bool _loading;

        public GameObject FirstSelection => _masterVolume != null ? _masterVolume.gameObject : gameObject;
        public bool IsConfigured => _masterVolume != null && _musicVolume != null && _sfxVolume != null &&
            _mouseSensitivity != null && _gamepadSensitivity != null && _invertY != null &&
            _cameraMode != null && _reticleMode != null;
        public bool HasOpenDropdown => (_cameraMode != null && _cameraMode.IsExpanded) || (_reticleMode != null && _reticleMode.IsExpanded);
        public void CloseDropdowns() { if (_cameraMode != null && _cameraMode.IsExpanded) _cameraMode.Hide(); if (_reticleMode != null && _reticleMode.IsExpanded) _reticleMode.Hide(); }

        private void Awake()
        {
            _persistence = new GamePersistenceService();
        }

        private void OnEnable()
        {
            _settings = _runtimeApplier != null && _runtimeApplier.CurrentSettings != null
                ? _runtimeApplier.CurrentSettings : _persistence.LoadSettings();
            LoadControls();
            _masterVolume.onValueChanged.AddListener(OnMaster);
            _musicVolume.onValueChanged.AddListener(OnMusic);
            _sfxVolume.onValueChanged.AddListener(OnSfx);
            _mouseSensitivity.onValueChanged.AddListener(OnMouse);
            _gamepadSensitivity.onValueChanged.AddListener(OnGamepad);
            _invertY.onValueChanged.AddListener(OnInvert);
            if (_cameraMode != null) _cameraMode.onValueChanged.AddListener(OnCameraMode);
            _reticleMode.onValueChanged.AddListener(OnReticle);
        }

        private void OnDisable()
        {
            _masterVolume.onValueChanged.RemoveListener(OnMaster);
            _musicVolume.onValueChanged.RemoveListener(OnMusic);
            _sfxVolume.onValueChanged.RemoveListener(OnSfx);
            _mouseSensitivity.onValueChanged.RemoveListener(OnMouse);
            _gamepadSensitivity.onValueChanged.RemoveListener(OnGamepad);
            _invertY.onValueChanged.RemoveListener(OnInvert);
            if (_cameraMode != null) _cameraMode.onValueChanged.RemoveListener(OnCameraMode);
            _reticleMode.onValueChanged.RemoveListener(OnReticle);
        }

        private void LoadControls()
        {
            _loading = true;
            _masterVolume.value = _settings.MasterVolume;
            _musicVolume.value = _settings.MusicVolume;
            _sfxVolume.value = _settings.SfxVolume;
            _mouseSensitivity.value = _settings.MouseSensitivity;
            _gamepadSensitivity.value = _settings.GamepadSensitivity;
            _invertY.isOn = _settings.InvertY;
            if (_cameraMode != null)
            {
                _cameraMode.SetValueWithoutNotify(_settings.CameraMode == CameraMode.ThirdPerson ? 1 : 0);
                _cameraMode.RefreshShownValue();
            }
            _reticleMode.SetValueWithoutNotify(_settings.ReticleMode == ReticleMode.None ? 0 : _settings.ReticleMode == ReticleMode.Dot ? 1 : 2);
            _reticleMode.RefreshShownValue();
            ApplyRuntime();
            _loading = false;
        }

        private void OnMaster(float value) { _settings.MasterVolume = value; Save(); }
        private void OnMusic(float value) { _settings.MusicVolume = value; Save(); }
        private void OnSfx(float value) { _settings.SfxVolume = value; Save(); }
        private void OnMouse(float value) { _settings.MouseSensitivity = value; Save(); }
        private void OnGamepad(float value) { _settings.GamepadSensitivity = value; Save(); }
        private void OnInvert(bool value) { _settings.InvertY = value; Save(); }
        private void OnCameraMode(int value)
        {
            _settings.CameraMode = value == 1 ? CameraMode.ThirdPerson : CameraMode.FirstPerson;
            Save();
        }
        private void OnReticle(int value)
        {
            _settings.ReticleMode = value == 0 ? ReticleMode.None : value == 1 ? ReticleMode.Dot : ReticleMode.Cross;
            Save();
        }

        private void Save()
        {
            if (_loading) return;
            ApplyRuntime();
            _persistence.SaveSettings(_settings);
        }

        private void ApplyRuntime()
        {
            AudioListener.volume = _settings.MasterVolume;
            if (_runtimeApplier != null) _runtimeApplier.Apply(_settings);
            if (_previewReticle != null) _previewReticle.SetMode(_settings.ReticleMode);
        }
    }
}
