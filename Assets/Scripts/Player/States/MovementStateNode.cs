using UnityEngine;
using Player.Core;
using Player.Animation;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Linq;
using Player.Combat.Melee;

namespace Player.States
{
    public class MovementStateNode : PredictedStateNode<MovementStateNode.MovementInput, MovementStateNode.MovementData>
    {
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private ControlAuthority controlAuthority;
        [SerializeField] private MovementAnimationController animationController;
        [SerializeField] private ComboTree comboTree;

        private ComboResolver _comboResolver;
        
        private void Awake()
        {
            if (movementCore == null) movementCore = GetComponentInParent<PlayerMovementCore>();
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();
            if (animationController == null) animationController = GetComponentInParent<MovementAnimationController>();
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

        protected override void Simulate(MovementInput input, ref MovementData state, float delta) { }

        protected override void StateSimulate(in MovementInput input, ref MovementData state, float delta)
        {
            if (movementCore == null) return;

            // 所有客户端都执行移动模拟
            movementCore.UpdateGroundedState(ref state.movementData);

            // Dash 检测（所有客户端执行，Owner用真实输入，远程用外推输入）
            bool dashTapped = inputCollector != null && 
                              inputCollector.InputState.dashIsPressed && 
                              !inputCollector.InputState.prevDashIsPressed;

            if (machine != null && dashTapped)
            {
                var dash = machine.states.FirstOrDefault(x => x is DashStateNode);
                if (dash != null)
                {
                    machine.SetState(dash);
                    return;
                }
            }

            // 基础连招：移动状态下允许消耗输入缓冲去匹配起手
            // 所有客户端执行，Owner用真实输入，远程用外推输入
            if (inputCollector != null)
            {
                var buffer = inputCollector.ComboBuffer;
                float now = inputCollector.NowSeconds;

                if (comboTree != null && buffer != null)
                {
                    _comboResolver ??= new ComboResolver(comboTree, buffer);

                    if (_comboResolver.TryResolveEntry(now, out var entryNode, out int consumeCount))
                    {
                        buffer.ConsumePrefix(consumeCount);

                        var melee = machine.states.FirstOrDefault(x => x is MeleeStateNode) as MeleeStateNode;
                        if (melee != null)
                        {
                            melee.SetEntryCombo(entryNode, buffer, _comboResolver, now);
                            machine.SetState(melee);
                            return;
                        }

                        var ranged = machine.states.FirstOrDefault(x => x is RangedStateNode) as RangedStateNode;
                        if (ranged != null && entryNode.name.Contains("Ranged"))
                        {
                            machine.SetState(ranged);
                            return;
                        }
                    }
                }
            }

            // 移动计算（所有客户端执行）
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

            // 计算并存储动画参数（用于同步到所有客户端）
            if (animationController != null)
            {
                var (moveX, moveY, speed) = animationController.CalculateAnimationParameters(state.movementData, movementCore.MaxMoveSpeed);
                state.animMoveX = moveX;
                state.animMoveY = moveY;
                state.animSpeed = speed;
            }
        }

        /// <summary>
        /// 视图层更新 - 所有客户端都会调用，用于更新动画
        /// </summary>
        protected override void UpdateView(MovementData viewState, MovementData? verifiedState)
        {
            base.UpdateView(viewState, verifiedState);

            // 所有客户端都应用动画参数
            if (animationController != null)
            {
                animationController.ApplyAnimationParameters(
                    viewState.animMoveX,
                    viewState.animMoveY,
                    viewState.animSpeed
                );
            }
        }

        [SerializeField] private PredictedPlayerInputCollector inputCollector;

        protected override void GetFinalInput(ref MovementInput input)
        {
            var collector = inputCollector;
            if (collector == null) { input.Reset(); return; }

            var c = collector.InputState;
            input.movement = c.movement;
            input.aimDirection = c.aimWorldDirection;
        }

        public struct MovementInput : IPredictedData
        {
            public Vector2 movement;
            public Vector3 aimDirection;
            public void Reset() { movement = Vector2.zero; aimDirection = Vector3.zero; }
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
