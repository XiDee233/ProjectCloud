using UnityEngine;
using MoreMountains.Feedbacks;
using Player.Core;

namespace Player.Feel
{
    /// <summary>
    /// 核心移动反馈模块。
    /// 负责处理落地、脚步声、速度提升等基础移动相关的Feel。
    /// </summary>
    [AddComponentMenu("Player/Feel/Core Movement Feel")]
    public class CoreMovementFeel : MonoBehaviour, IFeelModule
    {
        [Header("反馈设置")]
        [SerializeField] private MMFeedbacks landingFeedbacks;
        [SerializeField] private MMFeedbacks footstepFeedbacks;
        [SerializeField] private MMFeedbacks speedBoostFeedbacks;

        [Header("脚步声设置")]
        [SerializeField] private float footstepInterval = 0.3f;
        [SerializeField] private float minFootstepSpeed = 0.5f;
        [SerializeField] private float maxFootstepSpeed = 5f;

        [Header("速度反馈设置")]
        [SerializeField] private float speedBoostThreshold = 6f;
        [SerializeField] private float speedBoostCooldown = 0.5f;

        private PlayerMovementCore _movementCore;
        private MovementFeelController _feelController;

        private float _lastFootstepTime;
        private float _lastSpeedBoostTime;
        private bool _wasGrounded = true;

        #region IFeelModule Implementation

        public void OnRegistered(MovementFeelController controller)
        {
            //_feelController = controller;
            //_movementCore = GetComponent<PlayerMovementCore>();

            //if (_movementCore != null)
            //{
            //    _movementCore.OnLanded += OnPlayerLanded;
            //}
        }

        public void OnUnregistered(MovementFeelController controller)
        {
            //if (_movementCore != null)
            //{
            //    _movementCore.OnLanded -= OnPlayerLanded;
            //}
            //_feelController = null;
        }

        public void TickModule()
        {
            //if (_movementCore == null) return;

            //CheckGroundedState();
            //CheckFootsteps();
            //CheckSpeedBoost();
        }

        #endregion

    //    private void CheckGroundedState()
    //    {
    //        bool isGrounded = _movementCore.IsGrounded;

    //        // 检测从空中落地 (除了事件，也可以在这里做状态轮询)
    //        if (isGrounded && !_wasGrounded)
    //        {
    //            // OnPlayerLanded() 已通过事件触发，此处仅更新状态
    //        }

    //        _wasGrounded = isGrounded;
    //    }

    //    private void CheckFootsteps()
    //    {
    //        if (!_movementCore.IsGrounded)
    //            return;

    //        float currentSpeed = _movementCore.CurrentSpeed;

    //        if (currentSpeed < minFootstepSpeed)
    //            return;

    //        float speedRatio = Mathf.InverseLerp(minFootstepSpeed, maxFootstepSpeed, currentSpeed);
    //        float currentInterval = Mathf.Lerp(footstepInterval * 2f, footstepInterval * 0.5f, speedRatio);

    //        if (Time.time - _lastFootstepTime >= currentInterval)
    //        {
    //            PlayFeedback(footstepFeedbacks);
    //            _lastFootstepTime = Time.time;
    //        }
    //    }

    //    private void CheckSpeedBoost()
    //    {
    //        float currentSpeed = _movementCore.CurrentSpeed;

    //        if (currentSpeed >= speedBoostThreshold && Time.time - _lastSpeedBoostTime >= speedBoostCooldown)
    //        {
    //            PlayFeedback(speedBoostFeedbacks);
    //            _lastSpeedBoostTime = Time.time;
    //        }
    //    }

    //    private void OnPlayerLanded()
    //    {
    //        PlayFeedback(landingFeedbacks);
    //    }

    //    private void PlayFeedback(MMFeedbacks feedbacks)
    //    {
    //        if (_feelController != null)
    //            _feelController.PlayFeedback(feedbacks);
    //        else if (feedbacks != null)
    //            feedbacks.PlayFeedbacks();
    //    }
     }
}
