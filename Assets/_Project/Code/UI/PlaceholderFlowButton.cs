using Tikal.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class PlaceholderFlowButton : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private bool _completeIntro;
        private Button _button;

        private void OnEnable()
        {
            _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(Complete);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Complete);
        }

        public void Complete()
        {
            if (_completeIntro) _flow.CompleteIntro();
            else _flow.CompleteTutorial();
        }
    }
}
