using UnityEngine;
using UnityEngine.Playables;

namespace Tikal.Experiments
{
    // Experimental derived-mesh blendshape only: never writes bones, Avatar, source mesh or audio.
    [DisallowMultipleComponent, RequireComponent(typeof(SkinnedMeshRenderer)), DefaultExecutionOrder(1000)]
    public sealed class SimpleVoiceMouthFlap : MonoBehaviour
    {
        [SerializeField] private AudioSource _voice;
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private string _shape="Experimental_LocalMouthOpen";
        [SerializeField, Range(0,3)] private float _maximumDisplacementMm=3;
        [SerializeField, Min(.0001f)] private float _silenceThreshold=.0015f;
        [SerializeField, Min(.001f)] private float _fullOpenRms=.06f;
        [SerializeField, Min(.01f)] private float _attackSeconds=.045f;
        [SerializeField, Min(.01f)] private float _releaseSeconds=.14f;
        private readonly float[] _samples=new float[1024];
        private SkinnedMeshRenderer _renderer;
        private int _index=-1;
        private float _neutral,_opening;
        public float Opening => _opening;
        public float LastRms { get; private set; }
        public float MaximumDisplacementMm => _maximumDisplacementMm;
        public AudioSource Voice => _voice;
        public bool HasTarget => _renderer!=null&&_index>=0;

        private void Awake()
        {
            _renderer=GetComponent<SkinnedMeshRenderer>();
            if(_renderer.sharedMesh!=null)_index=_renderer.sharedMesh.GetBlendShapeIndex(_shape);
            if(_index>=0)_neutral=_renderer.GetBlendShapeWeight(_index);
        }
        private void OnEnable()
        {
            if(_director!=null){_director.stopped+=OnStop;_director.paused+=OnStop;}
        }
        private void OnDisable()
        {
            if(_director!=null){_director.stopped-=OnStop;_director.paused-=OnStop;}
            ResetToNeutral();
        }
        private void OnDestroy()=>ResetToNeutral();
        private void OnApplicationPause(bool paused){if(paused)ResetToNeutral();}
        private void OnStop(PlayableDirector director)=>ResetToNeutral();
        public void ResetToNeutral()
        {
            _opening=0;LastRms=0;
            if(HasTarget)_renderer.SetBlendShapeWeight(_index,_neutral);
        }
        private void LateUpdate()
        {
            if(!HasTarget)return;
            if(_director==null||_director.state!=PlayState.Playing||Time.timeScale<=0){ResetToNeutral();return;}
            float rms=0;
            if(_voice!=null&&_voice.isActiveAndEnabled&&_voice.isPlaying)
            {
                _voice.GetOutputData(_samples,0);
                for(int i=0;i<_samples.Length;i++)rms+=_samples[i]*_samples[i];
                rms=Mathf.Sqrt(rms/_samples.Length);
            }
            LastRms=rms;
            float target=Mathf.Clamp01((rms-_silenceThreshold)/Mathf.Max(.001f,_fullOpenRms-_silenceThreshold));
            float tau=target>_opening?_attackSeconds:_releaseSeconds;
            _opening=Mathf.Lerp(_opening,target,1-Mathf.Exp(-Time.unscaledDeltaTime/tau));
            if(target==0&&_opening<.001f)_opening=0;
            _renderer.SetBlendShapeWeight(_index,_neutral+100*_opening*Mathf.Clamp01(_maximumDisplacementMm/3));
        }
    }
}
