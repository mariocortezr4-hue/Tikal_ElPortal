using System;
using Tikal.Player;
using Tikal.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Core
{
    public enum LevelOutcome { Playing, Victory, Defeat }

    [DisallowMultipleComponent]
    public sealed class LevelOutcomeController : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private ClueJournalPresenter _journal;
        [SerializeField] private InteractionPromptPresenter _prompt;
        [SerializeField] private InscriptionPresenter _inscription;
        [SerializeField] private ClueDiscoveryPresenter _discovery;
        [SerializeField] private Renderer _victorySeal;
        [SerializeField] private CheckpointManager _checkpoints = null;
        private bool _journalEnabled, _promptEnabled, _inscriptionEnabled, _discoveryEnabled;
        public bool UsesCheckpointRespawn => _checkpoints != null && _checkpoints.isActiveAndEnabled && _checkpoints.IsConfigured && _checkpoints.HasCheckpoint;
        public LevelOutcome State { get; private set; } = LevelOutcome.Playing;
        public event Action<LevelOutcome> OutcomeChanged;
        private bool _restarting;
        public bool TriggerVictory() => Finish(LevelOutcome.Victory);
        public bool TriggerDefeat() => Finish(LevelOutcome.Defeat);
        private bool Finish(LevelOutcome outcome)
        {
            if (!isActiveAndEnabled || State != LevelOutcome.Playing) return false;
            State = outcome;
            _journalEnabled = _journal.enabled; _promptEnabled = _prompt.enabled;
            _inscriptionEnabled = _inscription.enabled; _discoveryEnabled = _discovery.enabled;
            _journal.Close();
            _journal.enabled = false;
            _player.SetGameplayActive(false);
            _prompt.enabled = false;
            _inscription.enabled = false;
            _discovery.enabled = false;
            if (outcome == LevelOutcome.Victory)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_EmissionColor",new Color(.8f,.45f,.13f));
                _victorySeal.SetPropertyBlock(block);
            }
            OutcomeChanged?.Invoke(outcome);
            return true;
        }
        public void RestartLevel()
        {
            if (State == LevelOutcome.Playing || _restarting) return;
            if (State == LevelOutcome.Defeat && UsesCheckpointRespawn)
            {
                if (!_checkpoints.IsRespawning) _checkpoints.Respawn();
                return;
            }
            _restarting = true;
            Time.timeScale = 1;
            SceneManager.LoadScene(gameObject.scene.name,LoadSceneMode.Single);
        }

        public bool RestoreAfterRespawn()
        {
            if (State != LevelOutcome.Defeat) return false;
            State = LevelOutcome.Playing; _restarting = false;
            _journal.enabled = _journalEnabled; _prompt.enabled = _promptEnabled;
            _inscription.enabled = _inscriptionEnabled; _discovery.enabled = _discoveryEnabled;
            OutcomeChanged?.Invoke(State);
            return true;
        }
    }
}
