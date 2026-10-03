using Tikal.Puzzles.Clues;
using UnityEngine;
using Tikal.Input;
using Tikal.Player;
namespace Tikal.Puzzles.Debugging
{
    public sealed class PuzzleFrameworkDebugControls : MonoBehaviour
    {
        [SerializeField] private TestPuzzle _puzzle;
        [SerializeField] private ClueTracker _tracker;
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerInputReader _inputReader;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string _summary;
        private bool _exploring;
        private void Start() { if (_player != null) _player.SetGameplayActive(false); Refresh(); }
        private void ToggleExploration() { _exploring = !_exploring; if (_player != null) _player.SetGameplayActive(_exploring); }
        private void OnEnable()
        {
            if (_puzzle == null || _tracker == null) return;
            _puzzle.PuzzleStarted += RefreshPuzzle;
            _puzzle.PuzzleSolved += RefreshPuzzle;
            _puzzle.PuzzleReset += RefreshPuzzle;
            _tracker.ClueDiscovered += RefreshClue;
            if (_inputReader != null) _inputReader.PausePressed += ToggleExploration;
            Refresh();
        }
        private void OnDisable()
        {
            if (_puzzle != null) { _puzzle.PuzzleStarted -= RefreshPuzzle; _puzzle.PuzzleSolved -= RefreshPuzzle; _puzzle.PuzzleReset -= RefreshPuzzle; }
            if (_tracker != null) _tracker.ClueDiscovered -= RefreshClue;
            if (_inputReader != null) _inputReader.PausePressed -= ToggleExploration;
        }
        private void RefreshPuzzle(PuzzleBase _) => Refresh();
        private void RefreshClue(ClueDefinition _) => Refresh();
        private void Refresh() { _summary = $"TEST ONLY | {_puzzle.PuzzleId} | {_puzzle.State} | Clues: {_tracker.Count}"; }
        private void OnGUI()
        {
            if (_puzzle == null || _tracker == null) return;
            GUILayout.BeginArea(new Rect(16, 16, 540, 220), GUI.skin.box);
            GUILayout.Label(_summary);
            if (_exploring) { GUILayout.Label("Pause returns to test controls"); GUILayout.EndArea(); return; }
            if (GUILayout.Button("Explore / Pause returns to test controls")) ToggleExploration();
            if (GUILayout.Button("Make available")) { _puzzle.MakeAvailable(); Refresh(); }
            if (GUILayout.Button("Begin")) _puzzle.BeginPuzzle();
            if (GUILayout.Button("Solve / repeat solve")) _puzzle.CompleteForTesting();
            if (GUILayout.Button("Reset active puzzle")) _puzzle.ResetPuzzle();
            GUILayout.EndArea();
        }
#endif
    }
}
