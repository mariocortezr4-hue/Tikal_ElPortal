using UnityEngine;
namespace Tikal.Puzzles.Level01
{
    [DisallowMultipleComponent]
    public sealed class GuardiansAlignmentFeedback : MonoBehaviour
    {
        [SerializeField] private PathGuardiansPuzzle _puzzle;
        [SerializeField] private Renderer[] _individualMarks = new Renderer[3];
        [SerializeField] private Renderer[] _centralMarks = new Renderer[3];
        [SerializeField, ColorUsage(false,true)] private Color _alignedEmission = new Color(.8f,.48f,.12f);
        private MaterialPropertyBlock _block;
        private bool _subscribed;
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private void OnEnable()
        {
            if(_puzzle==null||!Valid(_individualMarks)||!Valid(_centralMarks))
            {Debug.LogError("Guardian alignment feedback requires six marks and a puzzle.",this);return;}
            _block=new MaterialPropertyBlock();
            _puzzle.AlignmentChanged+=Refresh;_subscribed=true;Refresh(_puzzle);
        }
        private static bool Valid(Renderer[] marks)
        {return marks!=null&&marks.Length==3&&marks[0]!=null&&marks[1]!=null&&marks[2]!=null;}
        private void Start() { if(_subscribed)Refresh(_puzzle); }
        private void OnDisable()
        {if(_subscribed&&_puzzle!=null)_puzzle.AlignmentChanged-=Refresh;_subscribed=false;}
        private void Refresh(PathGuardiansPuzzle _)
        {
            for(int i=0;i<3;i++)
            {
                _block.SetColor(Emission,_puzzle.IsGuardianAligned(i)?_alignedEmission:Color.black);
                _individualMarks[i].SetPropertyBlock(_block);_centralMarks[i].SetPropertyBlock(_block);
            }
        }
    }
}
