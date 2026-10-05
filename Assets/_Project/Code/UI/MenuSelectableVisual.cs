using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuSelectableVisual : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Selectable _selectable;
        [SerializeField] private Graphic _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _focusIndicator;
        [SerializeField] private GameObject _persistentMarker;
        [SerializeField] private bool _primary;

        private static readonly Color Ink = new Color(.018f, .035f, .055f, .96f);
        private static readonly Color Stone = new Color(.055f, .105f, .135f, .94f);
        private static readonly Color Hover = new Color(.105f, .18f, .205f, .97f);
        private static readonly Color Gold = new Color(.86f, .62f, .27f, 1f);
        private static readonly Color Ivory = new Color(.96f, .93f, .84f, 1f);
        private static readonly Color Disabled = new Color(.055f, .07f, .075f, .72f);

        private bool _selected;
        private bool _hovered;
        private bool _pressed;

        public void Configure(Selectable selectable, Graphic background, TMP_Text label,
            GameObject focusIndicator, GameObject persistentMarker, bool primary)
        {
            _selectable = selectable;
            _background = background;
            _label = label;
            _focusIndicator = focusIndicator;
            _persistentMarker = persistentMarker;
            _primary = primary;
            if (_selectable != null) _selectable.transition = Selectable.Transition.None;
            RefreshVisual();
        }

        public void SetPersistent(bool value)
        {
            if (_persistentMarker != null) _persistentMarker.SetActive(value);
        }

        public void RefreshVisual()
        {
            var interactable = _selectable == null || _selectable.IsInteractable();
            var hasFocus = _selected && interactable;
            if (_focusIndicator != null) _focusIndicator.SetActive(hasFocus);
            if (_background != null)
            {
                if (!interactable) _background.color = Disabled;
                else if (_pressed) _background.color = new Color(.68f, .48f, .2f, 1f);
                else if (hasFocus) _background.color = Gold;
                else if (_hovered) _background.color = Hover;
                else _background.color = _primary ? new Color(.73f, .49f, .18f, .96f) : Stone;
            }
            if (_label == null) return;
            _label.color = !interactable ? new Color(.48f, .51f, .52f, 1f) : hasFocus ? (_background != null ? Ink : Gold) : Ivory;
            _label.fontStyle = hasFocus ? FontStyles.Bold : FontStyles.Normal;
            _label.characterSpacing = hasFocus ? 7f : 2f;
        }

        private void OnEnable()
        {
            _selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            RefreshVisual();
        }

        private void OnDisable()
        {
            _selected = _hovered = _pressed = false;
        }

        public void OnSelect(BaseEventData eventData) { _selected = true; RefreshVisual(); }
        public void OnDeselect(BaseEventData eventData) { _selected = false; _pressed = false; RefreshVisual(); }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (_selectable != null && _selectable.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; RefreshVisual(); }
        public void OnPointerDown(PointerEventData eventData) { _pressed = true; RefreshVisual(); }
        public void OnPointerUp(PointerEventData eventData) { _pressed = false; RefreshVisual(); }
    }
}
