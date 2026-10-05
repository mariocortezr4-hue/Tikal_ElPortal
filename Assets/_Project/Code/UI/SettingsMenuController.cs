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
        [SerializeField] private TMP_Dropdown _reticleMode;
        [SerializeField] private ReticleSettings _previewReticle;

        private GamePersistenceService _persistence;
        private GameSettingsData _settings;
        private bool _loading;

        public GameObject FirstSelection => _masterVolume != null ? _masterVolume.gameObject : gameObject;

        private void Awake()
        {
            _persistence = new GamePersistenceService();
            _settings = _persistence.LoadSettings();
            LoadControls();
        }

        private void OnEnable()
        {
            _masterVolume.onValueChanged.AddListener(OnMaster);
            _musicVolume.onValueChanged.AddListener(OnMusic);
            _sfxVolume.onValueChanged.AddListener(OnSfx);
            _mouseSensitivity.onValueChanged.AddListener(OnMouse);
            _gamepadSensitivity.onValueChanged.AddListener(OnGamepad);
            _invertY.onValueChanged.AddListener(OnInvert);
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
            _reticleMode.value = _settings.ReticleMode == ReticleMode.None ? 0 : _settings.ReticleMode == ReticleMode.Dot ? 1 : 2;
            ApplyRuntime();
            _loading = false;
        }

        private void OnMaster(float value) { _settings.MasterVolume = value; Save(); }
        private void OnMusic(float value) { _settings.MusicVolume = value; Save(); }
        private void OnSfx(float value) { _settings.SfxVolume = value; Save(); }
        private void OnMouse(float value) { _settings.MouseSensitivity = value; Save(); }
        private void OnGamepad(float value) { _settings.GamepadSensitivity = value; Save(); }
        private void OnInvert(bool value) { _settings.InvertY = value; Save(); }
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
            if (_previewReticle != null) _previewReticle.SetMode(_settings.ReticleMode);
        }
    }
}
