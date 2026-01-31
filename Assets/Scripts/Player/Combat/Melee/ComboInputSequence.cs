using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat.Melee
{
    public enum ComboInputMatchMode
    {
        ExactOrder,
        AllowExtraInputs
    }

    [CreateAssetMenu(menuName = "Player/Combat/Combo/Input Sequence")]
    public class ComboInputSequence : ScriptableObject
    {
        public ComboInputMatchMode matchMode = ComboInputMatchMode.AllowExtraInputs;

        [Min(0f)]
        public float maxTotalDuration = 0.4f;

        public List<ComboInputAtom> steps = new List<ComboInputAtom>();

        // 尝试用玩家最近的输入（bufferEvents）去匹配预设的连招步骤（steps）。
        // now             ：当前时间，用来判断按键是否过期。
        // consumePrefixCount：如果匹配成功，需要「吃掉」多少条最旧的输入（包括匹配到的最后一个事件）。
        // 返回值为 true 表示匹配成功，false 表示失败。
        public bool TryMatch(IReadOnlyList<ComboInputEvent> bufferEvents, float now, out int consumePrefixCount)
        {
            consumePrefixCount = 0;
            // 如果连招（steps）或者玩家输入（bufferEvents）是空的，那肯定匹配不上，直接返回失败。
            if (steps == null || steps.Count == 0) return false;
            if (bufferEvents == null || bufferEvents.Count == 0) return false;

            // 我们的 bufferEvents 是按时间从旧到新排列的（最新的输入在最后面）。
            // 为了实现类似《怪物猎人》那样的“先行输入”手感，我们从后往前匹配。
            // 也就是说，我们拿连招的最后一步，去和玩家最新的输入进行比较。
            int stepIndex = steps.Count - 1; // 指向连招步骤的指针，从最后一步开始。
            int eIndex = bufferEvents.Count - 1; // 指向玩家输入的指针，从最新的输入开始。

            float lastMatchTime = -1f; // 记录连招中，最后一个匹配上的按键的时间
            float firstMatchTime = -1f; // 记录连招中，第一个匹配上的按键的时间

            int earliestMatchedEventIndex = -1; // 记录匹配上的最“老”的那个输入事件在 bufferEvents 里的位置

            while (stepIndex >= 0 && eIndex >= 0)
            {
                var atom = steps[stepIndex];
                var e = bufferEvents[eIndex];

                // 每个步骤可以设置一个“最大允许延迟”（maxAgeOverride）——
                // 如果玩家按键距离现在的时间差超过这个值，就认为太晚了，直接跳过这个输入。
                float maxAge = atom != null && atom.maxAgeOverride > 0f ? atom.maxAgeOverride : 0f;
                if (maxAge > 0f && now - e.time > maxAge)
                {
                    // 这个输入已经过期，不可能匹配，尝试更老一条输入。
                    eIndex--;
                    continue;
                }

                // 如果当前输入（e）跟当前需要匹配的步骤（atom）完全一致，算作匹配成功。
                if (atom != null && e.type == atom.type && e.source == atom.source)
                {
                    // 记录连招中最晚的匹配时间（只赋值一次）。
                    if (lastMatchTime < 0f) lastMatchTime = e.time;
                    // 不断更新最早的匹配时间。
                    firstMatchTime = e.time;
                    // 记录最老的匹配输入的位置，后面用来计算 consumePrefixCount。
                    earliestMatchedEventIndex = eIndex;
                    // 成功匹配一个步骤，连招和输入指针都往前走。
                    stepIndex--;
                    eIndex--;
                    continue;
                }

                // 如果到这里，说明当前输入和连招步骤不匹配。
                // 检查匹配模式：
                if (matchMode == ComboInputMatchMode.ExactOrder)
                {
                    // 如果是“精确匹配”模式，那么任何一个不匹配的输入都会导致整个连招失败。
                    return false;
                }

                // 如果是“允许额外输入”模式（AllowExtraInputs），我们可以跳过这个不相关的输入，
                // 继续用更老的输入去匹配当前的连招步骤。
                eIndex--;
            }

            // 循环结束后，如果还有剩余的连招步骤没被匹配上，说明匹配失败。
            if (stepIndex >= 0) return false;

            // 检查整个连招的持续时间。
            // 如果从第一个匹配上的按键到最后一个匹配上的按键，总耗时超过了预设的 `maxTotalDuration`，
            // 那么也算作匹配失败。这可以防止玩家按键太慢。
            if (maxTotalDuration > 0f && firstMatchTime >= 0f && lastMatchTime >= 0f)
            {
                if (lastMatchTime - firstMatchTime > maxTotalDuration)
                    return false;
            }

            // 恭喜！所有检查都通过了，连招匹配成功！
            // 现在我们需要告诉系统，这次成功的连招匹配消耗了输入缓冲区中的哪些事件。
            // 我们需要消耗掉从开头到“最早匹配上的那个事件”之间的所有事件（包括它自己）。
            // 这样可以防止这些旧的输入被用于下一次的连招匹配。
            consumePrefixCount = earliestMatchedEventIndex + 1;
            return true;
        }
    }
}
