using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class EffectClip : PlayableAsset, ITimelineClipAsset
    {
        public EffectBehaviour template = new EffectBehaviour();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<EffectBehaviour>.Create(graph, template);
        }
    }
}
