using UnityEngine;
using Player.Core;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using Player.Combat.Melee;
using System.Linq;

namespace Player.States
{
    public class MeleeStateNode : PredictedStateNode<MeleeStateNode.MeleeInput, MeleeStateNode.MeleeData>
    {
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private MeleeCombatSystem meleeCombatSystem;
        [SerializeField] private ControlAuthority controlAuthority;

        public void Initialize(PlayerMovementCore core, MeleeCombatSystem combatSystem, ControlAuthority authority)
        {
            movementCore = core;
            meleeCombatSystem = combatSystem;
            controlAuthority = authority;
        }

        public override void Enter()
        {
            base.Enter();
            if (movementCore != null)
            {
                var data = movementCore.GetPersistentState();
                movementCore.SetMovementLocked(ref data, true);
                movementCore.SetRotationLocked(ref data, true);
                currentState = new MeleeData { movementData = data, isInitialized = false };
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (movementCore != null) movementCore.UpdatePersistentState(currentState.movementData);
        }

        // 空占位符方法，绕过 PurrNet 引擎的自动调用
        protected override void Simulate(MeleeInput input, ref MeleeData state, float delta) { }

        protected override void StateSimulate(in MeleeInput input, ref MeleeData state, float delta)
        {
            if (movementCore == null) return;

            // 如果不是拥有者，且不是服务器，我们不应该运行模拟逻辑
            if (!machine.isOwner && !machine.isServer) return;

           
        }

        protected override void GetFinalInput(ref MeleeInput input)
        {
            var p = controlAuthority != null ? controlAuthority.CurrentProvider : null;
            if (p == null) { input.Reset(); return; }

            input.primaryAttack = p.PrimaryAttack;
            if (input.primaryAttack.wasPressed) p.ConsumeInput(InputActionType.PrimaryAttack);

            input.aimDirection = p.AimWorldDirection;
        }

        public struct MeleeInput : IPredictedData
        {
            public InputButtonState primaryAttack;
            public Vector3 aimDirection;
            public void Reset() { primaryAttack = InputButtonState.None; aimDirection = Vector3.zero; }
            public void Dispose() { }
        }

        public struct MeleeData : IPredictedData<MeleeData>
        {
            public MovementCoreData movementData;
            public MeleeInput inputSnapshot;
            public bool isInitialized;
            public bool attackRequested;
            public void Dispose() { }
        }
    }
}
