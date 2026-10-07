using System;
using System.Collections;
using System.Collections.Generic;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.UI;
using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class CheckpointManager : MonoBehaviour, ICheckpointRestart
    {
        [SerializeField] private PlayerController _player = null;
        [SerializeField] private CharacterController _capsule = null;
        [SerializeField] private PlayerMovement _movement = null;
        [SerializeField] private PlayerLook _look = null;
        [SerializeField] private PlayerCameraController _camera = null;
        [SerializeField] private PlayerInteraction _interaction = null;
        [SerializeField] private LevelOutcomeController _outcome = null;
        [SerializeField] private GameFlowController _flow = null;
        [SerializeField] private PauseMenuController _pause = null;
        [SerializeField] private ClueJournalPresenter _journal = null;
        [SerializeField] private CanvasGroup _fade = null;
        [SerializeField] private CheckpointAnchor[] _checkpoints = Array.Empty<CheckpointAnchor>();
        [SerializeField] private CheckpointAnchor _initialCheckpoint = null;
        [SerializeField] private MonoBehaviour[] _resetTargets = Array.Empty<MonoBehaviour>();
        [SerializeField, Min(0)] private float _defeatMessageSeconds = 1.1f;
        [SerializeField, Min(0)] private float _fadeSeconds = .25f;
        private readonly Dictionary<string, CheckpointAnchor> _registered = new Dictionary<string, CheckpointAnchor>(StringComparer.Ordinal);
        private Coroutine _defeatRoutine;
        private bool _subscribed, _restoreJournal;
        public CheckpointAnchor CurrentCheckpoint { get; private set; }
        public bool IsRespawning { get; private set; }
        public CharacterController PlayerCollider => _capsule;
        public bool HasCheckpoint => IsRegistered(CurrentCheckpoint);
        public bool CanRestartFromCheckpoint => isActiveAndEnabled && IsConfigured && HasCheckpoint && !IsRespawning &&
            _outcome.State != LevelOutcome.Victory && !_flow.IsTransitioning;
        public event Action<CheckpointAnchor> CheckpointActivated;
        public event Action RespawnCompleted;
        public bool IsConfigured => _player != null && _capsule != null && _movement != null && _look != null &&
            _camera != null && _interaction != null && _outcome != null && _flow != null && _journal != null && _pause != null && _fade != null;

        private void Start()
        {
            var invalidIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in _checkpoints ?? Array.Empty<CheckpointAnchor>())
            {
                if (anchor == null || !anchor.IsValid || anchor.gameObject.scene != gameObject.scene) continue;
                if (invalidIds.Contains(anchor.CheckpointId)) continue;
                if (_registered.ContainsKey(anchor.CheckpointId))
                { _registered.Remove(anchor.CheckpointId); invalidIds.Add(anchor.CheckpointId); Debug.LogError("Duplicate checkpoint ID: " + anchor.CheckpointId, this); }
                else _registered.Add(anchor.CheckpointId, anchor);
            }
            Activate(_initialCheckpoint);
        }
        private void OnEnable()
        {
            if (!IsConfigured) { Debug.LogError("Apply Level01 Checkpoints: missing runtime references.", this); return; }
            ClearFade();
            _outcome.OutcomeChanged += OnOutcome; _subscribed = true;
        }
        private void OnDisable()
        {
            if (_subscribed) _outcome.OutcomeChanged -= OnOutcome;
            _subscribed = false;
            if (_defeatRoutine != null) StopCoroutine(_defeatRoutine);
            _defeatRoutine = null;
            if (IsRespawning && _outcome != null && _outcome.State == LevelOutcome.Playing)
            { if (_journal != null) _journal.enabled = _restoreJournal; _player.SetGameplayActive(true); }
            IsRespawning = false; ClearFade();
        }
        private bool IsRegistered(CheckpointAnchor anchor) => anchor != null && anchor.IsValid &&
            anchor.gameObject.scene == gameObject.scene && _registered.TryGetValue(anchor.CheckpointId, out var registered) && registered == anchor;
        public bool Activate(CheckpointAnchor anchor)
        {
            if (!isActiveAndEnabled || IsRespawning || !IsRegistered(anchor)) return false;
            if (CurrentCheckpoint == anchor) return true;
            CurrentCheckpoint = anchor; CheckpointActivated?.Invoke(anchor); return true;
        }
        public bool Register(CheckpointAnchor anchor)
        {
            if (!isActiveAndEnabled || IsRespawning || anchor == null || !anchor.IsValid || anchor.gameObject.scene != gameObject.scene) return false;
            if (_registered.TryGetValue(anchor.CheckpointId, out var existing) && existing != null) return existing == anchor;
            _registered[anchor.CheckpointId] = anchor; return true;
        }
        public bool Unregister(CheckpointAnchor anchor)
        {
            if (IsRespawning || anchor == CurrentCheckpoint || !IsRegistered(anchor)) return false;
            return _registered.Remove(anchor.CheckpointId);
        }
        public void RestartFromCheckpoint() => Respawn();
        public bool Respawn()
        {
            if (!isActiveAndEnabled || !IsConfigured || IsRespawning || _flow.IsTransitioning || _outcome.State == LevelOutcome.Victory) return false;
            if (!HasCheckpoint) { SafeFallback(); return false; }
            IsRespawning = true;
            try
            {
                if (!RestoreAtCheckpoint()) return false;
                FinishRespawn(); return true;
            }
            finally { IsRespawning = false; ClearFade(); }
        }
        private void OnOutcome(LevelOutcome outcome)
        {
            if (outcome == LevelOutcome.Defeat && HasCheckpoint && !IsRespawning && !_flow.IsTransitioning)
            { IsRespawning = true; _defeatRoutine = StartCoroutine(DefeatRespawn()); }
        }
        private IEnumerator DefeatRespawn()
        {
            try
            {
                yield return new WaitForSecondsRealtime(_defeatMessageSeconds);
                yield return Fade(1);
                if (!RestoreAtCheckpoint()) yield break;
                yield return Fade(0);
                FinishRespawn();
            }
            finally { IsRespawning = false; _defeatRoutine = null; ClearFade(); }
        }
        private bool RestoreAtCheckpoint()
        {
            try
            {
                if (!HasCheckpoint || _flow.IsTransitioning) { SafeFallback(); return false; }
                _pause.Resume(); _journal.Close();
                _restoreJournal = _journal.enabled; _journal.enabled = false;
                _player.SetGameplayActive(false); Time.timeScale = 1;
                _interaction.ClearFocusForRespawn();
                var capsuleEnabled = _capsule.enabled;
                _capsule.enabled = false;
                try
                {
                    _player.transform.SetPositionAndRotation(CurrentCheckpoint.Position, CurrentCheckpoint.Rotation);
                    _movement.ResetForRespawn();
                    _look.ResetAfterRespawn(_movement.EyeHeight, _movement.MaximumEyeHeight);
                }
                finally { _capsule.enabled = capsuleEnabled; }
                Physics.SyncTransforms();
                _camera.SnapAfterTeleport();
                foreach (var reset in _resetTargets ?? Array.Empty<MonoBehaviour>())
                    if (reset is IRespawnResettable resettable) resettable.OnPlayerRespawn();
                if (_outcome.RestoreAfterRespawn()) _restoreJournal = _journal.enabled;
                _journal.enabled = false; // Keep Global Journal out during the fade-in.
                return true;
            }
            catch (Exception exception) { Debug.LogException(exception, this); SafeFallback(); return false; }
        }
        private void FinishRespawn()
        {
            _journal.enabled = _restoreJournal;
            _player.SetGameplayActive(true);
            IsRespawning = false; ClearFade();
            RespawnCompleted?.Invoke();
        }
        private IEnumerator Fade(float target)
        {
            if (_fade == null) yield break;
            _fade.blocksRaycasts = true;
            float start = _fade.alpha, elapsed = 0;
            while (elapsed < _fadeSeconds)
            { elapsed += Time.unscaledDeltaTime; _fade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / _fadeSeconds)); yield return null; }
            _fade.alpha = target;
        }
        private void ClearFade() { if (_fade != null) { _fade.alpha = 0; _fade.blocksRaycasts = false; } }
        private void SafeFallback()
        {
            Time.timeScale = 1; ClearFade();
            if (_flow != null && !_flow.IsTransitioning) _flow.RestartCurrentGameplayScene();
        }
    }
}
