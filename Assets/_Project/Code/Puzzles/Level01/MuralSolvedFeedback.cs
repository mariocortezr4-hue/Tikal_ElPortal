using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    public sealed class MuralSolvedFeedback : MonoBehaviour
    {
        [SerializeField] private MuralFragmentsPuzzle _puzzle;
        [SerializeField] private Light _light;
        private void OnEnable()
        {
            if(_puzzle==null||_light==null) { Debug.LogError("Mural feedback references required.",this); return; }
            _puzzle.PuzzleSolved+=Refresh; _puzzle.PuzzleReset+=Refresh; Refresh(_puzzle);
        }
        private void OnDisable()
        {
            if(_puzzle==null) return;
            _puzzle.PuzzleSolved-=Refresh; _puzzle.PuzzleReset-=Refresh;
        }
        private void Refresh(PuzzleBase _) { if(_light!=null) _light.enabled=_puzzle.IsSolved; }
    }
}
