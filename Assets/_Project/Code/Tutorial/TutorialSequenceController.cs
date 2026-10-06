using System;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using UnityEngine;

namespace Tikal.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private TutorialStep[] _steps;
        [SerializeField, Range(.2f, 2f)] private float _completionFeedbackSeconds = .8f;
        private int _index = -1;
        private bool _finishing;

        public IReadOnlyList<TutorialStep> Steps => _steps;
        public TutorialStep CurrentStep => _index >= 0 && _index < _steps.Length ? _steps[_index] : null;
        public event Action<TutorialStep> StepActivated;
        public event Action<TutorialStep> StepCompleted;
        public event Action SequenceCompleted;

        private void Start() => Advance();
        private void Update() => CurrentStep?.TickStep(Time.deltaTime);

        private void Advance()
        {
            _index++;
            if (_index >= _steps.Length)
            {
                if (_finishing) return;
                _finishing = true;
                SequenceCompleted?.Invoke();
                _flow.CompleteTutorial();
                return;
            }
            var next = CurrentStep;
            if (next == null) throw new InvalidOperationException("Tutorial sequence contains a missing step.");
            next.Completed += OnStepCompleted;
            next.ActivateStep();
            StepActivated?.Invoke(next);
        }

        private void OnStepCompleted(TutorialStep step)
        {
            step.Completed -= OnStepCompleted;
            StepCompleted?.Invoke(step);
            StartCoroutine(AdvanceAfterFeedback());
        }

        private IEnumerator AdvanceAfterFeedback()
        {
            yield return new WaitForSeconds(_completionFeedbackSeconds);
            Advance();
        }
    }
}
