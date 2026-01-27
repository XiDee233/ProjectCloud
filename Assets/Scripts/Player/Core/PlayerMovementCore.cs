using UnityEngine;

namespace Player.Core
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Player/Core/Player Movement Core")]
    public class PlayerMovementCore : MonoBehaviour
    {
        public static PlayerMovementCore Instance { get; private set; }

        [Header("移动")]
        [SerializeField, Min(0.1f)] private float maxMoveSpeed = 8f;
        [SerializeField, Min(0f)] private float accelerationTime = 0.2f;
        [SerializeField, Min(0f)] private float decelerationTime = 0.15f;

        [Header("旋转")]
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.5f;

        [Header("地面检测")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.05f)] private float groundCheckDistance = 0.2f;

        [Header("重力")]
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _controller;
        private Camera _mainCamera;

        public event System.Action OnLanded;
        public LayerMask GroundMask => groundMask;
        public float MaxMoveSpeed => maxMoveSpeed;

        private void Awake()
        {
            Instance = this;
            _controller = GetComponent<CharacterController>();
            _mainCamera = Camera.main;

            if (!_controller)
                Debug.LogError("PlayerMovementCore requires a CharacterController.", this);
        }

        public MovementCoreData CreateDefaultMovementData()
        {
            return new MovementCoreData
            {
                position = transform.position,
                velocity = Vector3.zero,
                dashVelocity = Vector3.zero,
                currentSpeed = 0f,
                rotation = transform.rotation.eulerAngles.y,
                rotationVelocity = 0f,
                isGrounded = _controller.isGrounded,
                movementLocked = false,
                rotationLocked = false,
                rotationOverridden = false,
                rotationOverrideAngle = 0f
            };
        }

        public void ApplyGravity(ref MovementCoreData data, float delta)
        {
            if (!data.isGrounded)
            {
                data.velocity.y += gravity * delta;
                _controller.Move(new Vector3(0f, data.velocity.y * delta, 0f));
            }
            else if (data.velocity.y < 0f)
            {
                data.velocity.y = -2f;
            }
        }

        public void ApplyAcceleratedMovement(ref MovementCoreData data, Vector3 direction, float delta)
        {
            if (data.movementLocked) direction = Vector3.zero;

            float targetSpeed = direction.sqrMagnitude > 0f ? maxMoveSpeed : 0f;
            float deltaSpeed = direction.sqrMagnitude > 0f ? accelerationTime : decelerationTime;

            data.currentSpeed = Mathf.MoveTowards(data.currentSpeed, targetSpeed, (maxMoveSpeed / Mathf.Max(deltaSpeed, 0.001f)) * delta);
            
            // 计算水平速度并存入 velocity（保留 y 分量用于重力）
            Vector3 horizontalVelocity = direction.normalized * data.currentSpeed + data.dashVelocity;
            data.velocity.x = horizontalVelocity.x;
            data.velocity.z = horizontalVelocity.z;
            
            Vector3 totalMovement = horizontalVelocity * delta;
            _controller.Move(totalMovement);
        }

        public void ApplyRotation(ref MovementCoreData data, float targetAngle, float delta)
        {
            if (data.rotationLocked || float.IsNaN(targetAngle)) return;
            if (data.rotationOverridden) targetAngle = data.rotationOverrideAngle;

            data.rotation = Mathf.LerpAngle(data.rotation, targetAngle, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, data.rotation, 0f);
        }

        public void UpdateGroundedState(ref MovementCoreData data)
        {
            bool wasGrounded = data.isGrounded;
            data.isGrounded = _controller.isGrounded;

            if (data.isGrounded)
            {
                Vector3 origin = transform.position + Vector3.up * 0.1f;
                data.isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            }

            if (data.isGrounded && !wasGrounded)
            {
                data.velocity.y = -2f;
                OnLanded?.Invoke();
            }
        }

        public void FinalizeMovement(ref MovementCoreData data)
        {
            data.position = transform.position;
        }

        public Vector3 CalculateCameraRelativeMovement(Vector2 inputDirection)
        {
            if (!_mainCamera) return new Vector3(inputDirection.x, 0f, inputDirection.y);
            Vector3 forward = _mainCamera.transform.forward;
            Vector3 right = _mainCamera.transform.right;
            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();
            return right * inputDirection.x + forward * inputDirection.y;
        }

        public float CalculateAimAngle(Vector3 aimDirection)
        {
            if (aimDirection.sqrMagnitude > 0.01f)
            {
                return Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;
            }
            return float.NaN;
        }

        public void SetMovementLocked(ref MovementCoreData data, bool locked) => data.movementLocked = locked;
        public void SetRotationLocked(ref MovementCoreData data, bool locked) => data.rotationLocked = locked;
        public void OverrideRotation(ref MovementCoreData data, float angle)
        {
            data.rotationOverridden = true;
            data.rotationOverrideAngle = angle;
        }
        public void ClearRotationOverride(ref MovementCoreData data) => data.rotationOverridden = false;
        public void SetDashVelocity(ref MovementCoreData data, Vector3 velocity) => data.dashVelocity = velocity;

        public void ApplyRawMovement(Vector3 delta)
        {
            if (delta.sqrMagnitude > 0)
            {
                _controller.Move(delta);
            }
        }

        // 核心修复：添加 OnAnimatorMove 回调来拦截 Unity 自动应用根运动的行为
        // 当脚本中存在此方法时，Unity 不会再自动根据 Animator 的位移修改 Transform
        // 这样我们可以安全地开启 applyRootMotion（防止回原点），同时在 MeleeStateNode 中手动通过预测系统应用位移（防止超速）
        private void OnAnimatorMove()
        {
            // 故意留空，拦截自动位移
        }
    }

    public struct MovementCoreData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 dashVelocity;
        public float currentSpeed;
        public float rotation;
        public float rotationVelocity;
        public bool isGrounded;
        public bool movementLocked;
        public bool rotationLocked;
        public bool rotationOverridden;
        public float rotationOverrideAngle;
    }
}
