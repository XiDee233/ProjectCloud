using UnityEngine;
using System;

namespace Player.Combat.Ranged
{
    [AddComponentMenu("Player/Combat/Ranged Combat System")]
    public class RangedCombatSystem : MonoBehaviour
    {
        [SerializeField] private CombatTimelinePlayer timelinePlayer;
        [SerializeField] private RangedWeaponData weaponData;

        public bool IsFiring { get; private set; }
        public bool IsCharging { get; private set; }
        public float ChargeTimer { get; private set; }
        
        private float _cooldownTimer;

        private void Awake()
        {
            if (!timelinePlayer) timelinePlayer = GetComponent<CombatTimelinePlayer>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            if (IsCharging)
                ChargeTimer += Time.deltaTime;
        }

        public bool CanFire => _cooldownTimer <= 0f && !IsFiring;

        public void StartCharging()
        {
            if (weaponData == null || IsCharging) return;
            
            bool needsCharge = weaponData.needsCharging != null ? weaponData.needsCharging(weaponData) : false;
            if (!needsCharge) return;

            IsCharging = true;
            ChargeTimer = 0f;
            
            if (weaponData.chargeAnimationData != null)
                timelinePlayer.Play(weaponData.chargeAnimationData);
        }

        public void Release()
        {
            if (!IsCharging) return;

            float chargePercent = Mathf.Clamp01(ChargeTimer / weaponData.maxChargeTime);
            IsCharging = false;
            
            Fire(chargePercent);
        }

        public bool TryFire()
        {
            if (!CanFire) return false;
            
            Fire(0f);
            return true;
        }

        private void Fire(float chargePercent)
        {
            IsFiring = true;
            _cooldownTimer = 1f / weaponData.fireRate;

            if (weaponData.fireAnimationData != null)
            {
                timelinePlayer.Play(weaponData.fireAnimationData, () => {
                    IsFiring = false;
                });
            }
            else
            {
                IsFiring = false;
            }

            weaponData.Fire(chargePercent);
        }

        public RangedWeaponData WeaponData => weaponData;
    }
}
