using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.UI
{
    // Presentation adapter: the original dropdown remains the only source of values and save events.
    [DisallowMultipleComponent]
    public sealed class MenuSegmentedChoice : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _source;
        [SerializeField] private Button[] _segments = Array.Empty<Button>();
        private int _last = -1;
        private UnityEngine.Events.UnityAction[] _listeners;
        public TMP_Dropdown Source => _source;
        public Button[] Segments => _segments;
        private void OnEnable()
        {
            _listeners=new UnityEngine.Events.UnityAction[_segments.Length];
            for (int i=0;i<_segments.Length;i++)
            { int index=i; _listeners[i]=()=>Choose(index); _segments[i].onClick.AddListener(_listeners[i]); }
            _source.onValueChanged.AddListener(Changed);
            RefreshVisual();
        }
        private void OnDisable()
        {
            if(_listeners!=null)for(int i=0;i<_segments.Length;i++)
                if(_segments[i]!=null)_segments[i].onClick.RemoveListener(_listeners[i]);
            if(_source!=null)_source.onValueChanged.RemoveListener(Changed);
        }
        private void Choose(int value) { _source.value=value; RefreshVisual(); }
        private void Changed(int _) => RefreshVisual();
        private void LateUpdate(){if(_last!=_source.value)RefreshVisual();}
        public void RefreshVisual()
        {
            if(_source==null)return;
            _last=_source.value;
            for(int i=0;i<_segments.Length;i++)
                if(_segments[i]!=null)_segments[i].GetComponent<MenuSelectableVisual>()?.SetPersistent(i==_last);
        }
    }
}
