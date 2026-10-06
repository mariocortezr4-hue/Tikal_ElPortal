using TMPro;
using UnityEngine;

namespace Tikal.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class CinematicSubtitlePresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _dateCard;
        [SerializeField] private TMP_Text _dateText;
        [SerializeField] private GameObject _narrativePanel;
        [SerializeField] private TMP_Text _narrativeText;
        [SerializeField] private GameObject _subtitlePanel;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private GameObject _titleCard;
        [SerializeField] private TMP_Text _titleText;
        private string _activeCueId;

        public bool HasRequiredReferences => _dateCard != null && _dateText != null &&
            _narrativePanel != null && _narrativeText != null && _subtitlePanel != null &&
            _speakerText != null && _dialogueText != null && _titleCard != null && _titleText != null;

        private void Awake() => Clear();

        public void Show(string cueId, CinematicCueType type, string speaker, string text)
        {
            if (_activeCueId == cueId) return;
            Clear();
            _activeCueId = cueId;
            switch (type)
            {
                case CinematicCueType.Date:
                    _dateText.text = text; _dateCard.SetActive(true); break;
                case CinematicCueType.Narrative:
                    _narrativeText.text = text; _narrativePanel.SetActive(true); break;
                case CinematicCueType.Subtitle:
                    _speakerText.text = speaker; _dialogueText.text = text; _subtitlePanel.SetActive(true); break;
                case CinematicCueType.Title:
                    _titleText.text = text; _titleCard.SetActive(true); break;
            }
        }

        public void Release(string cueId)
        {
            if (_activeCueId == cueId) Clear();
        }

        private void Clear()
        {
            _activeCueId = string.Empty;
            if (_dateCard != null) _dateCard.SetActive(false);
            if (_narrativePanel != null) _narrativePanel.SetActive(false);
            if (_subtitlePanel != null) _subtitlePanel.SetActive(false);
            if (_titleCard != null) _titleCard.SetActive(false);
        }
    }
}
