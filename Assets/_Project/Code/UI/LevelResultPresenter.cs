using System.Collections;
using Tikal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelResultPresenter : MonoBehaviour
    {
        [SerializeField] private LevelOutcomeController _outcome;
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Button _retry;
        [SerializeField] private EventSystem _events;
        private Coroutine _transition;
        public bool IsVisible=>_panel.activeSelf;
        private void OnEnable()
        {
            _panel.SetActive(false);
            _outcome.OutcomeChanged+=Show;
            _retry.onClick.AddListener(_outcome.RestartLevel);
            if(_outcome.State!=LevelOutcome.Playing)Show(_outcome.State);
        }
        private void OnDisable()
        {
            _outcome.OutcomeChanged-=Show;
            _retry.onClick.RemoveListener(_outcome.RestartLevel);
            if(_transition!=null)StopCoroutine(_transition);
        }
        private void Show(LevelOutcome outcome)
        {
            if (_transition != null) { StopCoroutine(_transition); _transition = null; }
            if (outcome == LevelOutcome.Playing)
            {
                if (_events.currentSelectedGameObject != null && _events.currentSelectedGameObject.transform.IsChildOf(_panel.transform))
                    _events.SetSelectedGameObject(null);
                _panel.SetActive(false); _fade.alpha = 0;
                return;
            }
            if (outcome == LevelOutcome.Victory && Tikal.Cinematics.Level01PlazaTransition.TryBegin(_outcome)) return;
            _title.text=outcome==LevelOutcome.Victory?"NIVEL COMPLETADO":"HAS QUEDADO ATRAPADO";
            _subtitle.text=outcome==LevelOutcome.Victory?"La Entrada Secreta":"";
            _panel.SetActive(true);_fade.alpha=0;
            _events.SetSelectedGameObject(_retry.gameObject);
            _transition=StartCoroutine(FadeIn());
        }
        private IEnumerator FadeIn()
        {
            float start=Time.realtimeSinceStartup;
            while(_fade.alpha<1){_fade.alpha=Mathf.Clamp01((Time.realtimeSinceStartup-start)/.45f);yield return null;}
            _transition=null;
        }
    }
}
