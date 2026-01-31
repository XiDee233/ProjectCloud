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

        [Header("Combo (数据驱动)")]
        [SerializeField] private ComboTree comboTree;

        private ComboInputBuffer _comboBuffer;
        private ComboResolver _comboResolver;
        private float _comboLocalTime;

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
                
                // 从 meleeCombatSystem 获取当前节点索引（如果已设置）
                int nodeIndex = -1;
                if (meleeCombatSystem != null && meleeCombatSystem.CurrentNode != null)
                {
                    nodeIndex = GetIndexFromNode(meleeCombatSystem.CurrentNode);
                }
                
                currentState = new MeleeData 
                { 
                    movementData = data, 
                    isInitialized = false, 
                    comboIndex = 0,
                    currentNodeIndex = nodeIndex
                };
                Debug.Log("syb_enterMelee:" + Time.time);
            }

            if (comboTree != null)
            {
                _comboBuffer ??= new ComboInputBuffer
                {
                    MaxCount = comboTree.bufferMaxCount,
                    MaxAgeSeconds = comboTree.bufferMaxAgeSeconds
                };
                _comboResolver ??= new ComboResolver(comboTree, _comboBuffer);
            }
        }

        public void SetEntryCombo(ComboNode entryNode, ComboInputBuffer sharedBuffer, ComboResolver sharedResolver, float now)
        {
            if (meleeCombatSystem != null)
            {
                meleeCombatSystem.SetCurrentNode(entryNode);
            }

            // 设置 currentNodeIndex 到状态中，用于所有客户端同步
            var currentState = this.currentState;
            currentState.currentNodeIndex = GetIndexFromNode(entryNode);
            this.currentState = currentState;

            _comboBuffer = sharedBuffer;
            _comboResolver = sharedResolver;
            _comboLocalTime = now;
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

            // 初始化（所有客户端执行）
            if (!state.isInitialized && state.currentNodeIndex >= 0)
            {
                state.isInitialized = true;
                Debug.Log($"[MeleeStateNode] 攻击初始化 localTick={predictionManager.localTick}, context={predictionManager.localTickInContext}, time={Time.time:F3}");
                state.elapsedTime = 0f;
                state.isInComboWindow = false;
                state.isAttacking = true;
                
                var node = GetNodeFromIndex(state.currentNodeIndex);
                if (node != null) 
                {
                    meleeCombatSystem.CurrentNode = node;
                    meleeCombatSystem.IsAttacking = true;
                    meleeCombatSystem.TryAttack(node);
                }
                else
                {
                    ReturnToMovement();
                    return;
                }
            }

            // 从状态同步到 CombatSystem（确保回滚后状态一致）
            if (state.isInitialized)
            {
                meleeCombatSystem.IsAttacking = state.isAttacking;
                var node = GetNodeFromIndex(state.currentNodeIndex);
                meleeCombatSystem.CurrentNode = node;
            }

            // 更新动画（所有客户端执行）
            if (state.isInitialized)
            {
                // 计算已播放时间（直接累加 delta，状态回滚会自动处理）
                state.elapsedTime += delta;
                float elapsedTime = state.elapsedTime;

                // 推进 Timeline（包括动画 + Hitbox 判定）
                meleeCombatSystem.UpdateCombatState(elapsedTime, delta);

                // 应用 Root Motion
                Vector3 rootDelta = meleeCombatSystem.GetDeltaPosition();
                if (rootDelta.sqrMagnitude > 0)
                {
                    movementCore.ApplyRawMovement(rootDelta);
                }

                // 更新连招窗口状态
                state.isInComboWindow = meleeCombatSystem.GetComboWindowState();
            }

            // 连招逻辑（所有客户端执行）
            // Owner：使用真实输入
            // 远程：使用外推输入（可能不准确，但 Server 会纠正）
            if (state.isInComboWindow && comboTree != null && _comboResolver != null)
            {
                var collector = inputCollector;
                float now = collector != null ? collector.NowSeconds : 0f;

                if (collector != null)
                    _comboBuffer = collector.ComboBuffer;

                if (_comboBuffer != null)
                    _comboBuffer.Cleanup(now);

                var currentNode = meleeCombatSystem.CurrentNode;
                if (_comboResolver.TryResolveTransition(currentNode, now, out var transition, out int consumeCount))
                {
                    var nextNode = transition.toNode;
                    if (nextNode != null && meleeCombatSystem.TryCombo(nextNode))
                    {
                        _comboBuffer?.ConsumePrefix(consumeCount);

                        // 更新状态（会被同步到所有客户端）
                        state.elapsedTime = 0f;
                        state.isInComboWindow = false;
                        state.currentNodeIndex = GetIndexFromNode(nextNode);
                    }
                }
            }

            // 方向输入检测（所有客户端执行）
            if (state.isInComboWindow && input.movement.sqrMagnitude > 0.01f)
            {
                ReturnToMovement();
                return;
            }

            // 攻击完成检测
            bool attackComplete = meleeCombatSystem.CheckAttackComplete(state.elapsedTime);
            if (state.isAttacking && attackComplete)
            {
                state.isAttacking = false;
                Debug.Log($"[MeleeStateNode] syb______stop!! 时间={Time.time:F3}, elapsedTime={state.elapsedTime:F3}, currentNodeIndex={state.currentNodeIndex}");
                meleeCombatSystem.IsAttacking = false;
                meleeCombatSystem.CurrentNode = null;
            }

            if (!state.isAttacking)
            {
                ReturnToMovement();
            }

            movementCore.FinalizeMovement(ref state.movementData);
        }

        private void ReturnToMovement()
        {
            Debug.Log("syb_returnToMovement:" + Time.time);
            if (machine != null)
            {
                var s = machine.states.FirstOrDefault(x => x is MovementStateNode);
                if (s != null) machine.SetState(s);
            }
        }

        /// <summary>
        /// 从索引获取 ComboNode
        /// </summary>
        private ComboNode GetNodeFromIndex(int index)
        {
            if (comboTree == null) return null;
            
            // 先查找 entryNodes
            if (index < comboTree.entryNodes.Count)
            {
                return comboTree.entryNodes[index];
            }
            
            // 查找所有节点的 transitions
            int currentIndex = comboTree.entryNodes.Count;
            foreach (var entryNode in comboTree.entryNodes)
            {
                if (entryNode == null) continue;
                foreach (var transition in entryNode.transitions)
                {
                    if (transition.toNode != null)
                    {
                        if (currentIndex == index)
                            return transition.toNode;
                        currentIndex++;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// 获取 ComboNode 的索引
        /// </summary>
        private int GetIndexFromNode(ComboNode node)
        {
            if (comboTree == null || node == null) return -1;
            
            // 查找 entryNodes
            for (int i = 0; i < comboTree.entryNodes.Count; i++)
            {
                if (comboTree.entryNodes[i] == node)
                    return i;
            }
            
            // 查找 transitions
            int currentIndex = comboTree.entryNodes.Count;
            foreach (var entryNode in comboTree.entryNodes)
            {
                if (entryNode == null) continue;
                foreach (var transition in entryNode.transitions)
                {
                    if (transition.toNode == node)
                        return currentIndex;
                    if (transition.toNode != null)
                        currentIndex++;
                }
            }
            
            return -1;
        }

        [SerializeField] private PredictedPlayerInputCollector inputCollector;

        protected override void GetFinalInput(ref MeleeInput input)
        {
            var collector = inputCollector != null ? inputCollector : GetComponentInParent<PredictedPlayerInputCollector>();
            if (collector == null) { input.Reset(); return; }

            var c = collector.InputState;
            input.movement = c.movement;
            input.aimDirection = c.aimWorldDirection;
        }

        public struct MeleeInput : IPredictedData
        {
            public Vector2 movement;
            public Vector3 aimDirection;
            public void Reset() { movement = Vector2.zero; aimDirection = Vector3.zero; }
            public void Dispose() { }
        }

        public struct MeleeData : IPredictedData<MeleeData>
        {
            public MovementCoreData movementData;
            public MeleeInput inputSnapshot;
            public bool isInitialized;
            public int comboIndex;
            
            // PlayableGraph 状态
            public float elapsedTime;
            public bool isInComboWindow;
            public int currentNodeIndex;
            public bool isAttacking;
            
            public void Dispose() { }
        }
    }
}
