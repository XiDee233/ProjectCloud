using UnityEngine;
using System.Collections.Generic;

namespace Player.Animation
{
    public static class AnimationStates

    {
        // 基础层动画状态
        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Run = Animator.StringToHash("Run");
        public static readonly int Dash = Animator.StringToHash("Dash");
        public static readonly int Locomotion = Animator.StringToHash("Locomotion"); // 8方向 Blend Tree 状态

        // Blend Tree 参数名（用于 2D 方向混合）
        public const string PARAM_MOVE_X = "MoveX";     // 左右移动分量 (-1 到 1)
        public const string PARAM_MOVE_Y = "MoveY";     // 前后移动分量 (-1 到 1)
        public const string PARAM_SPEED = "Speed";      // 移动速度

        // 动画层索引
        public const int BASE_LAYER = 0;
        public const int UPPER_BODY_LAYER = 1;

        // 缓存哈希
        private static readonly Dictionary<string, int> _hashCache = new Dictionary<string, int>();

        public static int GetHash(string stateName)
        {
            if (!_hashCache.TryGetValue(stateName, out int hash))
            {
                hash = Animator.StringToHash(stateName);
                _hashCache[stateName] = hash;
            }
            return hash;
        }
    }
}
