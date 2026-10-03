using System;
using System.Collections;
using Tikal.Interaction;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    public enum EchoNote { A,B,C }
    [RequireComponent(typeof(AudioSource))]
    public sealed class EchoStone : MonoBehaviour,IInteractable
    {
        [SerializeField] private EchoSequencePuzzle _puzzle;
        [SerializeField] private EchoNote _note;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _tone;
        [SerializeField] private Renderer _visual;
        private Coroutine _pulse;
        private MaterialPropertyBlock _block;
        public EchoSequencePuzzle Puzzle=>_puzzle;
        public EchoNote Note=>_note;
        public bool HasTone=>_audio!=null&&_tone!=null&&_visual!=null;
        public string InteractionText=>"Tocar";
        public bool CanInteract=>isActiveAndEnabled&&HasTone&&_pulse==null&&_puzzle!=null&&_puzzle.CanTouch(this);
        public event Action<EchoNote> NotePlayed;
        public void Interact()
        {
            if(!CanInteract)return;
            _audio.PlayOneShot(_tone);_pulse=StartCoroutine(Pulse());_puzzle.Receive(this);NotePlayed?.Invoke(_note);
        }
        private void Emission(Color value){if(_visual==null)return;_block??=new MaterialPropertyBlock();_block.SetColor("_EmissionColor",value);_visual.SetPropertyBlock(_block);}
        private IEnumerator Pulse(){Emission(new Color(.55f,.35f,.12f));yield return new WaitForSecondsRealtime(_tone.length);Emission(Color.black);_pulse=null;}
        internal void CancelTone(){if(_pulse!=null)StopCoroutine(_pulse);_pulse=null;if(_audio!=null)_audio.Stop();Emission(Color.black);}
        private void OnDisable()=>CancelTone();
    }
}
