using Tikal.Persistence;
using UnityEngine;
using UnityEngine.Playables;

namespace Tikal.Cinematics
{
    // Timeline owns playback and per-clip gain. VO follows existing SFX/master settings.
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class IntroVoiceSettings : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        private AudioSource _source;
        private PersistentSettingsApplier _applier;
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
            if (_director != null) _director.stopped += StopVoice;
        }
        private void OnDisable()
        {
            if (_applier != null) _applier.SettingsApplied -= ApplySettings;
            if (_director != null) _director.stopped -= StopVoice;
            StopVoice(_director);
        }
        public void ApplySettings(GameSettingsData settings)
        {
            if (_source != null) _source.volume = (settings ?? GameSettingsData.CreateDefault()).SfxVolume;
        }
        private void StopVoice(PlayableDirector _) { if (_source != null) _source.Stop(); }
    }
}
