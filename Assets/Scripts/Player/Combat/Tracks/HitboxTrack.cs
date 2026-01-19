using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Player.Combat.Tracks
{
    [TrackColor(1f, 0.2f, 0.2f)]
    [TrackClipType(typeof(HitboxClip))]
    [TrackBindingType(typeof(Transform))]
    public class HitboxTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<HitboxMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
