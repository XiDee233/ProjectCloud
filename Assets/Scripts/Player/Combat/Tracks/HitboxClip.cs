using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class HitboxClip : PlayableAsset, ITimelineClipAsset
    {
        public HitboxBehaviour template = new HitboxBehaviour();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<HitboxBehaviour>.Create(graph, template);
        }
    }
}
