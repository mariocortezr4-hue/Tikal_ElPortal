using System;
using System.Text;
using Tikal.Core;
using Tikal.Input;
using Tikal.Player;
using Tikal.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // Process queued Pause before camera/interaction LateUpdate.
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private PlayerController _player = null;
        [SerializeField] private PlayerInputReader _input = null;
        [SerializeField] private InputDeviceManager _devices = null;
        [SerializeField] private GameFlowController _flow = null;
        [SerializeField] private ClueJournalPresenter _journal = null;
        [SerializeField] private LevelOutcomeController _outcome = null;
        [SerializeField] private TutorialSequenceController _tutorial = null;
        [SerializeField] private EventSystem _events = null;
        [SerializeField] private SettingsMenuController _settings = null;
        [SerializeField] private GameObject _overlay = null, _mainPanel = null, _settingsPanel = null, _controlsPanel = null;
        [SerializeField] private Button _continue = null, _configuration = null, _controls = null, _restart = null, _mainMenu = null, _quit = null;
        [SerializeField] private Button _settingsBack = null, _controlsBack = null;
        [SerializeField] private TMP_Text _controlsText = null, _hints = null, _restartHint = null;
        [SerializeField] private MonoBehaviour _checkpointRestart = null;
        [SerializeField] private bool _restartTutorialFromStart = false;
        private TikalInputActions _ui;
        private bool _subscribed, _pauseRequested, _cancelRequested, _cancelHadDropdown;
        private bool _previousGameplay, _previousMap, _journalWasEnabled, _previousCursorVisible;
        private CursorLockMode _previousCursor;
        private GameObject _previousSelection;
        private float _previousTimeScale;
        private GameObject _panel;
        public bool IsOpen { get; private set; }
        public event Action<bool> PauseChanged;
        public bool IsConfigured => _player != null && _input != null && _devices != null && _flow != null &&
            _events != null && _settings != null && _settings.IsConfigured && _overlay != null &&
            _mainPanel != null && _settingsPanel != null && _controlsPanel != null && _continue != null &&
            _configuration != null && _controls != null && _restart != null && _mainMenu != null && _quit != null &&
            _settingsBack != null && _controlsBack != null && _controlsText != null && _hints != null && _restartHint != null;
        public bool CanRestart => (_checkpointRestart is ICheckpointRestart checkpoint && checkpoint.CanRestartFromCheckpoint) ||
            (_restartTutorialFromStart && _tutorial != null);
        public bool CanOpen => IsConfigured && !_flow.IsTransitioning &&
            (_outcome == null || _outcome.State == LevelOutcome.Playing) &&
            (_tutorial == null || (_tutorial.State != TutorialSequenceState.Exiting && _tutorial.State != TutorialSequenceState.Completed)) &&
            (_player.GameplayActive || (_journal != null && _journal.IsOpen));

        private void OnEnable()
        {
            if (!IsConfigured) { Debug.LogError("Apply Gameplay Pause Menu authoring: missing references.", this); return; }
            _overlay.SetActive(false);
            _ui = new TikalInputActions();
            _ui.UI.Cancel.performed += OnCancel;
            _input.PausePressed += RequestPause;
            _devices.ActiveDeviceChanged += OnDeviceChanged;
            InputSystem.onActionChange += OnActionChange;
            if (_outcome != null) _outcome.OutcomeChanged += OnOutcome;
            _continue.onClick.AddListener(Resume);
            _configuration.onClick.AddListener(ShowSettings);
            _controls.onClick.AddListener(ShowControls);
            _restart.onClick.AddListener(Restart);
            _mainMenu.onClick.AddListener(MainMenu);
            _quit.onClick.AddListener(Quit);
            _settingsBack.onClick.AddListener(Back);
            _controlsBack.onClick.AddListener(Back);
            _subscribed = true;
        }

        private void OnDisable()
        {
            Close(true);
            if (!_subscribed) return;
            _input.PausePressed -= RequestPause;
            _devices.ActiveDeviceChanged -= OnDeviceChanged;
            InputSystem.onActionChange -= OnActionChange;
            if (_outcome != null) _outcome.OutcomeChanged -= OnOutcome;
            _continue.onClick.RemoveListener(Resume);
            _configuration.onClick.RemoveListener(ShowSettings);
            _controls.onClick.RemoveListener(ShowControls);
            _restart.onClick.RemoveListener(Restart);
            _mainMenu.onClick.RemoveListener(MainMenu);
            _quit.onClick.RemoveListener(Quit);
            _settingsBack.onClick.RemoveListener(Back);
            _controlsBack.onClick.RemoveListener(Back);
            _ui.UI.Cancel.performed -= OnCancel;
            _ui.Dispose(); _ui = null;
            _subscribed = false;
        }

        private void RequestPause() => _pauseRequested = true;
        private void OnCancel(InputAction.CallbackContext _) { if (IsOpen) { _cancelRequested = true; _cancelHadDropdown = _settings.HasOpenDropdown; } }
        // Queue events so Escape shared by Pause/Cancel produces exactly one operation.
        private void LateUpdate()
        {
            if (_pauseRequested) { _pauseRequested = _cancelRequested = false; if (IsOpen) Resume(); else Open(); }
            else if (_cancelRequested) { _cancelRequested = false; if (_cancelHadDropdown) _settings.CloseDropdowns(); else Back(); }
        }

        public void Open()
        {
            if (IsOpen || !_subscribed || !CanOpen) return;
            if (_journal != null) _journal.Close();
            _previousGameplay = _player.GameplayActive; _previousMap = _input.GameplayEnabled;
            _previousCursor = Cursor.lockState; _previousCursorVisible = Cursor.visible;
            _previousSelection = _events.currentSelectedGameObject;
            _previousTimeScale = Time.timeScale;
            _journalWasEnabled = _journal != null && _journal.enabled;
            if (_journal != null) _journal.enabled = false;
            _player.SetGameplayActive(false);
            Time.timeScale = 0;
            IsOpen = true; _ui.UI.Cancel.Enable();
            _overlay.SetActive(true);
            _restart.interactable = CanRestart;
            _restartHint.text = CanRestart ? (_restartTutorialFromStart ? "Reinicia el tutorial desde el inicio." : "Restaurar el checkpoint actual.") : "No hay un checkpoint disponible.";
            Show(_mainPanel, _continue.gameObject);
            PauseChanged?.Invoke(true);
        }

        public void Resume() => Close(true);
        private void Close(bool restoreGameplay)
        {
            _pauseRequested = _cancelRequested = false;
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = _previousTimeScale;
            if (_settings != null) _settings.CloseDropdowns();
            if (_overlay != null) _overlay.SetActive(false);
            _ui?.UI.Cancel.Disable();
            bool playable = (_outcome == null || _outcome.State == LevelOutcome.Playing) && (_flow == null || !_flow.IsTransitioning);
            if (restoreGameplay && playable && _player != null && _player.isActiveAndEnabled)
            {
                if (_journal != null) _journal.enabled = _journalWasEnabled;
                _player.SetGameplayActive(_previousGameplay);
                if (!_previousMap) _input.DisableGameplay();
                Cursor.lockState = _previousCursor; Cursor.visible = _previousCursorVisible;
                _events.SetSelectedGameObject(_previousSelection);
            }
            PauseChanged?.Invoke(false);
        }
        private void OnOutcome(LevelOutcome _) => Close(false);
        public void ShowSettings() { if (IsOpen) Show(_settingsPanel, _settings.FirstSelection); }
        public void ShowControls() { if (IsOpen) Show(_controlsPanel, _controlsBack.gameObject); }
        public void Back()
        {
            if (!IsOpen) return;
            if (_panel == _mainPanel) Resume();
            else Show(_mainPanel, _panel == _settingsPanel ? _configuration.gameObject : _controls.gameObject);
        }
        private void Show(GameObject panel, GameObject selection)
        {
            _mainPanel.SetActive(panel == _mainPanel); _settingsPanel.SetActive(panel == _settingsPanel); _controlsPanel.SetActive(panel == _controlsPanel);
            _panel = panel;
            _events.SetSelectedGameObject(null); _events.SetSelectedGameObject(selection);
            RefreshText();
        }
        private void OnDeviceChanged(InputDeviceFamily _) { if (IsOpen) RefreshText(); }
        private void OnActionChange(object _, InputActionChange change) { if (IsOpen && change == InputActionChange.BoundControlsChanged) RefreshText(); }
        private string Prompt(InputAction action) => InputPromptResolver.Resolve(action, _devices.ActiveDevice).DisplayText;
        private void RefreshText()
        {
            _hints.text = Prompt(_ui.UI.Submit) + "  SELECCIONAR     " + Prompt(_ui.UI.Cancel) + "  VOLVER";
            var text = new StringBuilder();
            var labels = new[] { "Moverse", "Mirar", "Saltar", "Correr", "Agacharse", "Interactuar", "Diario", "Pausa" };
            var actions = new[] { _input.MoveAction, _input.LookAction, _input.JumpAction, _input.SprintAction,
                _input.CrouchAction, _input.InteractAction, _input.JournalAction, _input.PauseAction };
            for (var i = 0; i < actions.Length; i++) text.Append(labels[i]).Append("   ").Append(Prompt(actions[i])).Append('\n');
            _controlsText.text = text.ToString();
        }
        public void Restart()
        {
            if (!IsOpen || !CanRestart) return;
            var checkpoint = _checkpointRestart as ICheckpointRestart;
            Resume();
            if (checkpoint != null && checkpoint.CanRestartFromCheckpoint) checkpoint.RestartFromCheckpoint();
            else if (_restartTutorialFromStart) _flow.RestartCurrentGameplayScene();
        }
        private void MainMenu() { if (!IsOpen) return; Close(false); Time.timeScale = 1; _flow.ReturnToMainMenu(); }
        private void Quit() { if (!IsOpen) return; Resume(); _flow.Quit(); }
    }
}
