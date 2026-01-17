using UnityEngine;

namespace Player.Combat.Melee
{
    [AddComponentMenu("Player/Combat/Melee Combat System")]
    public class MeleeCombatSystem : MonoBehaviour
    {
        [SerializeField] private MeleeComboStateMachine comboStateMachine;
        [SerializeField] private MeleeAttackData defaultAttack;

        public MeleeComboStateMachine ComboStateMachine
        {
            get => comboStateMachine;
            set => comboStateMachine = value;
        }

        private void Update()
        {
            if (comboStateMachine == null)
                return;
            comboStateMachine.Tick(Time.deltaTime);
        }

        public bool TryAttack()
        {
            if (comboStateMachine == null || defaultAttack == null)
                return false;

            if (!comboStateMachine.CanStartAttack())
                return false;

            comboStateMachine.StartAttack(defaultAttack);
            return true;
        }
    }
}
