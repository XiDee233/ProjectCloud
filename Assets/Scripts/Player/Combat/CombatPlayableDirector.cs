using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;

namespace Player.Combat
{
    [RequireComponent(typeof(PlayableDirector))]
    [AddComponentMenu("Player/Combat/Combat Playable Director")]
    public class CombatPlayableDirector : MonoBehaviour
    {
        private PlayableDirector _director;
        
        public event Action OnPlayComplete;
        public bool IsPlaying => _director.state == PlayState.Playing;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.extrapolationMode = DirectorWrapMode.None;
        }

        public void Play(CombatTimelineData data)
        {
            if (data == null || data.TimelineAsset == null) return;

            _director.playableAsset = data.TimelineAsset;
            _director.Play();
            
            // 简单轮询或通过事件监听结束（Timeline 不支持直接回调，可以用 Signal 或 PlayableGraph 监听）
        }

        public void Stop()
        {
            _director.Stop();
        }

        private void Update()
        {
            if (_director.state != PlayState.Playing && _director.playableAsset != null)
            {
                // 检测播放结束
                if (_director.time >= _director.duration || _director.time <= 0 && _director.state == PlayState.Paused)
                {
                    var complete = OnPlayComplete;
                    OnPlayComplete = null;
                    complete?.Invoke();
                    _director.playableAsset = null;
                }
            }
        }
    }
}
