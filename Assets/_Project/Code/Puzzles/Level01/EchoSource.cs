using System;
using System.Collections;
using Tikal.Interaction;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EchoSource : MonoBehaviour,IInteractable
    {
        [SerializeField] private EchoSequencePuzzle _puzzle;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip[] _tones=new AudioClip[3];
        [SerializeField,Range(.2f,1)] private float _gap=.3f;
        private Coroutine _playback;
        public EchoSequencePuzzle Puzzle=>_puzzle;
        public bool IsPlaying {get;private set;}
        public string InteractionText=>"Escuchar";
        public bool CanInteract=>isActiveAndEnabled&&_audio!=null&&_puzzle!=null&&_puzzle.CanListen;
        public bool HasThreeTones=>_audio!=null&&_tones!=null&&_tones.Length==3&&_tones[0]!=null&&_tones[1]!=null&&_tones[2]!=null;
        // Semantic hook for a future optional audio visualization; no visual subscriber is installed.
        public event Action<EchoNote> ReferenceNotePlayed;
        public event Action PlaybackFinished;
        public void Interact(){if(CanInteract)_puzzle.Listen();}
        internal void PlaySequence(EchoSequencePuzzle owner)
        {
            if(owner!=_puzzle||IsPlaying||!isActiveAndEnabled||!HasThreeTones)return;
            IsPlaying=true;_playback=StartCoroutine(Replay());
        }
        private IEnumerator Replay()
        {
            for(int i=0;i<_puzzle.SequenceLength;i++)
            {
                var note=_puzzle.TargetNoteAt(i);var clip=_tones[(int)note];_audio.PlayOneShot(clip);ReferenceNotePlayed?.Invoke(note);
                yield return new WaitForSecondsRealtime(clip.length+(i<2?_gap:0));
            }
            IsPlaying=false;_playback=null;PlaybackFinished?.Invoke();
        }
        public void CancelPlayback(){if(_playback!=null)StopCoroutine(_playback);_playback=null;IsPlaying=false;if(_audio!=null)_audio.Stop();}
        private void OnDisable()=>CancelPlayback();
    }
}
