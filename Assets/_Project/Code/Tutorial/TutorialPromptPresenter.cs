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

        private void OnEnable()
        {
            _sequence.StepActivated += OnStepActivated;
            _sequence.StepCompleted += OnStepCompleted;
            _devices.ActiveDeviceChanged += OnDeviceChanged;
            if (_completionRoot != null) _completionRoot.SetActive(false);
            if (_sequence.CurrentStep != null) Refresh(_sequence.CurrentStep);
        }
        private void OnDisable()
        {
            _sequence.StepActivated -= OnStepActivated;
            _sequence.StepCompleted -= OnStepCompleted;
            _devices.ActiveDeviceChanged -= OnDeviceChanged;
        }
        private void OnStepActivated(TutorialStep step) => Refresh(step);
        private void OnStepCompleted(TutorialStep step)
        {
            if (_objectiveRoot != null) _objectiveRoot.SetActive(false);
            if (_completion != null) _completion.text = "COMPLETADO  ·  " + step.Title;
            if (_completionRoot != null) _completionRoot.SetActive(true);
        }
        private void OnDeviceChanged(InputDeviceFamily _) { if (_sequence.CurrentStep != null) Refresh(_sequence.CurrentStep); }
        private void Refresh(TutorialStep step)
        {
            var prompt = InputPromptResolver.Resolve(ActionFor(step.PromptAction), _devices.ActiveDevice).DisplayText;
            if (_title != null) _title.text = step.Title;
            if (_control != null) _control.text = "[ " + prompt + " ]";
            if (_instruction != null) _instruction.text = step.Instruction;
            if (_completionRoot != null) _completionRoot.SetActive(false);
            if (_objectiveRoot != null) _objectiveRoot.SetActive(true);
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
