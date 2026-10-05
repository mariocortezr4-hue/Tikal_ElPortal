using System.Collections.Generic;
using Tikal.Core;
using Tikal.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _levelsPanel;
        [SerializeField] private GameObject _characterPanel;
        [SerializeField] private GameObject _cinematicsPanel;
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private TMP_Text _playLabel;
        [SerializeField] private TMP_Text _continueSubtitle;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelsButton;
        [SerializeField] private Button _characterButton;
        [SerializeField] private Button _cinematicsButton;
        [SerializeField] private Button _tutorialButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _level01Button;
        [SerializeField] private GameObject _level02Row;
        [SerializeField] private Button _introReplayButton;
        [SerializeField] private Button _tutorialReplayButton;
        [SerializeField] private Button[] _backButtons;
        [SerializeField] private SettingsMenuController _settings;
        [SerializeField] private CharacterSelectionController _characterSelection;

        private readonly List<GameObject> _panels = new List<GameObject>();

        private void Awake()
        {
            _panels.AddRange(new[] { _mainPanel, _levelsPanel, _characterPanel, _cinematicsPanel, _tutorialPanel, _settingsPanel, _creditsPanel });
        }

        private void OnEnable()
        {
            _playButton.onClick.AddListener(_flow.PlayOrContinue);
            _levelsButton.onClick.AddListener(() => Show(_levelsPanel, _level01Button.gameObject));
            _characterButton.onClick.AddListener(() => Show(_characterPanel, _characterSelection != null ? _characterSelection.FirstSelection : null));
            _cinematicsButton.onClick.AddListener(() => Show(_cinematicsPanel, _introReplayButton != null ? _introReplayButton.gameObject : null));
            _tutorialButton.onClick.AddListener(() => Show(_tutorialPanel, _tutorialReplayButton != null ? _tutorialReplayButton.gameObject : null));
            _settingsButton.onClick.AddListener(() => Show(_settingsPanel, _settings != null ? _settings.FirstSelection : null));
            _creditsButton.onClick.AddListener(() => Show(_creditsPanel, _creditsPanel));
            _quitButton.onClick.AddListener(_flow.Quit);
            _level01Button.onClick.AddListener(() => _flow.LaunchLevel(GameSceneIds.Level01));
            if (_introReplayButton != null) _introReplayButton.onClick.AddListener(_flow.ReplayIntro);
            if (_tutorialReplayButton != null) _tutorialReplayButton.onClick.AddListener(_flow.LaunchTutorial);
            if (_backButtons != null)
                foreach (var button in _backButtons)
                    if (button != null) button.onClick.AddListener(Cancel);
            Refresh();
            Show(_mainPanel, _playButton.gameObject);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveAllListeners();
            _levelsButton.onClick.RemoveAllListeners();
            _characterButton.onClick.RemoveAllListeners();
            _cinematicsButton.onClick.RemoveAllListeners();
            _tutorialButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveAllListeners();
            _creditsButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
            _level01Button.onClick.RemoveAllListeners();
            if (_introReplayButton != null) _introReplayButton.onClick.RemoveAllListeners();
            if (_tutorialReplayButton != null) _tutorialReplayButton.onClick.RemoveAllListeners();
            if (_backButtons != null)
                foreach (var button in _backButtons)
                    if (button != null) button.onClick.RemoveListener(Cancel);
        }

        public void Cancel()
        {
            if (_mainPanel.activeSelf) return;
            Show(_mainPanel, _playButton.gameObject);
        }

        private void Refresh()
        {
            var progress = _flow.Progress;
            _playLabel.text = _flow.PlayButtonLabel;
            _continueSubtitle.text = Subtitle(progress);
            _level01Button.gameObject.SetActive(progress.IsLevelUnlocked(GameSceneIds.Level01));
            _level01Button.interactable = _flow.CanLaunchLevel(GameSceneIds.Level01);
            if (_level02Row != null) _level02Row.SetActive(progress.IsLevelUnlocked(GameSceneIds.Level02));
            if (_introReplayButton != null) _introReplayButton.gameObject.SetActive(progress.IntroSeen);
            if (_tutorialReplayButton != null) _tutorialReplayButton.gameObject.SetActive(progress.TutorialCompleted);
        }

        private string Subtitle(GameProgressData progress)
        {
            if (!progress.HasStartedGame) return string.Empty;
            if (!progress.IntroSeen) return "Introduccion pendiente";
            if (!progress.TutorialCompleted) return "Tutorial pendiente";
            return _flow.CurrentLevelTitle;
        }

        private void Show(GameObject panel, GameObject selection)
        {
            Refresh();
            foreach (var item in _panels)
                if (item != null) item.SetActive(item == panel);
            if (_eventSystem != null && selection != null && selection.activeInHierarchy)
                _eventSystem.SetSelectedGameObject(selection);
        }
    }
}
