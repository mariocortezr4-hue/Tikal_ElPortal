using Tikal.Input;
using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class SecretEntranceCommandReceiver : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputReader;
        [SerializeField] private Transform _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _target;
        [SerializeField] private BoxCollider _zone;
        [SerializeField] private Collider[] _movingLeaf;
        [SerializeField] private LevelOutcomeController _outcome;
        [SerializeField,Min(.1f)] private float _range = 3;
        [SerializeField,Range(-1,1)] private float _minimumDot = .7f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private IPlayerInputReader _input;
        private bool _subscribed;
        public float Range => _range;
        public float MinimumDot => _minimumDot;
        private void Awake() => _input = _inputReader as IPlayerInputReader;
        private void OnEnable()
        {
            if(_input==null || _player==null || _camera==null || _target==null || _zone==null || _outcome==null)
            { Debug.LogError("Secret entrance requires explicit input, player, camera, target, zone and outcome references.",this); return; }
            _input.SecretCommandPressed += Receive;
            _subscribed=true;
        }
        private void OnDisable(){if(_subscribed)_input.SecretCommandPressed-=Receive;_subscribed=false;}
        private void Receive()
        {
            if(_outcome.State!=LevelOutcome.Playing || !_input.GameplayEnabled ||
                !_zone.enabled || !_zone.bounds.Contains(_player.position+Vector3.up))return;
            Vector3 delta=_target.position-_camera.transform.position;
            float distance=delta.magnitude;
            if(distance>.001f && distance<=_range && Vector3.Dot(_camera.transform.forward,delta/distance)>=_minimumDot)
            {
                int count=Physics.RaycastNonAlloc(_camera.transform.position,delta/distance,_hits,distance-.025f,~0,QueryTriggerInteraction.Ignore);
                if(count==_hits.Length)return;
                for(int i=0;i<count;i++)
                {
                    var hit=_hits[i].collider;
                    if(hit.transform.IsChildOf(_player))continue;
                    bool leaf=false;
                    if(_movingLeaf!=null)for(int j=0;j<_movingLeaf.Length;j++)if(hit==_movingLeaf[j]){leaf=true;break;}
                    if(!leaf)return;
                }
                _outcome.TriggerVictory();
            }
        }
    }
}