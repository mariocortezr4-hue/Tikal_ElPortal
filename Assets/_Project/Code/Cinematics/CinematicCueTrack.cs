using UnityEngine.Timeline;

namespace Tikal.Cinematics
{
    [TrackClipType(typeof(CinematicCueAsset))]
    [TrackBindingType(typeof(IntroSequenceController))]
    public sealed class CinematicCueTrack : TrackAsset
    {
    }
}
