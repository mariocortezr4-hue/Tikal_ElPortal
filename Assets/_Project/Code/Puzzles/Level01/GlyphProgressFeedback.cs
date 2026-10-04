using System.Collections;
using UnityEngine;

namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class GlyphProgressFeedback : MonoBehaviour
    {
        [SerializeField] private GlyphSequencePuzzle _puzzle;
        [SerializeField] private Renderer[] _marks = new Renderer[7];
        [SerializeField] private Renderer _frame;
        private MaterialPropertyBlock _block;
        private Coroutine _error;
        private bool _subscribed;

        private void OnEnable()
        {
            if (_puzzle == null || _frame == null || _marks == null || _marks.Length != 7)
            { Debug.LogError("Glyph feedback requires a puzzle, a frame and seven marks.", this); return; }
            foreach (var mark in _marks)
                if (mark == null) { Debug.LogError("Glyph feedback has a missing mark.", this); return; }
            _block = new MaterialPropertyBlock();
            _puzzle.ProgressChanged += Refresh;
            _puzzle.WrongSelection += Error;
            _subscribed = true;
            Refresh(_puzzle);
        }

        private void Start() { if (_subscribed) Refresh(_puzzle); }
        private void Paint(Renderer renderer, Color colour)
        {
            _block.SetColor("_EmissionColor", colour);
            renderer.SetPropertyBlock(_block);
        }

        private void Refresh(GlyphSequencePuzzle puzzle)
        {
            if (_error != null) StopCoroutine(_error);
            _error = null;
            for (int index = 0; index < 7; index++)
                Paint(_marks[index], index < puzzle.Progress ? new Color(.8f, .48f, .12f) : Color.black);
            Paint(_frame, puzzle.IsSolved ? new Color(.18f, .11f, .035f) : Color.black);
        }

        private void Error(GlyphSequencePuzzle puzzle)
        {
            if (_error != null) StopCoroutine(_error);
            _error = StartCoroutine(ErrorPulse());
        }

        private IEnumerator ErrorPulse()
        {
            // Only the frame pulses; progress marks stay cleared and retry is never blocked.
            Paint(_frame, new Color(.15f, .045f, .01f));
            yield return new WaitForSecondsRealtime(.18f);
            _error = null;
            Refresh(_puzzle);
        }

        private void OnDisable()
        {
            if (_subscribed && _puzzle != null)
            {
                _puzzle.ProgressChanged -= Refresh;
                _puzzle.WrongSelection -= Error;
            }
            _subscribed = false;
            if (_error != null) StopCoroutine(_error);
            _error = null;
        }
    }
}
