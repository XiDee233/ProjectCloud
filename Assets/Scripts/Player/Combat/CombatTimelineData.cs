using UnityEngine;
using UnityEngine.Timeline;

namespace Player.Combat
{
    [CreateAssetMenu(fileName = "CombatTimelineData", menuName = "Player/Combat/Combat Timeline Data")]
    public class CombatTimelineData : ScriptableObject
    {
        [SerializeField] private TimelineAsset timelineAsset;
        [SerializeField] private float totalDuration;
        
        public TimelineAsset TimelineAsset => timelineAsset;
        public float TotalDuration => totalDuration > 0 ? totalDuration : (timelineAsset ? (float)timelineAsset.duration : 0f);

        #if UNITY_EDITOR
        public void SetTimeline(TimelineAsset asset)
        {
            timelineAsset = asset;
            totalDuration = (float)asset.duration;
        }
        #endif
    }
}
