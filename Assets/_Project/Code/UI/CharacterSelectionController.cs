using Tikal.Core;
using Tikal.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private Button[] _characterButtons;
        [SerializeField] private TMP_Text[] _characterLabels;
        [SerializeField] private Image[] _selectionFrames;
        [SerializeField] private TMP_Text _currentSelectionLabel;
        [SerializeField] private Button _confirmButton;

        private readonly Color _selectedColor = new Color(.45f, .58f, .38f, 1f);
        private readonly Color _idleColor = new Color(.12f, .14f, .12f, .92f);
        private GamePersistenceService _persistence;
        private GameProgressData _progress;
        private string _pendingCharacterId;

        public GameObject FirstSelection => _characterButtons != null && _characterButtons.Length > 0 && _characterButtons[0] != null ? _characterButtons[0].gameObject : gameObject;

        private void Awake()
        {
            _persistence = new GamePersistenceService();
            _progress = _persistence.LoadProgress();
            _pendingCharacterId = _progress.SelectedCharacterId;
            RefreshLabels();
            RefreshSelection();
        }

        private void OnEnable()
        {
            if (_characterButtons != null)
                for (var index = 0; index < _characterButtons.Length; index++)
                {
                    var buttonIndex = index;
                    if (_characterButtons[index] != null) _characterButtons[index].onClick.AddListener(() => Select(buttonIndex));
                }
            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            RefreshSelection();
        }

        private void OnDisable()
        {
            if (_characterButtons != null)
                foreach (var button in _characterButtons)
                    if (button != null) button.onClick.RemoveAllListeners();
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(Confirm);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= CharacterCatalog.All.Count) return;
            _pendingCharacterId = CharacterCatalog.All[index].Id;
            RefreshSelection();
        }

        public void Confirm()
        {
            _progress.SelectedCharacterId = _pendingCharacterId;
            _persistence.SaveProgress(_progress);
            RefreshSelection();
        }

        private void RefreshLabels()
        {
            if (_characterLabels == null) return;
            for (var index = 0; index < _characterLabels.Length && index < CharacterCatalog.All.Count; index++)
                if (_characterLabels[index] != null)
                    _characterLabels[index].text = CharacterCatalog.All[index].DisplayName;
        }

        private void RefreshSelection()
        {
            var selected = CharacterCatalog.GetOrDefault(_pendingCharacterId);
            _pendingCharacterId = selected.Id;
            if (_currentSelectionLabel != null)
                _currentSelectionLabel.text = "Seleccionado: " + selected.DisplayName;
            if (_selectionFrames == null) return;
            for (var index = 0; index < _selectionFrames.Length && index < CharacterCatalog.All.Count; index++)
                if (_selectionFrames[index] != null)
                    _selectionFrames[index].color = CharacterCatalog.All[index].Id == selected.Id ? _selectedColor : _idleColor;
        }
    }
}
