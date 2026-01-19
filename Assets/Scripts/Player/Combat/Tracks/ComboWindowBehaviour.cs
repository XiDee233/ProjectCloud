using UnityEngine;
using UnityEngine.Playables;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class ComboWindowBehaviour : PlayableBehaviour
    {
        [Header("窗口状态")]
        public bool isWindowOpen = false;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // 根据 Playable 的时间判断窗口是否开启
            double time = playable.GetTime();
            double duration = playable.GetDuration();
            
            // 只有在 Clip 播放期间且权重 > 0 时才开启窗口
            isWindowOpen = time >= 0 && time < duration && info.weight > 0f;
        }
    }
}
