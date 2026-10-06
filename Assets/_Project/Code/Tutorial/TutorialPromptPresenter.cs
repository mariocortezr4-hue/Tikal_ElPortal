using System.Collections;
using Tikal.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tikal.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialPromptPresenter : MonoBehaviour
    {
        [SerializeField] private TutorialSequenceController _sequence;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private InputDeviceManager _devices;
        [SerializeField] private GameObject _objectiveRoot;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _control;
        [SerializeField] private TMP_Text _instruction;
        [SerializeField] private GameObject _completionRoot;
        [SerializeField] private TMP_Text _completion;
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField, Range(.1f, 1f)] private float _fadeDelay = .25f;
        [SerializeField, Range(.1f, 1f)] private float _fadeDuration = .5f;

        public TutorialSequenceController Sequence => _sequence;
        public bool HasRequiredReferences => _sequence != null && _input != null && _devices != null &&
            _objectiveRoot != null && _title != null && _control != null && _instruction != null &&
            _completionRoot != null && _completion != null && _fadeOverlay != null;
        public bool RemainsActiveDuringFeedback => _objectiveRoot != gameObject && _completionRoot != gameObject;

        private void OnEnable()
        {
            _sequence.StepActivated += OnStepActivated;
            _sequence.StepCompleted += OnStepCompleted;
            _sequence.AwaitingExit += OnAwaitingExit;
            _sequence.ExitReached += OnExitReached;
            _devices.ActiveDeviceChanged += OnDeviceChanged;
            if (_completionRoot != null) _completionRoot.SetActive(false);
            if (_fadeOverlay != null)
            {
                _fadeOverlay.alpha = 0;
                _fadeOverlay.gameObject.SetActive(false);
            }
            RefreshCurrentPresentation();
        }
        private void OnDisable()
        {
            _sequence.StepActivated -= OnStepActivated;
            _sequence.StepCompleted -= OnStepCompleted;
            _sequence.AwaitingExit -= OnAwaitingExit;
            _sequence.ExitReached -= OnExitReached;
            _devices.ActiveDeviceChanged -= OnDeviceChanged;
        }
        private void OnStepActivated(TutorialStep step) => Refresh(step);
        private void OnStepCompleted(TutorialStep step)
        {
            if (_objectiveRoot != null) _objectiveRoot.SetActive(false);
            if (_completion != null) _completion.text = "✓ COMPLETADO";
            if (_completionRoot != null) _completionRoot.SetActive(true);
        }

        private void OnAwaitingExit()
        {
            if (_completionRoot != null) _completionRoot.SetActive(false);
            if (_title != null) _title.text = "CONTINÚA POR EL SENDERO";
            if (_control != null)
            {
                _control.text = string.Empty;
                _control.gameObject.SetActive(false);
            }
            if (_instruction != null) _instruction.text = "Avanza hacia la entrada iluminada.";
            if (_objectiveRoot != null) _objectiveRoot.SetActive(true);
        }

        private void OnExitReached()
        {
            if (_objectiveRoot != null) _objectiveRoot.SetActive(false);
            if (_completion != null) _completion.text = "TUTORIAL COMPLETADO";
            if (_completionRoot != null) _completionRoot.SetActive(true);
            StartCoroutine(FadeForSceneTransition());
        }

        private void OnDeviceChanged(InputDeviceFamily _)
        {
            if (_sequence.State == TutorialSequenceState.Running && _sequence.CurrentStep != null)
                Refresh(_sequence.CurrentStep);
        }

        private void RefreshCurrentPresentation()
        {
            if (_sequence.State == TutorialSequenceState.AwaitingExit) { OnAwaitingExit(); return; }
            if (_sequence.State == TutorialSequenceState.Exiting || _sequence.State == TutorialSequenceState.Completed) { OnExitReached(); return; }
            var step = _sequence.CurrentStep;
            if (step == null && _sequence.Steps != null && _sequence.Steps.Count > 0) step = _sequence.Steps[0];
            if (step != null) Refresh(step);
        }

        private void Refresh(TutorialStep step)
        {
            var prompt = InputPromptResolver.Resolve(ActionFor(step.PromptAction), _devices.ActiveDevice).DisplayText;
            if (_title != null) _title.text = step.Title;
            if (_control != null)
            {
                _control.gameObject.SetActive(true);
                _control.text = "[ " + (prompt ?? string.Empty).ToUpperInvariant() + " ]";
            }
            if (_instruction != null) _instruction.text = step.Instruction;
            if (_completionRoot != null) _completionRoot.SetActive(false);
            if (_objectiveRoot != null) _objectiveRoot.SetActive(true);
        }

        private IEnumerator FadeForSceneTransition()
        {
            if (_fadeOverlay == null) yield break;
            yield return new WaitForSeconds(_fadeDelay);
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.alpha = 0;
            var elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
        }

        private InputAction ActionFor(TutorialPromptAction action)
        {
            switch (action)
            {
                case TutorialPromptAction.Move: return _input.MoveAction;
                case TutorialPromptAction.Look: return _input.LookAction;
                case TutorialPromptAction.Jump: return _input.JumpAction;
                case TutorialPromptAction.Crouch: return _input.CrouchAction;
                case TutorialPromptAction.Sprint: return _input.SprintAction;
                default: return _input.InteractAction;
            }
        }
    }
}
