using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class ComboWindowClip : PlayableAsset, ITimelineClipAsset
    {
        public ComboWindowBehaviour template = new ComboWindowBehaviour();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<ComboWindowBehaviour>.Create(graph, template);
        }
    }
}
