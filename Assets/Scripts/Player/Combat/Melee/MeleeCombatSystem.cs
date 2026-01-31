using UnityEngine;
using System;
using Player.Combat;

namespace Player.Combat.Melee
{
    [AddComponentMenu("Player/Combat/Melee Combat System")]
    public class MeleeCombatSystem : MonoBehaviour
    {
        [SerializeField] private CombatPlayableGraph combatPlayableGraph;

        public bool IsAttacking { get; set; }
        public ComboNode CurrentNode { get; set; }


        public void SetCurrentNode(ComboNode node)
        {
            CurrentNode = node;
        }

        private void Awake()
        {
            if (!combatPlayableGraph) combatPlayableGraph = GetComponent<CombatPlayableGraph>();
        }



        public bool TryAttack(ComboNode node)
        {
            if (node == null || node.timelineAsset == null)
                return false;

            CurrentNode = node;
            IsAttacking = true;

            if (combatPlayableGraph != null)
            {
                combatPlayableGraph.Initialize(node.timelineAsset);
            }

            return true;
        }

        public void StopCombat()
        {
            if (combatPlayableGraph != null)
            {
                combatPlayableGraph.Stop();
            }
            IsAttacking = false;
            Debug.Log("syb______stop!!" + Time.time);
            CurrentNode = null;
        }

        public bool TryCombo(ComboNode nextNode)
        {
            if (nextNode == null) return false;
            return TryAttack(nextNode);
        }

        public void UpdateCombatState(float elapsedTime, float delta)
        {
            if (combatPlayableGraph == null || !combatPlayableGraph.IsInitialized) return;

            combatPlayableGraph.SetTime(elapsedTime);
            combatPlayableGraph.Evaluate();
        }

        public bool CheckAttackComplete(float elapsedTime)
        {
            if (CurrentNode == null)
            {
                Debug.Log($"[CheckAttackComplete] 返回true原因: CurrentNode == null, elapsedTime={elapsedTime:F3}");
                return true;
            }
            if (elapsedTime >= CurrentNode.TotalDuration)
            {
                Debug.Log($"[CheckAttackComplete] 返回true原因: elapsedTime({elapsedTime:F3}) >= TotalDuration({CurrentNode.TotalDuration:F3}), Node={CurrentNode.name}");
                return true;
            }
            if (combatPlayableGraph != null && combatPlayableGraph.IsComplete())
            {
                Debug.Log($"[CheckAttackComplete] 返回true原因: combatPlayableGraph.IsComplete() == true, elapsedTime={elapsedTime:F3}, Node={CurrentNode.name}");
                return true;
            }
            return false;
        }

        public bool GetComboWindowState()
        {
            if (combatPlayableGraph == null || !combatPlayableGraph.IsInitialized)
                return false;

            return combatPlayableGraph.IsInComboWindow();
        }

        public Vector3 GetDeltaPosition()
        {
            return combatPlayableGraph != null ? combatPlayableGraph.GetDeltaPosition() : Vector3.zero;
        }

        public Quaternion GetDeltaRotation()
        {
            return combatPlayableGraph != null ? combatPlayableGraph.GetDeltaRotation() : Quaternion.identity;
        }
    }
}
