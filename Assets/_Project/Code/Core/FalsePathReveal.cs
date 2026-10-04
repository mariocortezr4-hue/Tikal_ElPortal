using Tikal.Puzzles;
using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class FalsePathReveal : MonoBehaviour
    {
        [SerializeField] private PuzzleBase _puzzle;
        [SerializeField] private GameObject _gate;
        [SerializeField] private GameObject _path;
        public bool IsRevealed {get;private set;}
        private bool _subscribed;
        private void OnEnable()
        {
            if(_puzzle==null || _gate==null || _path==null)
            {Debug.LogError("False path requires puzzle, seal and path.",this);return;}
            _puzzle.PuzzleSolved+=Reveal;_subscribed=true;
            _path.SetActive(false);_gate.SetActive(true);
            if(_puzzle.IsSolved)Reveal(_puzzle);
        }
        private void Reveal(PuzzleBase _){IsRevealed=true;_gate.SetActive(false);_path.SetActive(true);}
        private void OnDisable(){if(_subscribed)_puzzle.PuzzleSolved-=Reveal;_subscribed=false;}
    }
}