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
        [SerializeField] private MeleeAttackData[] comboChain; // 连招链条：按顺序存储攻击数据

        private void Awake()
        {
            if (movementCore == null) movementCore = GetComponentInParent<PlayerMovementCore>();
            if (meleeCombatSystem == null) meleeCombatSystem = GetComponentInParent<MeleeCombatSystem>();
            if (controlAuthority == null) controlAuthority = GetComponentInParent<ControlAuthority>();
        }

        public override void Enter()
        {
            base.Enter();
            if (movementCore != null)
            {
                var data = movementCore.CreateDefaultMovementData();
                movementCore.SetMovementLocked(ref data, true);
                movementCore.SetRotationLocked(ref data, true);
                currentState = new MeleeData { movementData = data, isInitialized = false, comboIndex = 0 };
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (meleeCombatSystem != null) meleeCombatSystem.StopCombat();
        }

        protected override void Simulate(MeleeInput input, ref MeleeData state, float delta) { }

        protected override void StateSimulate(in MeleeInput input, ref MeleeData state, float delta)
        {
            if (movementCore == null || meleeCombatSystem == null) return;
            if (!machine.isOwner && !machine.isServer) return;

            // 初始化：播放第一击
            if (!state.isInitialized)
            {
                state.isInitialized = true;
                state.attackStartTick = predictionManager.localTick; // 记录开始 Tick
                state.comboIndex = 0;
                state.elapsedTime = 0f;
                state.isInComboWindow = false;
                
                if (comboChain != null && comboChain.Length > 0)
                {
                    meleeCombatSystem.TryAttack(comboChain[0]);
                }
                else
                {
                    meleeCombatSystem.TryAttack();
                }
            }

            // 计算已播放时间（基于 Tick，完全确定性）
            ulong currentTick = predictionManager.localTick;
            ulong tickOffset = currentTick - state.attackStartTick;
            float elapsedTime = tickOffset * predictionManager.tickDelta;
            state.elapsedTime = elapsedTime;

            // 更新战斗状态（推进 PlayableGraph）
            meleeCombatSystem.UpdateCombatState(elapsedTime, delta);

            // 应用 Root Motion（手动提取并应用到预测状态）
            Vector3 rootDelta = meleeCombatSystem.GetDeltaPosition();
            if (rootDelta.sqrMagnitude > 0)
            {
                movementCore.ApplyRawMovement(rootDelta);
            }

            // 连招检测：从 ComboWindowTrack 读取窗口状态
            state.isInComboWindow = meleeCombatSystem.GetComboWindowState();

            if (input.primaryAttack.wasPressed && state.isInComboWindow)
            {
                // 获取下一个连招攻击
                MeleeAttackData nextAttack = GetNextComboAttack(state.comboIndex);
                if (nextAttack != null)
                {
                    if (meleeCombatSystem.TryCombo(nextAttack))
                    {
                        state.comboIndex++;
                        state.attackStartTick = predictionManager.localTick; // 重置开始时间
                        state.elapsedTime = 0f;
                        state.isInComboWindow = false;
                        
                        // 消费输入，防止重复触发
                        var p = controlAuthority?.CurrentProvider;
                        p?.ConsumeInput(InputActionType.PrimaryAttack);
                    }
                }
            }

            // 攻击完成，返回移动状态
            if (!meleeCombatSystem.IsAttacking)
            {
                ReturnToMovement();
            }

            movementCore.FinalizeMovement(ref state.movementData);
        }

        private MeleeAttackData GetNextComboAttack(int currentIndex)
        {
            if (comboChain == null || comboChain.Length == 0) return null;
            if (currentIndex + 1 >= comboChain.Length) return null;
            return comboChain[currentIndex + 1];
        }

        private void ReturnToMovement()
        {
            if (machine != null)
            {
                var s = machine.states.FirstOrDefault(x => x is MovementStateNode);
                if (s != null) machine.SetState(s);
            }
        }

        protected override void GetFinalInput(ref MeleeInput input)
        {
            var p = controlAuthority != null ? controlAuthority.CurrentProvider : null;
            if (p == null) { input.Reset(); return; }

            input.primaryAttack = p.PrimaryAttack;
            // 注意：这里不消费输入，让 StateSimulate 在连招窗口检查时再消费

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
            public int comboIndex; // 当前连招索引
            
            // 新增：PlayableGraph 状态
            public ulong attackStartTick; // 攻击开始的 Tick（用于计算已播放时间）
            public float elapsedTime; // 已播放时间（基于 Tick 计算，用于回滚兼容）
            
            // 新增：连招窗口状态（从 ComboWindowTrack 读取）
            public bool isInComboWindow;
            
            // 注意：Hitbox 判定状态不需要存储在 MeleeData 中
            // 判定结果存储在受击目标的状态数据中（如果受击目标是 PredictedIdentity）
            // 回滚时会自动撤销判定结果
            
            public void Dispose() { }
        }
    }
}
