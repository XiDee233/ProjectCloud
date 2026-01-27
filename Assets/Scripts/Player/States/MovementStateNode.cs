using UnityEngine;
using Player.Core;
using Player.Animation;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Linq;

namespace Player.States
{
    public class MovementStateNode : PredictedStateNode<MovementStateNode.MovementInput, MovementStateNode.MovementData>
    {
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private ControlAuthority controlAuthority;
        [SerializeField] private MovementAnimationController animationController;
        
        private void Awake()
        {
            if (movementCore == null) movementCore = GetComponentInParent<PlayerMovementCore>();
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();
            if (animationController == null) animationController = GetComponentInParent<MovementAnimationController>();
        }

        /// <summary>
        /// 更新视图层动画（在远程客户端上应用动画参数）
        /// </summary>
        /// <param name="viewState">插值后的状态数据</param>
        /// <param name="verifiedState">已验证的状态数据</param>
        protected override void UpdateView(MovementData viewState, MovementData? verifiedState)
        {
            base.UpdateView(viewState, verifiedState);

            // 应用动画参数到远程客户端的 Animator
            if (animationController != null)
            {
                animationController.ApplyAnimationParameters(
                    viewState.animMoveX,
                    viewState.animMoveY,
                    viewState.animSpeed
                );
            }
        }

        public override void Enter()
        {
            base.Enter();
            if (movementCore != null)
            {
                var data = movementCore.CreateDefaultMovementData();
                movementCore.ClearRotationOverride(ref data);
                movementCore.SetMovementLocked(ref data, false);
                movementCore.SetRotationLocked(ref data, false);
                movementCore.SetDashVelocity(ref data, Vector3.zero);
                currentState = new MovementData { movementData = data };
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        // 空占位符方法，绕过 PurrNet 引擎的自动调用
        protected override void Simulate(MovementInput input, ref MovementData state, float delta) { }

        protected override void StateSimulate(in MovementInput input, ref MovementData state, float delta)
        {
            if (movementCore == null) return;

            // 如果不是拥有者，且不是服务器，我们不应该运行模拟逻辑
            if (!machine.isOwner && !machine.isServer) return;

            movementCore.UpdateGroundedState(ref state.movementData);

            if (machine != null)
            {
                if (input.dash.wasPressed) 
                {
                    var s = machine.states.FirstOrDefault(x => x is DashStateNode); if (s != null) 
                    { 
                        machine.SetState(s); 
                        return; 
                    } 
                }
                if (input.primaryAttack.wasPressed) 
                { 
                    var s = machine.states.FirstOrDefault(x => x is MeleeStateNode); if (s != null) 
                    { 
                        machine.SetState(s); 
                        return;
                    } 
                }
                if (input.secondaryAttack.wasPressed) 
                { 
                    var s = machine.states.FirstOrDefault(x => x is RangedStateNode); if (s != null) 
                    { 
                        machine.SetState(s); return; 
                    } 
                }
            }

            Vector3 moveDir = movementCore.CalculateCameraRelativeMovement(input.movement);
            movementCore.ApplyAcceleratedMovement(ref state.movementData, moveDir, delta);

            if (input.aimDirection.sqrMagnitude > 0.01f)
            {
                float angle = movementCore.CalculateAimAngle(input.aimDirection);
                movementCore.ApplyRotation(ref state.movementData, angle, delta);
            }

            movementCore.ApplyGravity(ref state.movementData, delta);
            movementCore.FinalizeMovement(ref state.movementData);
            state.lastInput = input;

            // 计算并存储动画参数（用于同步到远程客户端）
            if (animationController != null)
            {
                var (moveX, moveY, speed) = animationController.CalculateAnimationParameters(state.movementData, movementCore.MaxMoveSpeed);
                state.animMoveX = moveX;
                state.animMoveY = moveY;
                state.animSpeed = speed;

                // 本地客户端直接应用动画（拥有者）
                if (machine.isOwner)
                    animationController.ApplyAnimationParameters(moveX, moveY, speed);
            }
        }

        protected override void GetFinalInput(ref MovementInput input)
        {
            var p = controlAuthority != null ? controlAuthority.CurrentProvider : null;
            if (p == null) { input.Reset(); return; }
            input.movement = p.Movement;
            
            input.dash = p.Dash;
            if (input.dash.wasPressed) p.ConsumeInput(InputActionType.Dash);

            input.primaryAttack = p.PrimaryAttack;
            if (input.primaryAttack.wasPressed) p.ConsumeInput(InputActionType.PrimaryAttack);

            input.secondaryAttack = p.SecondaryAttack;
            if (input.secondaryAttack.wasPressed) p.ConsumeInput(InputActionType.SecondaryAttack);

            input.aimDirection = p.AimWorldDirection;
        }

        public struct MovementInput : IPredictedData
        {
            public Vector2 movement;
            public Vector3 aimDirection;
            public InputButtonState dash;
            public InputButtonState primaryAttack;
            public InputButtonState secondaryAttack;
            public void Reset() { movement = Vector2.zero; aimDirection = Vector3.zero; dash = primaryAttack = secondaryAttack = InputButtonState.None; }
            public void Dispose() { }
        }

        public struct MovementData : IPredictedData<MovementData>
        {
            public MovementCoreData movementData;
            public MovementInput lastInput;

            // 动画参数（通过预测系统同步到所有客户端）
            public float animMoveX;
            public float animMoveY;
            public float animSpeed;

            public void Dispose() { }
        }
    }
}
