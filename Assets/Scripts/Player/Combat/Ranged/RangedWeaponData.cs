using UnityEngine;
using System;

namespace Player.Combat.Ranged
{
    [CreateAssetMenu(menuName = "Player/Ranged Weapon Data")]
    public class RangedWeaponData : ScriptableObject
    {
        [Header("动画数据")]
        public UnityEngine.Timeline.TimelineAsset fireTimelineAsset;
        public UnityEngine.Timeline.TimelineAsset chargeTimelineAsset;

        [Header("基础数值")]
        public float fireRate = 5f;
        public float baseDamage = 12f;
        public float movementSpeedMultiplier = 0.8f;

        [Header("蓄力属性 (用于弓箭类)")]
        public float maxChargeTime = 1.5f;
        public AnimationCurve chargeDamageCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        public AnimationCurve chargeRangeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);

        [Header("枪械属性")]
        public float recoilAmount = 3f;
        public float recoilRecoveryTime = 0.4f;

        [Header("逻辑委托 (函数即数据)")]
        // 替代 enum WeaponType，通过行为委托定义
        public Func<RangedWeaponData, bool> needsCharging;
        public Action<RangedWeaponData, float> onFire;
        public Func<RangedWeaponData, float, float> calculateDamage;

        public void Fire(float chargePercent = 0f)
        {
            onFire?.Invoke(this, chargePercent);
        }

        public float GetDamage(float chargePercent = 0f)
        {
            if (calculateDamage != null) return calculateDamage(this, chargePercent);
            return baseDamage * chargeDamageCurve.Evaluate(chargePercent);
        }
    }
}
