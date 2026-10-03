using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    public sealed class EchoSequencePuzzle : PuzzleBase
    {
        [SerializeField] private EchoNote[] _targetSequence={EchoNote.B,EchoNote.A,EchoNote.C};
        [SerializeField] private EchoSource _source;
        [SerializeField] private EchoStone[] _stones=new EchoStone[3];
        private bool _valid;
        public int Progress {get;private set;}
        public int SequenceLength=>_targetSequence.Length;
        public EchoNote TargetNoteAt(int index)=>_targetSequence[index];
        public event Action<EchoSequencePuzzle> ProgressChanged;
        public event Action<EchoSequencePuzzle> WrongNote;
        public bool CanListen=>_valid&&isActiveAndEnabled&&_source.isActiveAndEnabled&&!_source.IsPlaying&&(State==PuzzleState.Available||State==PuzzleState.Active);
        internal bool CanTouch(EchoStone stone)=>CanListen&&Array.IndexOf(_stones,stone)>=0;
        private void OnEnable()
        {
            _valid=ValidateConfiguration();
            if(!_valid)Debug.LogError("Echo puzzle requires exactly three valid notes, one source and three unique stones.",this);
        }
        private bool ValidateConfiguration()
        {
            if(_targetSequence==null||_targetSequence.Length!=3||_source==null||_source.Puzzle!=this||!_source.HasThreeTones||_stones==null||_stones.Length!=3)return false;
            foreach(var note in _targetSequence)if((int)note<0||(int)note>2)return false;
            var notes=new HashSet<EchoNote>();
            foreach(var stone in _stones)if(stone==null||stone.Puzzle!=this||!stone.HasTone||!notes.Add(stone.Note)||(int)stone.Note<0||(int)stone.Note>2)return false;
            return true;
        }
        public void Listen()
        {
            if(!CanListen)return;
            if(State==PuzzleState.Available&&!BeginPuzzle())return;
            foreach(var stone in _stones)stone.CancelTone();
            ClearProgress();_source.PlaySequence(this);
        }
        internal void Receive(EchoStone stone)
        {
            if(!CanTouch(stone))return;
            if(State==PuzzleState.Available&&!BeginPuzzle())return;
            if(stone.Note!=_targetSequence[Progress]){ClearProgress();WrongNote?.Invoke(this);return;}
            Progress++;
            if(Progress==3)TrySolve();
            ProgressChanged?.Invoke(this);
        }
        private void ClearProgress(){Progress=0;ProgressChanged?.Invoke(this);}
        protected override void OnPuzzleReset(){_source.CancelPlayback();foreach(var stone in _stones)stone.CancelTone();ClearProgress();}
        private void OnDisable(){if(_source!=null)_source.CancelPlayback();}
    }
}
