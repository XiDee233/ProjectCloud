using UnityEngine;
using System;

namespace Player.Combat.Ranged
{
    [AddComponentMenu("Player/Combat/Ranged Combat System")]
    public class RangedCombatSystem : MonoBehaviour
    {
        [SerializeField] private CombatPlayableGraph combatPlayableGraph;
        [SerializeField] private RangedWeaponData weaponData;

        public bool IsFiring { get; private set; }
        public bool IsCharging { get; private set; }
        public float ChargeTimer { get; private set; }
        
        private float _cooldownTimer;

        private void Awake()
        {
            if (!combatPlayableGraph) combatPlayableGraph = GetComponent<CombatPlayableGraph>();
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
            
            if (weaponData.chargeTimelineAsset != null)
                combatPlayableGraph.Play(weaponData.chargeTimelineAsset);
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

        public void StopCombat()
        {
            IsFiring = false;
            IsCharging = false;
            if (combatPlayableGraph != null)
            {
                combatPlayableGraph.Stop();
            }
        }

        private void Fire(float chargePercent)
        {
            IsFiring = true;
            _cooldownTimer = 1f / weaponData.fireRate;

            if (weaponData.fireTimelineAsset != null)
            {
                combatPlayableGraph.Play(weaponData.fireTimelineAsset, () => {
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

        public Vector3 GetDeltaPosition()
        {
            return combatPlayableGraph != null ? combatPlayableGraph.GetDeltaPosition() : Vector3.zero;
        }

        public Quaternion GetDeltaRotation()
        {
            return combatPlayableGraph != null ? combatPlayableGraph.GetDeltaRotation() : Quaternion.identity;
        }
    }
}
