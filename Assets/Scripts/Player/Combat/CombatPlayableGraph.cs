using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using Player.Combat.Tracks;

namespace Player.Combat
{
    [AddComponentMenu("Player/Combat/Combat Playable Graph")]
    public class CombatPlayableGraph : MonoBehaviour, IComboWindowStateProvider
    {
        [SerializeField] private Animator _animator;
        private PlayableGraph _graph;
        [SerializeField]
        private PlayableDirector _director; // 用于从 TimelineAsset 创建 Graph
        [SerializeField] private bool _showHitboxGizmos = true;
        private double _currentTime;
        private double _duration;
        private TimelineAsset _currentTimelineAsset;
        
        // 连招窗口状态（通过 IComboWindowStateProvider 接口更新）
        private bool _isInComboWindow;

        // 自动播放相关
        private System.Action _onComplete;

        // 保存原始 Animator Controller，战斗时临时禁用
        private RuntimeAnimatorController _cachedAnimatorController;


        private Quaternion _lastRootRotation;

        public bool IsInitialized => _graph.IsValid();
        public double CurrentTime => _currentTime;
        public double Duration => _duration;

        private void Awake()
        {
 
            _director.playOnAwake = false;
            // 核心修复：设置为手动更新模式，确保预测系统完全接管时间，且 Play() 状态能激活动画覆盖
            _director.timeUpdateMode = DirectorUpdateMode.Manual;

        }

        public void Initialize(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                Debug.LogWarning("CombatPlayableGraph: Invalid TimelineAsset");
                return;
            }

            // 移除手动 Destroy 调用，由 Director 接管生命周期
            _currentTimelineAsset = timelineAsset;
            _duration = timelineAsset.duration;
            _currentTime = 0;
            _isInComboWindow = false;
            _onComplete = null;

            // 核心修复：必须开启 applyRootMotion 才能让 Animator 处于“增量模式”，避免被 Timeline 强制写入绝对坐标（回原点）
            // 我们通过 OnAnimatorMove() 回调在 MovementCore 中拦截自动位移，实现手动控制
            if (_animator)
            {
                _animator.applyRootMotion = true;
                
                // 核心修复：暂时禁用 Animator Controller，防止与 Timeline 冲突
                // Animator Controller 会持续播放动画并覆盖 Timeline 的输出
                _cachedAnimatorController = _animator.runtimeAnimatorController;
                _animator.runtimeAnimatorController = null;
            }

            // 绑定轨道到相应对象
            var tracks = _currentTimelineAsset.GetOutputTracks();
            foreach (var track in tracks)
            {
                if (track is Tracks.ComboWindowTrack)
                {
                    _director.SetGenericBinding(track, this);
                }
                else if (track is Tracks.CombatEventTrack)
                {
                    _director.SetGenericBinding(track, gameObject);
                }
                else if (track is AnimationTrack animTrack)
                {
                    // 确保使用 ApplySceneOffsets
                    animTrack.trackOffset = TrackOffset.ApplySceneOffsets;
                    if (_animator) _director.SetGenericBinding(track, _animator);
                }
            }
            
            _director.playableAsset = _currentTimelineAsset;
            _director.RebuildGraph();
            _graph = _director.playableGraph;
            
            if (!_graph.IsValid())
            {
                Debug.LogError("CombatPlayableGraph: Failed to create PlayableGraph");
                return;
            }

            // 核心修复：调用 Play() 但由于模式是 Manual，时间不会自动走，
            // 但此状态会强制 AnimationPlayableOutput 覆盖 Animator Controller 的权重
            _director.Play();
            _director.time = 0;
            _director.Evaluate();


        }

        public void Play(TimelineAsset timelineAsset, System.Action onComplete = null)
        {
            //Initialize(timelineAsset);
            //_onComplete = onComplete;
            //_isAutoPlaying = true;
        }

        public void Stop()
        {
            _onComplete = null;
            
            if (_director != null)
            {
                _director.Stop();
                _director.playableAsset = null;
            }

            if (_animator)
            {
                _animator.applyRootMotion = false;
                
                // 恢复原始 Animator Controller
                if (_cachedAnimatorController != null)
                {
                    _animator.runtimeAnimatorController = _cachedAnimatorController;
                    _cachedAnimatorController = null;
                }
                
                _animator.Rebind();
            }
        }


