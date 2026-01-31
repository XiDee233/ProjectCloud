using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Combat/Combo/Tree")]
    public class ComboTree : ScriptableObject
    {
        [Header("起手节点（移动状态匹配）")]
        public List<ComboNode> entryNodes = new List<ComboNode>();

        [Header("输入缓冲")]
        [Min(1)]
        public int bufferMaxCount = 12;

        [Min(0.01f)]
        public float bufferMaxAgeSeconds = 0.35f;

        [Header("规则")]
        public bool allowLocomotionConsume = true;
        public bool allowMeleeConsumeOnlyInWindow = true;
    }
}
