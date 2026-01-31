using UnityEngine;
using Player.Core;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using MoreMountains.Feedbacks;
using System.Linq;

namespace Player.States
{
    public class DashStateNode : PredictedStateNode<DashStateNode.DashInput, DashStateNode.DashData>
    {
        [Header("核心引用")]
        [SerializeField] private PlayerMovementCore movementCore;
        [SerializeField] private ControlAuthority controlAuthority;
        [SerializeField] private PredictedPlayerInputCollector inputCollector;

        [Header("冲刺参数")]
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashSpeedMultiplier = 3f;

        [Header("反馈效果")]
        [SerializeField] private MMFeedbacks dashStartFeedbacks;
        [SerializeField] private MMFeedbacks dashEndFeedbacks;


        private void Awake()
        {
            if (movementCore == null) movementCore = GetComponentInParent<PlayerMovementCore>();
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();
            if (inputCollector == null) inputCollector = GetComponentInParent<PredictedPlayerInputCollector>();
        }

        public override void Enter()
        {
            base.Enter();

            if (movementCore != null)
            {
                var data = movementCore.CreateDefaultMovementData();

                currentState = new DashData
                {
                    movementData = data,
                    isInitialized = false
                };
            }

            if (dashStartFeedbacks != null) dashStartFeedbacks.PlayFeedbacks();
        }

        public override void Exit()
        {
            base.Exit();
            if (movementCore != null)
            {
                var data = currentState.movementData;

                movementCore.SetDashVelocity(ref data, Vector3.zero);
                movementCore.SetMovementLocked(ref data, false);
                movementCore.SetRotationLocked(ref data, false);
                movementCore.ClearRotationOverride(ref data);
            }
            if (dashEndFeedbacks != null) dashEndFeedbacks.PlayFeedbacks();
        }

        // 计算冲刺方向：优先移动输入，其次瞄准方向，最后正前方
        private Vector3 CalculateDashDirection(IInputProvider provider)
        {
            if (provider == null || movementCore == null) return Vector3.forward;

            // 优先使用移动输入（相机相对）
            if (provider.Movement.sqrMagnitude > 0.01f)
            {
                return movementCore.CalculateCameraRelativeMovement(provider.Movement).normalized;
            }

            // 次优先使用瞄准方向
            if (provider.AimWorldDirection.sqrMagnitude > 0.01f)
            {
                Vector3 aimDir = provider.AimWorldDirection;
                aimDir.y = 0f; // 投影到水平面
                return aimDir.normalized;
            }

            // 默认使用正前方
            return Vector3.forward;
        }

        // 空占位符方法，绕过 PurrNet 引擎的自动调用
        protected override void Simulate(DashInput input, ref DashData state, float delta) { }

        protected override void StateSimulate(in DashInput input, ref DashData state, float delta)
        {
            if (movementCore == null) return;

            // 如果不是拥有者，且不是服务器，我们不应该运行模拟逻辑
            if (!machine.isOwner && !machine.isServer) return;

            // 1. 确定性初始化：在全网同步的第一个Tick执行
            if (!state.isInitialized)
            {
                state.isInitialized = true;
                state.direction = input.direction;
                state.timer = dashDuration;
                state.initialSpeed = (dashDistance / dashDuration) * dashSpeedMultiplier;

                // 修改状态数据
                movementCore.SetDashVelocity(ref state.movementData, state.direction * state.initialSpeed);
                movementCore.SetMovementLocked(ref state.movementData, true);
                movementCore.SetRotationLocked(ref state.movementData, true);
                movementCore.OverrideRotation(ref state.movementData, Mathf.Atan2(state.direction.x, state.direction.z) * Mathf.Rad2Deg);
            }

            // 更新地面状态
            movementCore.UpdateGroundedState(ref state.movementData);

            // 计算速度衰减：从初始爆发速度开始线性衰减到0
            float normalizedTime = 1f - (state.timer / dashDuration);
            float currentSpeed = state.initialSpeed * (1f - normalizedTime);

            // 更新冲刺速度
            movementCore.SetDashVelocity(ref state.movementData, state.direction * currentSpeed);

            // 执行物理移动
            movementCore.ApplyAcceleratedMovement(ref state.movementData, Vector3.zero, delta);

            // 应用重力
            movementCore.ApplyGravity(ref state.movementData, delta);

            // 更新计时器
            state.timer -= delta;
            if (state.timer <= 0f)
            {
                // 冲刺结束，切换回移动状态
                if (machine != null)
                {
                    var s = machine.states.FirstOrDefault(x => x is MovementStateNode);
                    if (s != null) machine.SetState(s);
                }
                return;
            }

            movementCore.FinalizeMovement(ref state.movementData);
        }

        protected override void GetFinalInput(ref DashInput input)
        {

            var c = inputCollector.InputState;

            // 使用与 CalculateDashDirection 相同的优先级逻辑，但输入来源必须是预测系统采样值
            if (c.movement.sqrMagnitude > 0.01f)
            {
                input.direction = movementCore.CalculateCameraRelativeMovement(c.movement).normalized;

                return;
            }

            if (c.aimWorldDirection.sqrMagnitude > 0.01f)
            {
                var aimDir = c.aimWorldDirection;
                aimDir.y = 0f;
                input.direction = aimDir.normalized;
                return;
            }

            input.direction = Vector3.forward;
        }

        public struct DashInput : IPredictedData
        {
            public Vector3 direction;
            public void Reset() => direction = Vector3.forward;
            public void Dispose() { }
        }

        public struct DashData : IPredictedData<DashData>
        {
            public MovementCoreData movementData;
            public Vector3 direction;
            public float timer;
            public float initialSpeed;
            public bool isInitialized;
            public void Dispose() { }
        }
    }
}
