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
        public LevelOutcome State { get; private set; } = LevelOutcome.Playing;
        public event Action<LevelOutcome> OutcomeChanged;
        private bool _restarting;
        public bool TriggerVictory() => Finish(LevelOutcome.Victory);
        public bool TriggerDefeat() => Finish(LevelOutcome.Defeat);
        private bool Finish(LevelOutcome outcome)
        {
            if (!isActiveAndEnabled || State != LevelOutcome.Playing) return false;
            State = outcome;
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
            _restarting = true;
            SceneManager.LoadScene(gameObject.scene.name,LoadSceneMode.Single);
        }
    }
}