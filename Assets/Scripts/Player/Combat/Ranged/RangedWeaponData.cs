using UnityEngine;

namespace Player.Combat.Ranged
{
    [CreateAssetMenu(menuName = "Player/Ranged Weapon Data")]
    public class RangedWeaponData : ScriptableObject
    {
        public enum WeaponType
        {
            Bow,
            Gun
        }

        [Header("类型")]
        public WeaponType weaponType;

        [Header("弓箭")]
        public float maxChargeTime = 1.5f;
        public AnimationCurve chargeDamageCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        public AnimationCurve chargeRangeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);

        [Header("枪械")]
        public float fireRate = 5f;
        public float recoilAmount = 3f;
        public float recoilRecoveryTime = 0.4f;

        [Header("通用")]
        public float movementSpeedMultiplier = 0.8f;
        public float baseDamage = 12f;
    }
}
