using UnityEngine;

namespace Tikal.Interaction.Debugging
{
    public sealed class InteractionDebugPrompt : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private IInteractable _focused;
        private string _text;
        private string _label;

        private void OnEnable()
        {
            if (_interaction == null) return;
            _interaction.FocusChanged += OnFocusChanged;
            OnFocusChanged(_interaction.CurrentInteractable);
        }

        private void OnDisable()
        {
            if (_interaction != null) _interaction.FocusChanged -= OnFocusChanged;
            OnFocusChanged(null);
        }

        private void OnFocusChanged(IInteractable focused)
        {
            _focused = focused;
            _text = null;
            _label = null;
            RefreshText();
        }

        private void Update() => RefreshText();

        private void RefreshText()
        {
            var owner = _focused as Object;
            var text = owner != null && _focused.CanInteract ? _focused.InteractionText : null;
            if (text == _text) return;
            _text = text;
            _label = text == null ? null : "Interactuar: " + text;
        }

        private void OnGUI()
        {
            if (_label != null) GUI.Label(new Rect(Screen.width * 0.5f - 120, Screen.height * 0.5f + 35, 260, 30), _label);
        }
#endif
    }
}
