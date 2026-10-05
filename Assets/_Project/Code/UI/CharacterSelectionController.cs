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
            var saved = CharacterCatalog.GetOrDefault(_progress.SelectedCharacterId);
            if (_currentSelectionLabel != null)
                _currentSelectionLabel.text = "ELECCIÓN: " + selected.DisplayName.ToUpperInvariant() + "   ·   GUARDADO: " + saved.DisplayName.ToUpperInvariant();
            if (_characterButtons == null) return;
            for (var index = 0; index < _characterButtons.Length && index < CharacterCatalog.All.Count; index++)
            {
                if (_characterButtons[index] == null) continue;
                var visual = _characterButtons[index].GetComponent<MenuSelectableVisual>();
                if (visual != null) visual.SetPersistent(CharacterCatalog.All[index].Id == saved.Id);
            }
        }
    }
}
