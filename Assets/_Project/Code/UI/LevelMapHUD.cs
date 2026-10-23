using Tikal.Core;
using Tikal.Input;
using Tikal.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tikal.UI
{
    [DisallowMultipleComponent,DefaultExecutionOrder(-150)]
    public sealed class LevelMapHUD : MonoBehaviour
    {
        [SerializeField] PlayerController _player;
        [SerializeField] PlayerInputReader _input;
        [SerializeField] InputDeviceManager _devices;
        [SerializeField] ClueJournalPresenter _journal;
        [SerializeField] PauseMenuController _pause;
        [SerializeField] LevelOutcomeController _outcome;
        [SerializeField] CheckpointManager _checkpoints;
        [SerializeField] GameFlowController _flow;
        [SerializeField] LevelMapDefinition _definition;
        [SerializeField] GameObject _small,_expanded;
        [SerializeField] ExplorationMapGraphic _smallMap,_largeMap;
        [SerializeField] RectTransform _smallMarker,_largeMarker;
        [SerializeField] TMP_Text _mapHint,_journalHint,_closeHint;
        TikalInputActions _ui;
        InputAction _mapAction;
        bool _subscribed,_mapRequested,_cancelRequested,_pauseRequested,_previousGameplay,_previousInput,_previousCursorVisible;
        CursorLockMode _previousCursor;
        public bool IsOpen {get;private set;}
        public bool IsConfigured=>_player!=null&&_input!=null&&_devices!=null&&_journal!=null&&_pause!=null&&_definition!=null&&_small!=null&&_expanded!=null&&_smallMap!=null&&_largeMap!=null&&_smallMarker!=null&&_largeMarker!=null&&_mapHint!=null&&_journalHint!=null&&_closeHint!=null;
        public string MapHint=>_mapHint.text;
        public string JournalHint=>_journalHint.text;
        public Vector2 SmallMarkerPosition=>_smallMarker.anchoredPosition;
        public bool SmallVisible=>_small.activeSelf;
        public InputAction MapAction=>_mapAction;
        bool Playable=>(_outcome==null||_outcome.State==LevelOutcome.Playing)&&(_checkpoints==null||!_checkpoints.IsRespawning)&&(_flow==null||!_flow.IsTransitioning);
        void OnEnable(){if(_input!=null&&_input.MapAction!=null)Subscribe();}
        void Start()=>Subscribe(); // All input Awake/OnEnable calls have completed, irrespective of UI execution order.
        void Subscribe()
        {
            if(_subscribed)return;
            if(!IsConfigured){Debug.LogError("Apply Level01 Map HUD: missing references.",this);return;}
            _mapAction=_input.MapAction;if(_mapAction==null){Debug.LogError("Global/Map semantic action missing.",this);return;}
            _ui=new TikalInputActions();_ui.UI.Cancel.performed+=Cancel;
            _mapAction.performed+=Map;_input.PausePressed+=Pause;_journal.Opening+=BeforeJournal;
            _pause.PauseChanged+=PauseVisibilityChanged;
            _devices.ActiveDeviceChanged+=DeviceChanged;InputSystem.onActionChange+=BindingsChanged;
            if(_outcome!=null)_outcome.OutcomeChanged+=OutcomeChanged;
            if(_checkpoints!=null)_checkpoints.RespawnCompleted+=Respawned;
            _subscribed=true;IsOpen=false;_expanded.SetActive(false);RefreshHints();RefreshVisibility();UpdateMarkers();
        }
        void OnDisable()
        {
            Close();if(!_subscribed)return;_mapAction.performed-=Map;_input.PausePressed-=Pause;_journal.Opening-=BeforeJournal;
            _pause.PauseChanged-=PauseVisibilityChanged;
            _devices.ActiveDeviceChanged-=DeviceChanged;InputSystem.onActionChange-=BindingsChanged;
            if(_outcome!=null)_outcome.OutcomeChanged-=OutcomeChanged;if(_checkpoints!=null)_checkpoints.RespawnCompleted-=Respawned;
            _ui.UI.Cancel.performed-=Cancel;_ui.Dispose();_ui=null;_subscribed=false;_small.SetActive(false);
        }
        void Map(InputAction.CallbackContext _){if(!_pause.IsOpen&&Playable)_mapRequested=true;}
        void Cancel(InputAction.CallbackContext _)=>_cancelRequested=true;
        void Pause(){_pauseRequested=true;_mapRequested=false;Close();}
        void BeforeJournal(){_mapRequested=false;Close();}
        void PauseVisibilityChanged(bool open){if(open)Close();RefreshVisibility();}
        void OutcomeChanged(LevelOutcome _)=>Close();
        void Respawned(){if(IsOpen)Close();UpdateMarkers();}
        void DeviceChanged(InputDeviceFamily _)=>RefreshHints();
        void BindingsChanged(object source,InputActionChange change)
        {if(CluePresentation.IsBindingChange(source,change,_mapAction)||CluePresentation.IsBindingChange(source,change,_input.JournalAction))RefreshHints();}
        void LateUpdate()
        {
            if(!_subscribed)return;
            if(_pauseRequested){_pauseRequested=_mapRequested=_cancelRequested=false;}
            else if(_mapRequested){_mapRequested=_cancelRequested=false;if(IsOpen)Close();else Open();}
            else if(_cancelRequested){_cancelRequested=false;Close();}
            if(IsOpen&&(!Playable||_pause.IsOpen))Close();
            RefreshVisibility();UpdateMarkers();
        }
        public void Open()
        {
            if(IsOpen||!_subscribed||!Playable||_pause.IsOpen||!_journal.isActiveAndEnabled||(!_player.GameplayActive&&!_journal.IsOpen))return;
            _journal.Close();_previousGameplay=_player.GameplayActive;_previousInput=_input.GameplayEnabled;
            _previousCursor=Cursor.lockState;_previousCursorVisible=Cursor.visible;
            IsOpen=true;_player.SetGameplayActive(false);_expanded.SetActive(true);_ui.UI.Cancel.Enable();RefreshHints();RefreshVisibility();UpdateMarkers();
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;_expanded.SetActive(false);_ui?.UI.Cancel.Disable();_mapRequested=_cancelRequested=false;
            if(Playable&&_player!=null&&_player.isActiveAndEnabled)
            {_player.SetGameplayActive(_previousGameplay);if(!_previousInput)_input.DisableGameplay();Cursor.lockState=_previousCursor;Cursor.visible=_previousCursorVisible;}
            RefreshVisibility();
        }
        void RefreshVisibility()
        {if(_small!=null)_small.SetActive(!IsOpen&&Playable&&_pause!=null&&!_pause.IsOpen&&_journal!=null&&!_journal.IsOpen&&_player!=null&&_player.GameplayActive&&_input!=null&&_input.GameplayEnabled);}
        void RefreshHints()
        {
            var family=_devices.ActiveDevice;string map=InputPromptResolver.Resolve(_mapAction,family).DisplayText;string journal=InputPromptResolver.Resolve(_input.JournalAction,family).DisplayText;
            _mapHint.text="["+map+"] MAPA";_journalHint.text="["+journal+"] VER DIARIO";_closeHint.text="["+map+"] CERRAR   ·   "+InputPromptResolver.Resolve(_ui.UI.Cancel,family).DisplayText+" VOLVER";
        }
        void UpdateMarkers()
        {
            UpdateMarker(_smallMarker,_smallMap.rectTransform);UpdateMarker(_largeMarker,_largeMap.rectTransform);
        }
        void UpdateMarker(RectTransform marker,RectTransform map)
        {
            var point=_definition.LocalPoint(_player.transform.position,map.rect);if((marker.anchoredPosition-point).sqrMagnitude>.00001f)marker.anchoredPosition=point;
            marker.localRotation=Quaternion.Euler(0,0,-_player.transform.eulerAngles.y);
        }
    }
}
