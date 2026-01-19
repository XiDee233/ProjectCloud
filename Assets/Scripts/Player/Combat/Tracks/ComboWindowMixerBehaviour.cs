using UnityEngine;
using UnityEngine.Playables;
using Player.Combat;

namespace Player.Combat.Tracks
{
    public class ComboWindowMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // 计算窗口状态
            bool isWindowOpen = false;
            
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight > 0f)
                {
                    ScriptPlayable<ComboWindowBehaviour> inputPlayable = (ScriptPlayable<ComboWindowBehaviour>)playable.GetInput(i);
                    ComboWindowBehaviour behaviour = inputPlayable.GetBehaviour();
                    if (behaviour.isWindowOpen)
                    {
                        isWindowOpen = true;
                        break;
                    }
                }
            }
            
            // 通过接口更新状态（playerData 是通过 TrackBindingType 绑定的 IComboWindowStateProvider）
            if (playerData is IComboWindowStateProvider provider)
            {
                provider.SetComboWindowState(isWindowOpen);
            }
        }
    }
}
