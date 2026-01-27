using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;

namespace Player.Combat.Tracks
{
    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackClipType(typeof(CombatEventClip))]
    public class CombatEventTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CombatEventMixerBehaviour>.Create(graph, inputCount);
        }
    }

    [Serializable]
    public class CombatEventClip : PlayableAsset, ITimelineClipAsset
    {
        public CombatEventBehaviour template = new CombatEventBehaviour();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CombatEventBehaviour>.Create(graph, template);
            return playable;
        }
    }

    [Serializable]
    public class CombatEventBehaviour : PlayableBehaviour
    {
        public string eventName;
        public float floatParam;
        public string stringParam;
        
        private bool _hasTriggered = false;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _hasTriggered = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!_hasTriggered && info.weight > 0f)
            {
                _hasTriggered = true;
                // 这里可以分发事件
                if (playerData is GameObject go)
                {
                    // 分发到战斗系统
                    var listeners = go.GetComponentsInChildren<ICombatEventListener>();
                    foreach (var l in listeners) l.OnCombatEvent(eventName, floatParam, stringParam);
                }
            }
        }
    }

    public class CombatEventMixerBehaviour : PlayableBehaviour { }
}
