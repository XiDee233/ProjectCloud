using UnityEngine;
using Player.Core;

namespace Player.Animation
{
    /// <summary>
    /// 8方向移动动画控制器
    /// 计算移动方向相对于角色朝向的本地空间分量，输出 MoveX/MoveY 参数驱动 2D Blend Tree
    /// 由状态节点主动调用 UpdateAnimation() 推送数据，而非轮询
    /// </summary>
    [RequireComponent(typeof(AnimationController))]
    [AddComponentMenu("Player/Animation/Movement Animation Controller")]
    public class MovementAnimationController : MonoBehaviour
    {
        private AnimationController animController;
        
        [Header("阈值")]
        [SerializeField] private float walkThreshold = 0.1f;

        [Header("Blend Tree 参数名")]
        [SerializeField] private string moveXParam = AnimationStates.PARAM_MOVE_X;
        [SerializeField] private string moveYParam = AnimationStates.PARAM_MOVE_Y;
        [SerializeField] private string speedParam = AnimationStates.PARAM_SPEED;

        [Header("平滑过渡")]
        [SerializeField] private float lerpSpeed = 0.2f;

        // 当前混合值
        private float currentMoveX;
        private float currentMoveY;
        private float currentSpeed;


        private void Awake()
        {
            if (!animController) animController = GetComponent<AnimationController>();
        }

        /// <summary>
        /// 计算动画参数（用于状态模拟）
        /// </summary>
        /// <param name="data">移动核心数据</param>
        /// <param name="maxMoveSpeed">最大移动速度，用于归一化 speed 参数</param>
        /// <returns>计算出的动画参数</returns>
        public (float moveX, float moveY, float speed) CalculateAnimationParameters(MovementCoreData data, float maxMoveSpeed)
        {
            // 提取水平速度
            Vector3 horizontalVelocity = new Vector3(data.velocity.x, 0f, data.velocity.z);
            float speed = horizontalVelocity.magnitude;

            // 计算目标混合值
            float targetMoveX = 0f;
            float targetMoveY = 0f;

            if (speed > walkThreshold)
            {
                // 将世界空间移动方向投影到角色本地空间
                Vector3 moveDir = horizontalVelocity.normalized;
                targetMoveX = Vector3.Dot(moveDir, transform.right);   // 左右分量
                targetMoveY = Vector3.Dot(moveDir, transform.forward); // 前后分量
            }

            // 归一化速度 (0-1)
            float normalizedSpeed = maxMoveSpeed > 0f ? Mathf.Clamp01(speed / maxMoveSpeed) : 0f;

            return (targetMoveX, targetMoveY, normalizedSpeed);
        }

        /// <summary>
        /// 应用动画参数到 Animator（用于视图渲染）
        /// </summary>
        /// <param name="moveX">MoveX 参数</param>
        /// <param name="moveY">MoveY 参数</param>
        /// <param name="speed">Speed 参数</param>
        public void ApplyAnimationParameters(float moveX, float moveY, float speed)
        {
            // 快速平滑过渡
            float t = lerpSpeed;
            currentMoveX = Mathf.Lerp(currentMoveX, moveX, t);
            currentMoveY = Mathf.Lerp(currentMoveY, moveY, t);
            currentSpeed = Mathf.Lerp(currentSpeed, speed, t);

            // 设置 Animator 参数
            animController.SetFloat(moveXParam, currentMoveX);
            animController.SetFloat(moveYParam, currentMoveY);
            animController.SetFloat(speedParam, currentSpeed);
        }

        /// <summary>
        /// 由状态节点调用，传入当前帧的移动数据来更新动画
        /// </summary>
        /// <param name="data">移动核心数据</param>
        /// <param name="maxMoveSpeed">最大移动速度，用于归一化 speed 参数</param>
        public void UpdateAnimation(MovementCoreData data, float maxMoveSpeed)
        {
            var (moveX, moveY, speed) = CalculateAnimationParameters(data, maxMoveSpeed);
            ApplyAnimationParameters(moveX, moveY, speed);
        }

        /// <summary>
        /// 获取当前本地空间移动方向（用于调试或其他系统读取）
        /// </summary>
        public Vector2 GetLocalMoveDirection() => new Vector2(currentMoveX, currentMoveY);
        
        /// <summary>
        /// 获取当前速度（用于调试或其他系统读取）
        /// </summary>
        public float GetCurrentSpeed() => currentSpeed;
    }
}
