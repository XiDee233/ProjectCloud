using System;
using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Melee Attack Data")]
    public class MeleeAttackData : ScriptableObject
    {
        [Header("基础信息")]
        public string attackName;
        [Tooltip("启动帧数量")]
        public float startupFrames = 4f;
        [Tooltip("判定帧数量")]
        public float activeFrames = 8f;
        [Tooltip("恢复帧数量")]
        public float recoveryFrames = 12f;

        [Header("控制")]
        public float cancelWindowStart = 0.5f;
        public float cancelWindowEnd = 1f;
        public bool lockRotation = true;
        public float movementSpeedMultiplier = 0f;

        [Header("判定")]
        public Vector3 hitboxOffset;
        public float hitboxRadius = 0.5f;
        public int damage = 10;
        public float knockback = 5f;

        [Header("连招")]
        public MeleeAttackData[] nextPossibleAttacks;

        [NonSerialized] public Func<MeleeAttackData, bool> CanComboFrom;
    }
}
