using UnityEngine;
using Tikal.Puzzles;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class PointOfNoReturnTrigger : MonoBehaviour
    {
        [SerializeField] private CharacterController _player;
        [SerializeField] private LevelOutcomeController _outcome;
        [SerializeField] private GameObject _closingPassage;
        [SerializeField] private PuzzleBase _requiredPuzzle;
        private bool _triggered;
        private void OnTriggerEnter(Collider other)
        {
            if(_triggered || other!=_player || _outcome.State!=LevelOutcome.Playing || _requiredPuzzle==null || !_requiredPuzzle.IsSolved)return;
            _triggered=true;
            _closingPassage.SetActive(true);
            _outcome.TriggerDefeat();
        }
    }
}