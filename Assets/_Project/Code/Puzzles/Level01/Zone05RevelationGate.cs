using System;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class Zone05RevelationGate : MonoBehaviour
    {
        [SerializeField] private PuzzleBase _prerequisite;
        [SerializeField] private SecretKeyDiscoveryPuzzle _discovery;
        [SerializeField] private GameObject _content;
        private bool _subscribed;
        public bool IsRevealed {get;private set;}
        public event Action Revealed;
        private void OnEnable()
        {
            if(_prerequisite==null || _discovery==null || _content==null)
            {Debug.LogError("Zone05 requires observation puzzle, discovery and narrative content.",this);return;}
            _prerequisite.PuzzleSolved+=Reveal;_subscribed=true;
            if(_prerequisite.IsSolved)Reveal(_prerequisite);else _content.SetActive(false);
        }
        private void Reveal(PuzzleBase _)
        {
            if(IsRevealed)return;
            IsRevealed=true;_content.SetActive(true);_discovery.MakeAvailable();Revealed?.Invoke();
        }
        private void OnDisable(){if(_subscribed)_prerequisite.PuzzleSolved-=Reveal;_subscribed=false;}
    }
}