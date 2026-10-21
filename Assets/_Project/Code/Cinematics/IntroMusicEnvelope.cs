using Tikal.Persistence;
using UnityEngine;
using UnityEngine.Playables;

namespace Tikal.Cinematics
{
    // Timeline owns playback; this component multiplies the cinematic mix by existing settings.
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class IntroMusicEnvelope : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private AnimationCurve _envelope = new AnimationCurve();
        private AudioSource _source;
        private GameSettingsData _settings;
        private PersistentSettingsApplier _applier;
        public AnimationCurve Envelope => _envelope;
        public float MixAt(double time) => Mathf.Clamp01(_envelope.Evaluate((float)time));

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _applier = FindAnyObjectByType<PersistentSettingsApplier>();
            ApplySettings(_applier != null && _applier.CurrentSettings != null
                ? _applier.CurrentSettings : new GamePersistenceService().LoadSettings());
        }
        private void OnEnable()
        {
            if (_applier != null) _applier.SettingsApplied += ApplySettings;
        }
        private void OnDisable()
        {
            if (_applier != null) _applier.SettingsApplied -= ApplySettings;
            if (_source != null) { _source.volume = 0; _source.Stop(); }
        }
        public void ApplySettings(GameSettingsData settings)
        {
            _settings = settings ?? GameSettingsData.CreateDefault();
            // Master follows the project's existing listener convention, without double multiplication.
            AudioListener.volume = _settings.MasterVolume;
            UpdateVolume();
        }
        private void LateUpdate() => UpdateVolume();
        private void UpdateVolume()
        {
            if (_source == null || _director == null || _settings == null) return;
            _source.volume = _settings.MusicVolume * MixAt(_director.time);
        }
    }
}
