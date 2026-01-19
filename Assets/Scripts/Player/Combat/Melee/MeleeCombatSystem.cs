using UnityEngine;
using System;
using Player.Combat;

namespace Player.Combat.Melee
{
    [AddComponentMenu("Player/Combat/Melee Combat System")]
    public class MeleeCombatSystem : MonoBehaviour
    {
        [SerializeField] private CombatPlayableGraph combatPlayableGraph;
        [SerializeField] private MeleeAttackData defaultAttack;

        public bool IsAttacking { get; private set; }
        public MeleeAttackData CurrentAttack { get; private set; }

        public event Action OnAttackComplete;

        private void Awake()
        {
            if (!combatPlayableGraph) combatPlayableGraph = GetComponent<CombatPlayableGraph>();
        }

        public bool CanStartAttack()
        {
            return !IsAttacking;
        }

        public bool TryAttack()
        {
            return TryAttack(defaultAttack);
        }

        public bool TryAttack(MeleeAttackData attackData)
        {
            if (attackData == null || !CanStartAttack())
                return false;

            CurrentAttack = attackData;
            IsAttacking = true;

            // 初始化 PlayableGraph（不播放）
            if (combatPlayableGraph != null)
            {
                combatPlayableGraph.Initialize(attackData.animationData);
            }

            attackData.onAttackPerform?.Invoke(attackData);
            return true;
        }

        /// <summary>
        /// 请求连招：由状态机在检测到输入缓冲时调用
        /// 只有在连招窗口开启时才能执行，否则忽略（依赖 wasPressed 的缓冲机制）
        /// </summary>
        public bool TryCombo(MeleeAttackData nextAttack)
        {
            if (nextAttack == null) return false;

            // 检查是否可以从当前攻击连到下一个攻击
            if (CurrentAttack != null && CurrentAttack.canComboTo != null)
            {
                if (!CurrentAttack.canComboTo(nextAttack))
                    return false;
            }

            // 窗口开启，立即执行
            return TryAttack(nextAttack);
        }

        /// <summary>
        /// 更新战斗状态：由 StateSimulate 调用，更新 PlayableGraph 时间
        /// </summary>
        public void UpdateCombatState(float elapsedTime)
        {
            if (combatPlayableGraph == null || !combatPlayableGraph.IsInitialized) return;

            // 更新 PlayableGraph 时间
            combatPlayableGraph.SetTime(elapsedTime);
            combatPlayableGraph.Evaluate(0f); // 评估当前帧

            // 检查是否完成
            if (elapsedTime >= CurrentAttack.animationData.TotalDuration || combatPlayableGraph.IsComplete())
            {
                IsAttacking = false;
                CurrentAttack = null;
                OnAttackComplete?.Invoke();
            }
        }

        /// <summary>
        /// 获取连招窗口状态：从 ComboWindowTrack 的 Mixer 读取当前窗口状态
        /// </summary>
        public bool GetComboWindowState()
        {
            if (combatPlayableGraph == null || !combatPlayableGraph.IsInitialized)
                return false;

            return combatPlayableGraph.IsInComboWindow();
        }
    }
}
