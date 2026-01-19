using UnityEngine;
using UnityEngine.Playables;

namespace Player.Combat.Tracks
{
    public class HitboxMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Transform hitboxTransform = playerData as Transform;
            if (hitboxTransform == null) return;

            int inputCount = playable.GetInputCount();
            
            // 混合所有激活的 Hitbox，执行判定逻辑
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight > 0f)
                {
                    ScriptPlayable<HitboxBehaviour> inputPlayable = (ScriptPlayable<HitboxBehaviour>)playable.GetInput(i);
                    HitboxBehaviour behaviour = inputPlayable.GetBehaviour();
                    
                    if (behaviour.isActive)
                    {
                        // 执行判定逻辑（完全回滚兼容）
                        // 判定结果应该存储在受击目标的状态数据中
                        // 不需要验证检查（isServer || isVerified）
                        PerformHitboxCheck(hitboxTransform, behaviour);
                    }
                }
            }
        }

        private void PerformHitboxCheck(Transform hitboxTransform, HitboxBehaviour behaviour)
        {
            // 计算判定位置
            Vector3 checkPosition = hitboxTransform.position + hitboxTransform.TransformDirection(behaviour.offset);
            
            // 执行球形判定（Physics.OverlapSphere 或自定义实现）
            // 注意：判定逻辑必须完全确定性
            Collider[] hits = Physics.OverlapSphere(checkPosition, behaviour.radius);
            
            foreach (var hit in hits)
            {
                // 获取受击目标的组件（假设有 IBeAttacked 接口或类似组件）
                // 判定结果存储在受击目标的状态数据中，回滚时会自动撤销
                // var target = hit.GetComponent<IBeAttacked>();
                // if (target != null) target.TakeDamage(behaviour.damage, behaviour.knockback);
            }
        }
    }
}
