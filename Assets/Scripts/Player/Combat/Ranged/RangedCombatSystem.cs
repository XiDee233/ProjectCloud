using UnityEngine;

namespace Player.Combat.Ranged
{
    [AddComponentMenu("Player/Combat/Ranged Combat System")]
    public class RangedCombatSystem : MonoBehaviour
    {
        [SerializeField] private RangedWeaponData weaponData;
        [SerializeField] private float fireCooldown = 0.2f;

        public float chargeTimer { get; private set; }
        public bool isCharging { get; private set; }
        private float _cooldownTimer;

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            if (isCharging)
                chargeTimer += Time.deltaTime;
        }

        public bool CanFire => _cooldownTimer <= 0f;

        public void StartCharging()
        {
            if (weaponData == null || weaponData.weaponType != RangedWeaponData.WeaponType.Bow)
                return;

            isCharging = true;
            chargeTimer = 0f;
        }

        public void Release()
        {
            if (!isCharging)
                return;

            isCharging = false;
            _cooldownTimer = fireCooldown;
        }

        public bool TryFire()
        {
            if (weaponData == null || !CanFire)
                return false;

            _cooldownTimer = fireCooldown;
            chargeTimer = 0f;
            isCharging = false;
            return true;
        }

        public RangedWeaponData WeaponData => weaponData;
    }
}
