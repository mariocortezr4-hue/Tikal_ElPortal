using System.Collections;
using Tikal.Puzzles.Clues;
using TMPro;
using UnityEngine;
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
        private Coroutine _dismiss;
        private bool _subscribed;
        private void OnEnable()
        {
            if(_tracker==null||_panel==null||_title==null||_description==null)
            { Debug.LogError("Clue presentation requires tracker and visuals.",this); return; }
            _tracker.ClueDiscovered+=Show; _subscribed=true; _panel.SetActive(false);
        }
        private void OnDisable()
        {
            if(_subscribed&&_tracker!=null) _tracker.ClueDiscovered-=Show;
            _subscribed=false;
            if(_dismiss!=null) StopCoroutine(_dismiss);
            _dismiss=null;
            if(_panel!=null) _panel.SetActive(false);
        }
        private void Show(ClueDefinition clue)
        {
            if(_dismiss!=null) StopCoroutine(_dismiss);
            _title.text=clue.Title; _description.text=clue.Description;
            _panel.SetActive(true); _dismiss=StartCoroutine(Dismiss());
        }
        private IEnumerator Dismiss()
        {
            yield return new WaitForSecondsRealtime(_visibleSeconds);
            _panel.SetActive(false); _dismiss=null;
        }
    }
}
