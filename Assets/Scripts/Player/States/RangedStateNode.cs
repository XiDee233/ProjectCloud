using UnityEngine;
using Player.Core;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using Player.Combat.Ranged;
using System.Linq;

namespace Player.States
{
    public class RangedStateNode : PredictedStateNode<RangedStateNode.RangedInput, RangedStateNode.RangedData>
    {
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private RangedCombatSystem rangedCombatSystem;
        [SerializeField] private ControlAuthority controlAuthority;

        public void Initialize(PlayerMovementCore core, RangedCombatSystem combatSystem, ControlAuthority authority)
        {
            movementCore = core;
            rangedCombatSystem = combatSystem;
            controlAuthority = authority;
        }

        public override void Enter()
        {
            base.Enter();
            if (movementCore != null)
            {
                var data = movementCore.GetPersistentState();
                movementCore.SetMovementLocked(ref data, false);
                movementCore.SetRotationLocked(ref data, false);
                currentState = new RangedData { movementData = data, isInitialized = false, fired = false };
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (movementCore != null) movementCore.UpdatePersistentState(currentState.movementData);
        }

        // 空占位符方法，绕过 PurrNet 引擎的自动调用
        protected override void Simulate(RangedInput input, ref RangedData state, float delta) { }

        protected override void StateSimulate(in RangedInput input, ref RangedData state, float delta)
        {
            if (movementCore == null) return;

            // 如果不是拥有者，且不是服务器，我们不应该运行模拟逻辑
            if (!machine.isOwner && !machine.isServer) return;

        
        }

        protected override void GetFinalInput(ref RangedInput input)
        {
            var p = controlAuthority != null ? controlAuthority.CurrentProvider : null;
            if (p == null) { input.Reset(); return; }
            
            input.primaryAttack = p.PrimaryAttack;
            if (input.primaryAttack.wasPressed) p.ConsumeInput(InputActionType.PrimaryAttack);

            input.secondaryAttack = p.SecondaryAttack;
            if (input.secondaryAttack.wasPressed) p.ConsumeInput(InputActionType.SecondaryAttack);

            input.aimDirection = p.AimWorldDirection;
        }

        private void TryReturnToMovement()
        {
            if (machine != null)
            {
                var s = machine.states.FirstOrDefault(x => x is MovementStateNode);
                if (s != null) machine.SetState(s);
            }
        }

        public struct RangedInput : IPredictedData
        {
            public InputButtonState primaryAttack;
            public InputButtonState secondaryAttack;
            public Vector3 aimDirection;
            public void Reset() { primaryAttack = secondaryAttack = InputButtonState.None; aimDirection = Vector3.zero; }
            public void Dispose() { }
        }

        public struct RangedData : IPredictedData<RangedData>
        {
            public MovementCoreData movementData;
            public RangedInput inputSnapshot;
            public bool isInitialized;
            public bool fired;
            public void Dispose() { }
        }
    }
}
