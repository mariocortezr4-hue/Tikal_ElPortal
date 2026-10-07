using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tikal.UI
{
    public static class MenuVisualPalette
    {
        public static readonly Color Ink = new Color(.025f, .035f, .03f, .96f);
        public static readonly Color Stone = new Color(.075f, .105f, .085f, .96f);
        public static readonly Color Hover = new Color(.13f, .17f, .13f, .98f);
        public static readonly Color Gold = new Color(.86f, .65f, .32f);
        public static readonly Color Jade = new Color(.12f, .28f, .21f);
        public static readonly Color JadeEdge = new Color(.40f, .68f, .52f);
        public static readonly Color Ivory = new Color(.96f, .93f, .84f);
        public static readonly Color Muted = new Color(.74f, .73f, .65f);
    }

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
        [SerializeField] private GameObject _focusOutline = null;
        [SerializeField] private GameObject _savedOutline = null;

        private static readonly Color Stone = MenuVisualPalette.Stone;
        private static readonly Color Hover = MenuVisualPalette.Hover;
        private static readonly Color Gold = MenuVisualPalette.Gold;
        private static readonly Color Ivory = MenuVisualPalette.Ivory;
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
            RefreshVisual();
        }

        public void ConfigureBorders(GameObject focusOutline, GameObject savedOutline)
        { _focusOutline = focusOutline; _savedOutline = savedOutline; RefreshVisual(); }

        public void RefreshVisual()
        {
            var interactable = _selectable == null || _selectable.IsInteractable();
            var hasFocus = _selected && interactable;
            var saved = _persistentMarker != null && _persistentMarker.activeSelf;
            if (_focusIndicator != null) _focusIndicator.SetActive(hasFocus);
            if (_focusOutline != null) _focusOutline.SetActive(hasFocus);
            if (_savedOutline != null) _savedOutline.SetActive(saved);
            if (_background != null)
            {
                if (!interactable) _background.color = Disabled;
                else if (saved) _background.color = MenuVisualPalette.Jade;
                else if (_pressed) _background.color = Hover;
                else if (hasFocus) _background.color = Hover;
                else if (_hovered) _background.color = Hover;
                else _background.color = _primary ? new Color(.21f, .17f, .095f, .97f) : Stone;
            }
            if (_label == null) return;
            _label.color = !interactable ? MenuVisualPalette.Muted : hasFocus ? Color.white : Ivory;
            _label.fontStyle = hasFocus ? FontStyles.Bold : FontStyles.Normal;
            _label.characterSpacing = 2f;
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
