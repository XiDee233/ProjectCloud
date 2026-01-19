using UnityEngine;
using UnityEngine.Playables;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class HitboxBehaviour : PlayableBehaviour
    {
        [Header("判定参数")]
        public Vector3 offset;
        public float radius = 0.5f;
        public int damage = 10;
        public float knockback = 5f;
        
        [Header("状态")]
        public bool isActive = false;
        private bool _wasActive = false;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // 根据 Playable 的时间判断是否激活
            double time = playable.GetTime();
            double duration = playable.GetDuration();
            
            // 只有在 Clip 播放期间才激活
            isActive = time >= 0 && time < duration && info.weight > 0f;
            
            // 检测状态变化（用于调试或事件触发）
            if (isActive && !_wasActive)
            {
                OnHitboxActivated(playerData);
            }
            else if (!isActive && _wasActive)
            {
                OnHitboxDeactivated(playerData);
            }
            
            _wasActive = isActive;
        }

        private void OnHitboxActivated(object playerData)
        {
            // 状态变化标记，实际判定在 Mixer 中执行
        }

        private void OnHitboxDeactivated(object playerData)
        {
            // 状态变化标记
        }
    }
}
