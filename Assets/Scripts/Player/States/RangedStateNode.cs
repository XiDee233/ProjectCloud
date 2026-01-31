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

        private void Awake()
        {
            if (movementCore == null) movementCore = GetComponentInParent<PlayerMovementCore>();
            if (rangedCombatSystem == null) rangedCombatSystem = GetComponentInParent<RangedCombatSystem>();
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();
        }

        public override void Enter()
        {
            base.Enter();
            if (movementCore != null)
            {
                var data = movementCore.CreateDefaultMovementData();
                movementCore.SetMovementLocked(ref data, false);
                movementCore.SetRotationLocked(ref data, false);
                currentState = new RangedData { movementData = data, isInitialized = false, fired = false };
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (rangedCombatSystem != null) rangedCombatSystem.StopCombat();
        }

        protected override void Simulate(RangedInput input, ref RangedData state, float delta) { }

        protected override void StateSimulate(in RangedInput input, ref RangedData state, float delta)
        {
            if (movementCore == null || rangedCombatSystem == null) return;
            if (!machine.isOwner && !machine.isServer) return;

            // 处理移动
            Vector3 moveDir = movementCore.CalculateCameraRelativeMovement(input.movement);
            movementCore.ApplyAcceleratedMovement(ref state.movementData, moveDir, delta);

            // 处理旋转
            if (input.aimDirection.sqrMagnitude > 0.01f)
            {
                float angle = movementCore.CalculateAimAngle(input.aimDirection);
                movementCore.ApplyRotation(ref state.movementData, angle, delta);
            }

            // 处理射击逻辑
            if (input.primaryAttack.isPressed)
            {
                rangedCombatSystem.StartCharging();
            }
            else if (input.primaryAttack.wasReleased)
            {
                rangedCombatSystem.Release();
                state.fired = true;
            }

            // 应用 Root Motion
            Vector3 rootDelta = rangedCombatSystem.GetDeltaPosition();
            if (rootDelta.sqrMagnitude > 0)
            {
                movementCore.ApplyRawMovement(rootDelta);
            }

            // 如果射击完成且没有继续按键，返回移动状态
            if (state.fired && !rangedCombatSystem.IsFiring && !input.primaryAttack.isPressed)
            {
                ReturnToMovement();
            }

            movementCore.FinalizeMovement(ref state.movementData);
        }

        private void ReturnToMovement()
        {
            if (machine != null)
            {
                var s = machine.states.FirstOrDefault(x => x is MovementStateNode);
                if (s != null) machine.SetState(s);
            }
        }

        [SerializeField] private PredictedPlayerInputCollector inputCollector;

        protected override void GetFinalInput(ref RangedInput input)
        {
            if (inputCollector == null) inputCollector = GetComponentInParent<PredictedPlayerInputCollector>();
            if (inputCollector == null) { input.Reset(); return; }

            //var c = inputCollector.Current;
            //input.movement = c.movement;
            //input.primaryAttack = c.primaryAttack;
            //input.secondaryAttack = c.secondaryAttack;
            //input.aimDirection = c.aimWorldDirection;
        }

        public struct RangedInput : IPredictedData
        {
            public Vector2 movement;
            public InputButtonState primaryAttack;
            public InputButtonState secondaryAttack;
            public Vector3 aimDirection;
            public void Reset() { movement = Vector2.zero; primaryAttack = secondaryAttack = InputButtonState.None; aimDirection = Vector3.zero; }
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
