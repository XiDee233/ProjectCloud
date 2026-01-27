using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace Player.Combat.Tracks
{
    public class HitboxMixerBehaviour : PlayableBehaviour
    {
        private Transform _lastTransform;

        // 用于追踪每个输入 Clip 的上一帧状态，以实现扫掠检测
        private struct InputFrameData
        {
            public Vector3 position;
            public Quaternion rotation;
            public double time;
            public bool wasActive;
        }

        private readonly Dictionary<int, InputFrameData> _lastInputDatas = new Dictionary<int, InputFrameData>();
        private readonly HashSet<Collider> _hitColliders = new HashSet<Collider>();

        public override void OnGraphStop(Playable playable)
        {
            _lastInputDatas.Clear();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _lastInputDatas.Clear();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _lastTransform = playerData as Transform;
            if (_lastTransform == null) return;

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
                        // 计算当前判定位置和旋转
                        Vector3 currentPosition = _lastTransform.position + _lastTransform.TransformDirection(behaviour.offset);
                        Quaternion currentRotation = _lastTransform.rotation;
                        double currentTime = inputPlayable.GetTime();

                        _hitColliders.Clear();

                        // 检查是否存在上一帧数据，并且是连续播放（不是 Seek）
                        if (_lastInputDatas.TryGetValue(i, out var lastData) && lastData.wasActive)
                        {
                            double deltaTime = currentTime - lastData.time;
                            if (deltaTime > 0 && deltaTime < 0.1)
                            {
                                // 使用步进采样代替直线扫掠
                                PerformSamplingHitboxCheck(lastData, currentPosition, currentRotation, behaviour);
                            }
                        }
                        else
                        {
                            // 第一次激活或非连续播放，仅执行当前位置检测
                            Collider[] overlaps = Physics.OverlapBox(currentPosition, behaviour.size * 0.5f, currentRotation);
                            foreach (var col in overlaps) _hitColliders.Add(col);
                        }

                        // 更新上一帧数据
                        _lastInputDatas[i] = new InputFrameData
                        {
                            position = currentPosition,
                            rotation = currentRotation,
                            time = currentTime,
                            wasActive = true
                        };

                        // 处理结果
                        ProcessHits(behaviour);
                    }
                    else
                    {
                        _lastInputDatas[i] = new InputFrameData { wasActive = false };
                    }
                }
            }
        }

        private void PerformSamplingHitboxCheck(InputFrameData lastData, Vector3 currentPos, Quaternion currentRot, HitboxBehaviour behaviour)
        {
            // 计算位移和旋转变化量
            float distance = Vector3.Distance(lastData.position, currentPos);
            float angle = Quaternion.Angle(lastData.rotation, currentRot);

            // 根据位移和旋转动态决定采样次数 (最少 2 次，即起点和终点)
            // 比如每 0.2 米采样一次，或者每 15 度采样一次
            int distanceSamples = Mathf.CeilToInt(distance / 0.2f);
            int angleSamples = Mathf.CeilToInt(angle / 15f);
            int sampleCount = Mathf.Max(2, Mathf.Max(distanceSamples, angleSamples));
            
            // 限制最大采样数防止性能爆炸
            sampleCount = Mathf.Min(sampleCount, 10);

            // 进行步进采样检测
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                Vector3 sampledPos = Vector3.Lerp(lastData.position, currentPos, t);
                Quaternion sampledRot = Quaternion.Slerp(lastData.rotation, currentRot, t);

                Collider[] overlaps = Physics.OverlapBox(sampledPos, behaviour.size * 0.5f, sampledRot);
                foreach (var col in overlaps)
                {
                    _hitColliders.Add(col);
                }
            }
        }

        private void ProcessHits(HitboxBehaviour behaviour)
        {
            foreach (var hit in _hitColliders)
            {
                // 获取受击目标的组件（假设有 IBeAttacked 接口或类似组件）
                // 判定结果存储在受击目标的状态数据中，回滚时会自动撤销
                // var target = hit.GetComponent<IBeAttacked>();
                // if (target != null) target.TakeDamage(behaviour.damage, behaviour.knockback);
            }
        }

        public void DrawGizmos(Playable playable)
        {
            if (_lastTransform == null || !playable.IsValid()) return;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight > 0f)
                {
                    ScriptPlayable<HitboxBehaviour> inputPlayable = (ScriptPlayable<HitboxBehaviour>)playable.GetInput(i);
                    HitboxBehaviour behaviour = inputPlayable.GetBehaviour();
                    
                    if (behaviour.isActive)
                    {
                        // 实时从 transform 计算位置
                        Vector3 checkPosition = _lastTransform.position + _lastTransform.TransformDirection(behaviour.offset);
                        Quaternion rotation = _lastTransform.rotation;

                        // 保存当前的 Gizmos 矩阵
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        
                        // 设置 Gizmos 矩阵以支持带旋转的 Box 绘制
                        // 使用 TRS (Position, Rotation, Scale)
                        Gizmos.matrix = Matrix4x4.TRS(checkPosition, rotation, Vector3.one);

                        // 绘制实心 Box (注意这里的位置是 0,0,0 因为位置已经在矩阵中设置了)
                        Gizmos.color = behaviour.debugColor;
                        Gizmos.DrawCube(Vector3.zero, behaviour.size);
                        
                        // 绘制线框 Box
                        Gizmos.color = new Color(behaviour.debugColor.r, behaviour.debugColor.g, behaviour.debugColor.b, 1f);
                        Gizmos.DrawWireCube(Vector3.zero, behaviour.size);

                        // 恢复矩阵
                        Gizmos.matrix = oldMatrix;
                    }
                }
            }
        }
    }
}
