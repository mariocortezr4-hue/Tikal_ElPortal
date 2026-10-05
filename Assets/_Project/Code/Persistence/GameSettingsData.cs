using System;
using Tikal.UI;
using UnityEngine;

namespace Tikal.Persistence
{
    public enum GraphicsQuality { Low, Medium, High }
    public enum DisplayMode { Windowed, Fullscreen }

    [Serializable]
    public sealed class GameSettingsData
    {
        [SerializeField, Range(0, 1)] private float _masterVolume = 1f;
        [SerializeField, Range(0, 1)] private float _musicVolume = .8f;
        [SerializeField, Range(0, 1)] private float _sfxVolume = 1f;
        [SerializeField, Min(0)] private float _mouseSensitivity = .12f;
        [SerializeField, Min(0)] private float _gamepadSensitivity = 150f;
        [SerializeField] private bool _invertY;
        [SerializeField] private ReticleMode _reticleMode = ReticleMode.Dot;
        [SerializeField] private GraphicsQuality _graphicsQuality = GraphicsQuality.Medium;
        [SerializeField] private DisplayMode _displayMode = DisplayMode.Fullscreen;
        [SerializeField] private string _resolution = string.Empty;

        public float MasterVolume { get => _masterVolume; set => _masterVolume = Mathf.Clamp01(value); }
        public float MusicVolume { get => _musicVolume; set => _musicVolume = Mathf.Clamp01(value); }
        public float SfxVolume { get => _sfxVolume; set => _sfxVolume = Mathf.Clamp01(value); }
        public float MouseSensitivity { get => _mouseSensitivity; set => _mouseSensitivity = Mathf.Max(0, value); }
        public float GamepadSensitivity { get => _gamepadSensitivity; set => _gamepadSensitivity = Mathf.Max(0, value); }
        public bool InvertY { get => _invertY; set => _invertY = value; }
        public Tikal.UI.ReticleMode ReticleMode { get => _reticleMode; set => _reticleMode = value; }
        public GraphicsQuality GraphicsQuality { get => _graphicsQuality; set => _graphicsQuality = value; }
        public DisplayMode DisplayMode { get => _displayMode; set => _displayMode = value; }
        public string Resolution { get => _resolution; set => _resolution = value ?? string.Empty; }

        public static GameSettingsData CreateDefault() => new GameSettingsData();

        public void Normalize()
        {
            MasterVolume = _masterVolume;
            MusicVolume = _musicVolume;
            SfxVolume = _sfxVolume;
            MouseSensitivity = _mouseSensitivity <= 0 ? .12f : _mouseSensitivity;
            GamepadSensitivity = _gamepadSensitivity <= 0 ? 150f : _gamepadSensitivity;
            if (_reticleMode != Tikal.UI.ReticleMode.None && _reticleMode != Tikal.UI.ReticleMode.Dot && _reticleMode != Tikal.UI.ReticleMode.Cross)
                _reticleMode = Tikal.UI.ReticleMode.Dot;
            _resolution ??= string.Empty;
        }
    }
}
