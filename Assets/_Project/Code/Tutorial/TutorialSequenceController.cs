using System;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using UnityEngine;

namespace Tikal.Tutorial
{
    public enum TutorialSequenceState { NotStarted, Running, StepFeedback, AwaitingExit, Exiting, Completed }

    [DisallowMultipleComponent]
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private TutorialStep[] _steps;
        [SerializeField] private TutorialExitGate _exitGate;
        [SerializeField, Range(.2f, 2f)] private float _completionFeedbackSeconds = .8f;
        [SerializeField, Range(.2f, 2f)] private float _exitFeedbackSeconds = .9f;
        private int _index = -1;

        public IReadOnlyList<TutorialStep> Steps => _steps;
        public TutorialStep CurrentStep => _index >= 0 && _index < _steps.Length ? _steps[_index] : null;
        public TutorialExitGate ExitGate => _exitGate;
        public TutorialSequenceState State { get; private set; }
        public bool AllControlStepsCompleted
        {
            get
            {
                if (_steps == null || _steps.Length == 0) return false;
                for (var index = 0; index < _steps.Length; index++)
                    if (_steps[index] == null || _steps[index].State != TutorialStepState.Completed) return false;
                return true;
            }
        }
        public event Action<TutorialStep> StepActivated;
        public event Action<TutorialStep> StepCompleted;
        public event Action AwaitingExit;
        public event Action ExitReached;
        public event Action SequenceCompleted;

        private void Start()
        {
            if (_flow == null || _exitGate == null || _steps == null || _steps.Length == 0)
                throw new InvalidOperationException("Tutorial sequence is missing required references.");
            _exitGate.SetExitAvailable(false);
            _exitGate.ExitTriggered += OnExitTriggered;
            Advance();
        }

        private void OnDestroy()
        {
            if (_exitGate != null) _exitGate.ExitTriggered -= OnExitTriggered;
        }

        private void Update()
        {
            if (State == TutorialSequenceState.Running) CurrentStep?.TickStep(Time.deltaTime);
        }

        private void Advance()
        {
            _index++;
            if (_index >= _steps.Length)
            {
                BeginAwaitingExit();
                return;
            }
            var next = CurrentStep;
            if (next == null) throw new InvalidOperationException("Tutorial sequence contains a missing step.");
            State = TutorialSequenceState.Running;
            next.Completed += OnStepCompleted;
            next.ActivateStep();
            StepActivated?.Invoke(next);
        }

        private void OnStepCompleted(TutorialStep step)
        {
            step.Completed -= OnStepCompleted;
            State = TutorialSequenceState.StepFeedback;
            StepCompleted?.Invoke(step);
            StartCoroutine(AdvanceAfterFeedback());
        }

        private IEnumerator AdvanceAfterFeedback()
        {
            yield return new WaitForSeconds(_completionFeedbackSeconds);
            Advance();
        }

        private void BeginAwaitingExit()
        {
            if (!AllControlStepsCompleted)
                throw new InvalidOperationException("Tutorial exit cannot activate before every control step is completed.");
            State = TutorialSequenceState.AwaitingExit;
            _exitGate.SetExitAvailable(true);
            AwaitingExit?.Invoke();
        }

        private void OnExitTriggered()
        {
            if (State != TutorialSequenceState.AwaitingExit || !AllControlStepsCompleted) return;
            _exitGate.SetExitAvailable(false);
            State = TutorialSequenceState.Exiting;
            ExitReached?.Invoke();
            StartCoroutine(CompleteAfterExitFeedback());
        }

        private IEnumerator CompleteAfterExitFeedback()
        {
            yield return new WaitForSeconds(_exitFeedbackSeconds);
            State = TutorialSequenceState.Completed;
            SequenceCompleted?.Invoke();
            _flow.CompleteTutorial();
        }
    }
}
