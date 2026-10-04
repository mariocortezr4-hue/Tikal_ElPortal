using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class PointOfNoReturnTrigger : MonoBehaviour
    {
        [SerializeField] private CharacterController _player;
        [SerializeField] private LevelOutcomeController _outcome;
        [SerializeField] private GameObject _closingPassage;
        private bool _triggered;
        private void OnTriggerEnter(Collider other)
        {
            if(_triggered || other!=_player || _outcome.State!=LevelOutcome.Playing)return;
            _triggered=true;
            _closingPassage.SetActive(true);
            _outcome.TriggerDefeat();
        }
    }
}