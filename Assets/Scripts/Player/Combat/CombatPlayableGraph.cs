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
        private PlayableGraph _graph;
        private PlayableDirector _director; // 用于从 TimelineAsset 创建 Graph
        private double _currentTime;
        private double _duration;
        private TimelineAsset _currentTimelineAsset;
        
        // 连招窗口状态（通过 IComboWindowStateProvider 接口更新）
        private bool _isInComboWindow;

        public bool IsInitialized => _graph.IsValid();
        public double CurrentTime => _currentTime;
        public double Duration => _duration;

        private void Awake()
        {
            // 创建一个临时 PlayableDirector 用于从 TimelineAsset 创建 Graph
            _director = gameObject.AddComponent<PlayableDirector>();
            _director.playOnAwake = false;
        }

        public void Initialize(CombatTimelineData data)
        {
            if (data == null || data.TimelineAsset == null)
            {
                Debug.LogWarning("CombatPlayableGraph: Invalid CombatTimelineData");
                return;
            }

            // 清理旧的 Graph
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _currentTimelineAsset = data.TimelineAsset;
            _duration = data.TotalDuration;
            _currentTime = 0;
            _isInComboWindow = false;

            // 绑定 ComboWindowTrack 到 this（实现 IComboWindowStateProvider）
            // 必须在设置 playableAsset 之前绑定，确保 Graph 创建时绑定生效
            var tracks = _currentTimelineAsset.GetOutputTracks();
            foreach (var track in tracks)
            {
                if (track is Tracks.ComboWindowTrack)
                {
                    _director.SetGenericBinding(track, this);
                }
            }
            
            // 使用 PlayableDirector 创建 Graph
            _director.playableAsset = _currentTimelineAsset;
            _graph = _director.playableGraph;
            
            if (!_graph.IsValid())
            {
                Debug.LogError("CombatPlayableGraph: Failed to create PlayableGraph");
                return;
            }

            // 不自动播放，手动控制
            _director.Stop();
        }

        public void SetTime(double time)
        {
            if (!_graph.IsValid() || _currentTimelineAsset == null) return;

            _currentTime = Mathf.Clamp((float)time, 0f, (float)_duration);
            
            // 设置 PlayableDirector 的时间，这会更新 Graph 中所有 Playable 的时间
            _director.time = _currentTime;
            
            // 手动更新所有 Playable 的时间（如果需要更精细的控制）
            UpdatePlayableTime(_currentTime);
        }

        public void Evaluate(float delta)
        {
            if (!_graph.IsValid()) return;

            // 评估 Graph（不推进时间，只评估当前帧）
            _graph.Evaluate(delta);
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

        private void UpdatePlayableTime(double time)
        {
            // 手动设置 Graph 中所有 Playable 的时间
            // 这对于精细控制很重要
            if (!_graph.IsValid()) return;

            // 遍历所有输出，从 PlayableOutput 获取源 Playable
            int outputCount = _graph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var output = _graph.GetOutput(i);
                if (output.IsOutputValid())
                {
                    var sourcePlayable = output.GetSourcePlayable();
                    if (sourcePlayable.IsValid())
                    {
                        SetPlayableTimeRecursive(sourcePlayable, time);
                    }
                }
            }
        }

        private void SetPlayableTimeRecursive(Playable playable, double time)
        {
            if (!playable.IsValid()) return;

            playable.SetTime(time);

            // 递归设置所有输入的时间
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var input = playable.GetInput(i);
                if (input.IsValid())
                {
                    SetPlayableTimeRecursive(input, time);
                }
            }
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}
