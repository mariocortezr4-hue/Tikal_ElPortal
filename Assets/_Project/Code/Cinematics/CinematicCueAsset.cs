using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Cinematics
{
    public enum CinematicCueType
    {
        Date,
        Narrative,
        Subtitle,
        Title,
        AncientMapReceived
    }

    [Serializable]
    public sealed class CinematicCueBehaviour : PlayableBehaviour
    {
        public string CueId;
        public CinematicCueType Type;
        public string Speaker;
        public string Text;
        private IntroSequenceController _sequence;
        private bool _visible;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _sequence = playerData as IntroSequenceController;
            if (_sequence == null || info.effectiveWeight <= 0f) return;
            _visible = true;
            _sequence.ApplyCue(CueId, Type, Speaker, Text);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_visible || _sequence == null) return;
            _visible = false;
            _sequence.ReleaseCue(CueId);
        }
    }

    public sealed class CinematicCueAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private string _cueId;
        [SerializeField] private CinematicCueType _type;
        [SerializeField] private string _speaker;
        [SerializeField, TextArea] private string _text;

        public string CueId => _cueId;
        public CinematicCueType Type => _type;
        public string Speaker => _speaker;
        public string Text => _text;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CinematicCueBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.CueId = _cueId;
            behaviour.Type = _type;
            behaviour.Speaker = _speaker;
            behaviour.Text = _text;
            return playable;
        }
    }
}
