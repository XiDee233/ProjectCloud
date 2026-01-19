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
        public CombatTimelineData animationData;

        [Header("控制属性 (可被 Timeline 覆盖)")]
        public bool lockRotation = true;
        public float movementSpeedMultiplier = 0f;

        [Header("基础伤害属性")]
        public int damage = 10;
        public float knockback = 5f;

        [Header("判定 (建议通过 Timeline 事件定义)")]
        public Vector3 hitboxOffset;
        public float hitboxRadius = 0.5f;

        [Header("逻辑委托 (函数即数据)")]
        // 使用委托来定义该攻击是否可以从另一个攻击连招，或者定义连招窗口
        public Func<MeleeAttackData, bool> canComboTo;
        public Action<MeleeAttackData> onAttackPerform;
    }
}
