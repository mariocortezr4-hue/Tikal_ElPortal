using Tikal.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class CinematicSkipPromptPresenter : MonoBehaviour
    {
        [SerializeField] private IntroSequenceController _sequence;
        [SerializeField] private InputDeviceManager _devices;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _progress;

        public bool HasRequiredReferences => _sequence != null && _devices != null && _label != null && _progress != null;

        private void OnEnable()
        {
            if (!HasRequiredReferences) return;
            _sequence.SkipProgressChanged += OnProgressChanged;
            _devices.ActiveDeviceChanged += OnDeviceChanged;
            OnProgressChanged(0);
            Refresh();
        }

        private void OnDisable()
        {
            if (_sequence == null || _devices == null) return;
            _sequence.SkipProgressChanged -= OnProgressChanged;
            _devices.ActiveDeviceChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(InputDeviceFamily _) => Refresh();

        private void Refresh()
        {
            if (!HasRequiredReferences) return;
            var prompt = InputPromptResolver.Resolve(_sequence.SkipAction, _devices.ActiveDevice).DisplayText;
            _label.text = "MANTENER [ " + (prompt ?? string.Empty).ToUpperInvariant() + " ] PARA OMITIR";
        }

        private void OnProgressChanged(float progress) => _progress.fillAmount = Mathf.Clamp01(progress);
    }
}
