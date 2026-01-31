using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Combat/Combo/Node")]
    public class ComboNode : ScriptableObject
    {
        [Header("基础信息")]
        public string attackName;

        [Header("动画数据")]
        public UnityEngine.Timeline.TimelineAsset timelineAsset;

        public float TotalDuration => timelineAsset != null ? (float)timelineAsset.duration : 0f;

        [Header("控制属性 (可被 Timeline 覆盖)")]
        public bool lockRotation = true;
        public float movementSpeedMultiplier = 0f;

        [Header("在移动状态可作为起手")]
        public bool isEntry = true;

        [Header("起手输入定义（仅用于 TryResolveEntry 匹配）")]
        public ComboInputSequence entryInput;

        [Header("派生")]
        public List<ComboTransition> transitions = new List<ComboTransition>();

        [Min(0)]
        public int entryPriority = 0;
    }
}
