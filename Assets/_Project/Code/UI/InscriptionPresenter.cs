using System.Collections;
using Tikal.Interaction;
using TMPro;
using UnityEngine;
namespace Tikal.UI
{
    public sealed class InscriptionPresenter : MonoBehaviour
    {
        [SerializeField] private ReadableInscription[] _inscriptions;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _text;
        [SerializeField, Min(1)] private float _duration=5;
        private Coroutine _dismiss;
        private bool _subscribed;
        private void OnEnable()
        {
            if(_inscriptions==null||_panel==null||_title==null||_text==null){Debug.LogError("Inscription presentation requires sources and visuals.",this);return;}
            foreach(var inscription in _inscriptions)if(inscription==null){Debug.LogError("Missing readable source.",this);return;}
            foreach(var inscription in _inscriptions)inscription.ReadRequested+=Show;
            _subscribed=true;_panel.SetActive(false);
        }
        private void OnDisable()
        {
            if(_subscribed)foreach(var inscription in _inscriptions)if(inscription!=null)inscription.ReadRequested-=Show;
            _subscribed=false;if(_dismiss!=null)StopCoroutine(_dismiss);_dismiss=null;
            if(_panel!=null)_panel.SetActive(false);
        }
        private void Show(ReadableInscription inscription)
        {
            _title.text=inscription.Title;_text.text=inscription.Text;_panel.SetActive(true);
            if(_dismiss!=null)StopCoroutine(_dismiss);_dismiss=StartCoroutine(Dismiss());
        }
        private IEnumerator Dismiss(){yield return new WaitForSecondsRealtime(_duration);_panel.SetActive(false);_dismiss=null;}
    }
}
