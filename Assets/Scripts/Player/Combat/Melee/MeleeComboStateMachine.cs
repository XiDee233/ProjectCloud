using UnityEngine;

namespace Player.Combat.Melee
{
    public class MeleeComboStateMachine : MonoBehaviour
    {
        public enum ComboStage
        {
            Idle,
            Startup,
            Active,
            Recovery
        }

        [SerializeField] private float startupDuration = 0.15f;
        [SerializeField] private float activeDuration = 0.2f;
        [SerializeField] private float recoveryDuration = 0.35f;

        public ComboStage CurrentStage { get; private set; } = ComboStage.Idle;
        public MeleeAttackData CurrentAttack { get; private set; }

        private float _stageTimer;

        public bool CanStartAttack() => CurrentStage == ComboStage.Idle;

        public void StartAttack(MeleeAttackData attackData)
        {
            CurrentAttack = attackData;
            CurrentStage = ComboStage.Startup;
            _stageTimer = startupDuration;
        }

        public void Tick(float delta)
        {
            if (CurrentStage == ComboStage.Idle)
                return;

            _stageTimer -= delta;
            if (_stageTimer > 0f)
                return;

            switch (CurrentStage)
            {
                case ComboStage.Startup:
                    TransitionToStage(ComboStage.Active, activeDuration);
                    break;
                case ComboStage.Active:
                    TransitionToStage(ComboStage.Recovery, recoveryDuration);
                    break;
                case ComboStage.Recovery:
                    TransitionToStage(ComboStage.Idle, 0f);
                    break;
            }
        }

        private void TransitionToStage(ComboStage nextStage, float duration)
        {
            CurrentStage = nextStage;
            _stageTimer = duration;

            if (nextStage == ComboStage.Idle)
                CurrentAttack = null;
        }
    }
}
