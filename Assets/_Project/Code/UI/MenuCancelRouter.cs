using Tikal.Input;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuCancelRouter : MonoBehaviour
    {
        [SerializeField] private MainMenuController _menu;
        private TikalInputActions _actions;
        private System.Action<InputAction.CallbackContext> _cancel;

        private void OnEnable()
        {
            _actions = new TikalInputActions();
            _cancel = _ => _menu.Cancel();
            _actions.UI.Cancel.performed += _cancel;
            _actions.UI.Enable();
        }

        private void OnDisable()
        {
            if (_actions == null) return;
            _actions.UI.Cancel.performed -= _cancel;
            _actions.Dispose();
            _actions = null;
            _cancel = null;
        }
    }
}
