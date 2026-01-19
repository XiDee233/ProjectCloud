using UnityEngine;
using UnityEngine.Playables;

namespace Player.Combat.Tracks
{
    public class EffectMixerBehaviour : PlayableBehaviour
    {
        // Mixer 主要负责管理多个 Effect 的生成和销毁
        // 实际生成逻辑在 EffectBehaviour 中
        // 这里可以添加额外的混合逻辑，例如优先级管理
    }
}
