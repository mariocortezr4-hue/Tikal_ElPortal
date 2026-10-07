using System.Collections;
using Tikal.Input;
using Tikal.Puzzles.Clues;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class ClueDiscoveryPresenter : MonoBehaviour
    {
        [SerializeField] private ClueTracker _tracker;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;
        [SerializeField, Min(1)] private float _visibleSeconds=6;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private InputDeviceManager _devices;
        private ClueDefinition _currentClue;
        private Coroutine _dismiss;
        private bool _subscribed;
        private void OnEnable()
        {
            if(_tracker==null||_panel==null||_title==null||_description==null||_inputReader==null||_devices==null)
            { Debug.LogError("Clue presentation requires tracker, visuals, input reader and active device manager.",this); return; }
            _tracker.ClueDiscovered+=Show; if(_devices!=null)_devices.ActiveDeviceChanged+=OnDeviceChanged; _subscribed=true; _panel.SetActive(false);
            InputSystem.onActionChange += OnActionChange;
        }
        private void OnDisable()
        {
            if(_subscribed&&_tracker!=null) _tracker.ClueDiscovered-=Show;
            if(_subscribed) InputSystem.onActionChange -= OnActionChange;
            if(_subscribed&&_devices!=null)_devices.ActiveDeviceChanged-=OnDeviceChanged;
            _subscribed=false;
            if(_dismiss!=null) StopCoroutine(_dismiss);
            _dismiss=null;
            if(_panel!=null) _panel.SetActive(false);
        }
        private void Show(ClueDefinition clue)
        {
            if(_dismiss!=null) StopCoroutine(_dismiss);
            _currentClue=clue;RenderClue();
            _panel.SetActive(true); _dismiss=StartCoroutine(Dismiss());
        }
        private void OnDeviceChanged(InputDeviceFamily _){if(_panel.activeSelf&&_currentClue!=null)RenderClue();}
        private void OnActionChange(object source, InputActionChange change)
        {
            if (_currentClue != null && _panel.activeSelf &&
                CluePresentation.IsBindingChange(source, change, _inputReader.SecretCommandAction)) RenderClue();
        }
        private void RenderClue(){_title.text=_currentClue.SemanticContent==ClueSemanticContent.SecretCommand?"DESCUBRIMIENTO\n"+_currentClue.Title.ToUpperInvariant():_currentClue.Title;_description.text=CluePresentation.Description(_currentClue,_inputReader,_devices);}
        private IEnumerator Dismiss()
        {
            yield return new WaitForSecondsRealtime(_visibleSeconds);
            _panel.SetActive(false); _dismiss=null;
        }
    }
}
