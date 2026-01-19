using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

using Player.Combat;

namespace Player.Combat.Tracks
{
    [TrackColor(0.2f, 1f, 0.5f)]
    [TrackClipType(typeof(ComboWindowClip))]
    [TrackBindingType(typeof(IComboWindowStateProvider))]
    public class ComboWindowTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<ComboWindowMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
