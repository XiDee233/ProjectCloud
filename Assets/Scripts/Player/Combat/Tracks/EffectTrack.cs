using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Player.Combat.Tracks
{
    [TrackColor(0.8f, 0.2f, 1f)]
    [TrackClipType(typeof(EffectClip))]
    [TrackBindingType(typeof(Transform))]
    public class EffectTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<EffectMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
