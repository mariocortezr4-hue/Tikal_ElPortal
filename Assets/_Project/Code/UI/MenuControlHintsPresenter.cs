using Tikal.Input;
using TMPro;
using UnityEngine;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuControlHintsPresenter : MonoBehaviour
    {
        [SerializeField] private InputDeviceManager _devices;
        [SerializeField] private MainMenuController _menu;
        [SerializeField] private TMP_Text _navigateHint;
        [SerializeField] private TMP_Text _submitHint;
        [SerializeField] private TMP_Text _cancelHint;

        private TikalInputActions _actions;

        private void OnEnable()
        {
            _actions = new TikalInputActions();
            if (_devices != null) _devices.ActiveDeviceChanged += OnDeviceChanged;
            if (_menu != null) _menu.PanelChanged += OnPanelChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_devices != null) _devices.ActiveDeviceChanged -= OnDeviceChanged;
            if (_menu != null) _menu.PanelChanged -= OnPanelChanged;
            _actions?.Dispose();
            _actions = null;
        }

        private void OnDeviceChanged(InputDeviceFamily _) => Refresh();
        private void OnPanelChanged(MenuPanelContext _) => Refresh();

        private void Refresh()
        {
            if (_actions == null) return;
            var family = _devices != null ? _devices.ActiveDevice : InputDeviceFamily.KeyboardMouse;
            var navigate = InputPromptResolver.Resolve(_actions.UI.Navigate, family).DisplayText;
            var submit = InputPromptResolver.Resolve(_actions.UI.Submit, family).DisplayText;
            var cancel = InputPromptResolver.Resolve(_actions.UI.Cancel, family).DisplayText;
            if (_navigateHint != null) _navigateHint.text = navigate + "  NAVEGAR";
            if (_submitHint != null) _submitHint.text = submit + "  " + SubmitVerb();
            if (_cancelHint != null)
            {
                _cancelHint.text = cancel + "  VOLVER";
                _cancelHint.gameObject.SetActive(_menu != null && _menu.CurrentPanel != MenuPanelContext.Main);
            }
        }

        private string SubmitVerb()
        {
            if (_menu == null) return "SELECCIONAR";
            switch (_menu.CurrentPanel)
            {
                case MenuPanelContext.Settings: return "AJUSTAR";
                case MenuPanelContext.Character: return "ELEGIR / CONFIRMAR";
                default: return "SELECCIONAR";
            }
        }
    }
}
