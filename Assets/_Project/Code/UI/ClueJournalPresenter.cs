using System.Linq;
using System.Text;
using Tikal.Input;
using Tikal.Player;
using Tikal.Puzzles.Clues;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class ClueJournalPresenter : MonoBehaviour
    {
        [SerializeField] private ClueTracker _tracker;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private PlayerController _player;
        [SerializeField] private InputDeviceManager _devices;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _content;
        [SerializeField] private TMP_Text _closeHint;
        private bool _subscribed;
        private bool _previousGameplay;
        private bool _previousPlayerMap;
        private CursorLockMode _previousCursor;
        private bool _previousCursorVisible;
        public bool IsOpen {get;private set;}
        public event System.Action Opening;
        private void OnEnable()
        {
            if(_subscribed)return;
            if(_tracker==null||_inputReader==null||_player==null||_devices==null||_panel==null||_content==null||_closeHint==null){Debug.LogError("Clue journal requires tracker, player, input and UI references.",this);return;}
            _inputReader.JournalPressed+=Toggle;_tracker.ClueDiscovered+=OnDiscovered;_tracker.ProgressRestored+=OnRestored;_devices.ActiveDeviceChanged+=OnDeviceChanged;
            _subscribed=true;_panel.SetActive(false);IsOpen=false;
            InputSystem.onActionChange += OnActionChange;
        }
        private void OnDisable()
        {
            Close();if(!_subscribed)return;
            InputSystem.onActionChange -= OnActionChange;
            _inputReader.JournalPressed-=Toggle;_tracker.ClueDiscovered-=OnDiscovered;_tracker.ProgressRestored-=OnRestored;_devices.ActiveDeviceChanged-=OnDeviceChanged;_subscribed=false;
        }
        public void Toggle(){if(IsOpen)Close();else Open();}
        public void Open()
        {
            if(IsOpen||!_subscribed||!isActiveAndEnabled)return;
            Opening?.Invoke(); // A Level01 map relinquishes its modal lock before Journal snapshots it.
            _previousGameplay=_player.GameplayActive;_previousPlayerMap=_inputReader.GameplayEnabled;_previousCursor=Cursor.lockState;_previousCursorVisible=Cursor.visible;
            IsOpen=true;_player.SetGameplayActive(false);Refresh();RefreshHint();_panel.SetActive(true);
        }
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;if(_panel!=null)_panel.SetActive(false);if(_player==null)return;_player.SetGameplayActive(_previousGameplay);
            if(_player.isActiveAndEnabled){if(_previousPlayerMap)_inputReader.EnableGameplay();else _inputReader.DisableGameplay();Cursor.lockState=_previousCursor;Cursor.visible=_previousCursorVisible;}
        }
        private void OnDiscovered(ClueDefinition _){if(IsOpen)Refresh();}
        private void OnRestored(){if(IsOpen)Refresh();}
        private void OnDeviceChanged(InputDeviceFamily _){if(IsOpen){Refresh();RefreshHint();}}
        private void OnActionChange(object source, InputActionChange change)
        {
            if (!IsOpen) return;
            if (CluePresentation.IsBindingChange(source, change, _inputReader.SecretCommandAction)) Refresh();
            if (CluePresentation.IsBindingChange(source, change, _inputReader.JournalAction)) RefreshHint();
        }
        private void RefreshHint()
        {
            var control=InputPromptResolver.Resolve(_inputReader.JournalAction,_devices.ActiveDevice).DisplayText;
            _closeHint.text=string.IsNullOrEmpty(control)?"Diario de pistas":"[ "+control+" ]  Cerrar diario";
        }
        private void Refresh()
        {
            if(_tracker.Count==0){_content.text="Todavía no has descubierto pistas.";return;}
            var text=new StringBuilder();
            // OrderBy is stable: equal authored orders retain discovery order, including after snapshot restore.
            foreach(var clue in _tracker.DiscoveredClues.OrderBy(c=>c.Order))
            {
                if(text.Length>0)text.Append("\n\n\n");
                text.Append((clue.Title??string.Empty).ToUpperInvariant()).Append("\n\n").Append(CluePresentation.Description(clue,_inputReader,_devices));
            }
            _content.text=text.ToString();
        }
    }
}