        public void SetTime(double time)
        {
            if (!_graph.IsValid() || _currentTimelineAsset == null) return;

            _currentTime = Mathf.Clamp((float)time, 0f, (float)_duration);
            
            // 设置 PlayableDirector 的时间，这会更新 Graph 中所有 Playable 的时间
            _director.time = _currentTime;
        }

        public void Evaluate(float delta)
        {
            if (!_graph.IsValid()) return;

            // 核心修复：在 Manual 模式下，必须调用 _director.Evaluate() 
            // 才能根据 _director.time 评估整个 Timeline 并将输出应用到 Animator
            if (_director != null)
            {
                _director.Evaluate();
            }
        }

        /// <summary>
        /// 获取当前帧动画产生的位移（Root Motion）
        /// 必须在 Evaluate 之后调用
        /// </summary>
        public Vector3 GetDeltaPosition()
        {
            Vector3 delta = _animator.deltaPosition;
            return delta;
        }

        /// <summary>
        /// 获取当前帧动画产生的旋转（Root Motion）
        /// 必须在 Evaluate 之后调用
        /// </summary>
        public Quaternion GetDeltaRotation()
        {
            Quaternion delta = _animator.deltaRotation;
            return delta;
        }

        public bool IsComplete()
        {
            return _currentTime >= _duration;
        }

        public bool IsInComboWindow()
        {
            // 查询连招窗口状态（通过 IComboWindowStateProvider 接口更新）
            return _isInComboWindow;
        }

        /// <summary>
        /// 实现 IComboWindowStateProvider 接口：设置连招窗口状态
        /// 由 ComboWindowMixerBehaviour.ProcessFrame 调用
        /// </summary>
        public void SetComboWindowState(bool isOpen)
        {
            _isInComboWindow = isOpen;
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        /// <summary>
        /// 在 Scene 视图中绘制 Hitbox 相关 Gizmos
        /// 该方法会在编辑器中和运行时触发，用于可视化攻击判定（Hitbox）
        /// </summary>
        private void OnDrawGizmos()
        {
            // 如果未开启 Hitbox 可视化选项则直接返回
            if (!_showHitboxGizmos) return;

            // 优先使用运行时 Graph（_graph）绘制 Gizmos
            if (_graph.IsValid())
            {
                DrawGizmosRecursive(_graph);
            }
            // 如果运行时 Graph 无效，但 Timeline Editor 的 Director 有 Graph（通常用于编辑器预览/拖拽）
            else if (_director != null && _director.playableGraph.IsValid())
            {
                DrawGizmosRecursive(_director.playableGraph);
            }
        }

        /// <summary>
        /// 递归遍历 PlayableGraph 的所有输出，查找并绘制 Hitbox Mixer
        /// </summary>
        /// <param name="graph">要遍历的 PlayableGraph</param>
        private void DrawGizmosRecursive(PlayableGraph graph)
        {
            int outputCount = graph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var output = graph.GetOutput(i);
                // 检查输出是否有效
                if (output.IsOutputValid())
                {
                    var sourcePlayable = output.GetSourcePlayable();
                    // 检查 Playable 是否有效
                    if (sourcePlayable.IsValid())
                    {
                        // 对每个源 Playable 递归绘制 Gizmos
                        DrawGizmosForPlayable(sourcePlayable);
                    }
                }
            }
        }

        /// <summary>
        /// 递归查找所有 Playable，如果是 HitboxMixer 类型则调用其 Gizmos 绘制方法
        /// </summary>
        /// <param name="playable">当前遍历到的 Playable</param>
        private void DrawGizmosForPlayable(Playable playable)
        {
            // Playable 无效直接返回
            if (!playable.IsValid()) return;

            // 如果当前 Playable 是我们自定义的 HitboxMixerBehaviour 类型
            if (playable.GetPlayableType() == typeof(HitboxMixerBehaviour))
            {
                // 获取 HitboxMixer 行为实例，调用其 Gizmos 绘制方法
                var mixer = ((ScriptPlayable<HitboxMixerBehaviour>)playable).GetBehaviour();
                mixer.DrawGizmos(playable);
            }

            // 递归处理该 Playable 的所有输入（输入即子节点/下级 Playable）
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var input = playable.GetInput(i);
                DrawGizmosForPlayable(input);
            }
        }
    }
}
