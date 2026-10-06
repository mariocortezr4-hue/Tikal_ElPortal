using System;
using UnityEngine;

namespace Tikal.Tutorial
{
    public enum TutorialStepState { Inactive, Active, Completed }
    public enum TutorialPromptAction { Move, Look, Jump, Crouch, Sprint, Interact }

    public abstract class TutorialStep : MonoBehaviour
    {
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _instruction;
        [SerializeField] private TutorialPromptAction _promptAction;
        public string Title => _title;
        public string Instruction => _instruction;
        public TutorialPromptAction PromptAction => _promptAction;
        public TutorialStepState State { get; private set; }
        public virtual bool HasRequiredReferences => true;
        public event Action<TutorialStep> Completed;

        internal void ActivateStep()
        {
            if (State != TutorialStepState.Inactive) return;
            State = TutorialStepState.Active;
            OnActivated();
        }

        internal void TickStep(float deltaTime)
        {
            if (State == TutorialStepState.Active) OnStepTick(deltaTime);
        }

        protected virtual void OnActivated() { }
        protected virtual void OnStepTick(float deltaTime) { }
        protected virtual void OnDeactivated() { }
        protected void Complete()
        {
            if (State != TutorialStepState.Active) return;
            State = TutorialStepState.Completed;
            OnDeactivated();
            Completed?.Invoke(this);
        }
    }

}
