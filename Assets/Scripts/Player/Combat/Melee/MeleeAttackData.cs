using System;
using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Melee Attack Data")]
    public class MeleeAttackData : ScriptableObject
    {
        [Header("基础信息")]
        public string attackName;
        
        [Header("动画数据")]
        public UnityEngine.Timeline.TimelineAsset timelineAsset;

        public float TotalDuration => timelineAsset != null ? (float)timelineAsset.duration : 0f;

        [Header("控制属性 (可被 Timeline 覆盖)")]
        public bool lockRotation = true;
        public float movementSpeedMultiplier = 0f;

        [Header("逻辑委托 (函数即数据)")]
        // 使用委托来定义该攻击是否可以从另一个攻击连招，或者定义连招窗口
        public Func<MeleeAttackData, bool> canComboTo;
        public Action<MeleeAttackData> onAttackPerform;
    }
}
