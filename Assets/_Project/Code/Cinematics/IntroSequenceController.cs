using System;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Tikal.Cinematics
{
    [DisallowMultipleComponent]
    public sealed class IntroSequenceController : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private CinematicSubtitlePresenter _subtitles;
        [SerializeField, Range(1f, 1.5f)] private float _skipHoldSeconds = 1.25f;
        private readonly HashSet<string> _receivedEvents = new HashSet<string>();
        private TikalInputActions _actions;
        private float _skipProgress;
        private bool _completed;

        public PlayableDirector Director => _director;
        public bool HasRequiredReferences => _flow != null && _director != null &&
            _director.playableAsset != null && _subtitles != null && _subtitles.HasRequiredReferences;
        public InputAction SkipAction { get { EnsureActions(); return _actions.UI.Cancel; } }
        public bool IsReplay => _flow != null && _flow.IsIntroReplay;
        public bool AncientMapWasReceived { get; private set; }
        public float SkipHoldSeconds => _skipHoldSeconds;
        public event Action<float> SkipProgressChanged;
        public event Action AncientMapReceived;

        private void Awake() => EnsureActions();

        private void OnEnable()
        {
            EnsureActions();
            _actions.UI.Enable();
            if (_director != null) _director.stopped += OnDirectorStopped;
        }

        private void Start()
        {
            if (!HasRequiredReferences)
                throw new InvalidOperationException("Ancient Map intro is missing required references.");
            _director.Play();
        }

        private void Update()
        {
            if (_completed) return;
            var held = SkipAction.IsPressed();
            var next = held ? Mathf.Min(_skipHoldSeconds, _skipProgress + Time.unscaledDeltaTime) : 0f;
            if (!Mathf.Approximately(next, _skipProgress))
            {
                _skipProgress = next;
                SkipProgressChanged?.Invoke(Mathf.Clamp01(_skipProgress / _skipHoldSeconds));
            }
            if (_skipProgress >= _skipHoldSeconds) CompleteIntro();
        }

        private void OnDisable()
        {
            if (_director != null) _director.stopped -= OnDirectorStopped;
            if (_actions != null) _actions.UI.Disable();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
            _actions = null;
        }

        public void ApplyCue(string cueId, CinematicCueType type, string speaker, string text)
        {
            if (_completed) return;
            if (type == CinematicCueType.AncientMapReceived)
            {
                if (_receivedEvents.Add(cueId))
                {
                    AncientMapWasReceived = true;
                    AncientMapReceived?.Invoke();
                }
                return;
            }
            _subtitles.Show(cueId, type, speaker, text);
        }

        public void ReleaseCue(string cueId) => _subtitles.Release(cueId);

        private void OnDirectorStopped(PlayableDirector _) => CompleteIntro();

        private void CompleteIntro()
        {
            if (_completed) return;
            _completed = true;
            _actions.UI.Disable();
            if (_director != null)
            {
                _director.stopped -= OnDirectorStopped;
                if (_director.state == PlayState.Playing) _director.Stop();
            }
            _flow.CompleteIntro();
        }

        private void EnsureActions()
        {
            if (_actions == null) _actions = new TikalInputActions();
        }
    }
}
