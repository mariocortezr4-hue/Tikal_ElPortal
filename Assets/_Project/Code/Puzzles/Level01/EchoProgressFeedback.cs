using System.Collections;
using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    public sealed class EchoProgressFeedback : MonoBehaviour
    {
        [SerializeField] private EchoSequencePuzzle _puzzle;
        [SerializeField] private Renderer[] _marks=new Renderer[3];
        private MaterialPropertyBlock _block;
        private Coroutine _error;
        private bool _subscribed;
        private void OnEnable()
        {
            if(_puzzle==null||_marks==null||_marks.Length!=3||_marks[0]==null||_marks[1]==null||_marks[2]==null){Debug.LogError("Echo feedback requires three marks and a puzzle.",this);return;}
            _block=new MaterialPropertyBlock();_puzzle.ProgressChanged+=Refresh;_puzzle.WrongNote+=Error;_subscribed=true;Refresh(_puzzle);
        }
        private void Start(){if(_subscribed)Refresh(_puzzle);}
        private void Refresh(EchoSequencePuzzle _)
        {
            if(_error!=null)StopCoroutine(_error);_error=null;
            for(int i=0;i<3;i++)Set(i,i<_puzzle.Progress?new Color(.8f,.48f,.12f):Color.black);
        }
        private void Set(int i,Color color){_block.SetColor("_EmissionColor",color);_marks[i].SetPropertyBlock(_block);}
        private void Error(EchoSequencePuzzle _){if(_error!=null)StopCoroutine(_error);_error=StartCoroutine(ErrorPulse());}
        private IEnumerator ErrorPulse(){for(int i=0;i<3;i++)Set(i,new Color(.15f,.045f,.01f));yield return new WaitForSecondsRealtime(.2f);_error=null;Refresh(_puzzle);}
        private void OnDisable(){if(_subscribed&&_puzzle!=null){_puzzle.ProgressChanged-=Refresh;_puzzle.WrongNote-=Error;}_subscribed=false;if(_error!=null)StopCoroutine(_error);_error=null;}
    }
}
