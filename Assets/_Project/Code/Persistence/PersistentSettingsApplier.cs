using Tikal.Player;
using Tikal.UI;
using UnityEngine;

namespace Tikal.Persistence
{
    [DisallowMultipleComponent]
    public sealed class PersistentSettingsApplier : MonoBehaviour
    {
        [SerializeField] private ReticleSettings _reticleSettings;
        [SerializeField] private PlayerLook _playerLook;

        private GamePersistenceService _service;
        public CameraMode CameraMode { get; private set; } = Tikal.Persistence.CameraMode.FirstPerson;

        private void Awake()
        {
            _service = new GamePersistenceService();
            Apply(_service.LoadSettings());
        }

        public void Apply(GameSettingsData settings)
        {
            settings ??= GameSettingsData.CreateDefault();
            CameraMode = settings.CameraMode;
            AudioListener.volume = settings.MasterVolume;
            if (_reticleSettings != null) _reticleSettings.SetMode(settings.ReticleMode);
            if (_playerLook != null) _playerLook.ApplySettings(settings.MouseSensitivity, settings.GamepadSensitivity, settings.InvertY);
        }
    }
}
