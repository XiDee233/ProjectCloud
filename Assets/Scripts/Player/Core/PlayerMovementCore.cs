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
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.08f;

        [Header("地面检测")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.05f)] private float groundCheckDistance = 0.2f;

        [Header("重力")]
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _controller;
        private Camera _mainCamera;

        // 状态接力存储
        private MovementCoreData _persistentState;

        public event System.Action OnLanded;
        public LayerMask GroundMask => groundMask;

        private void Awake()
        {
            Instance = this;
            _controller = GetComponent<CharacterController>();
            _mainCamera = Camera.main;

            if (!_controller)
                Debug.LogError("PlayerMovementCore requires a CharacterController.", this);

            // 初始化持久状态
            _persistentState = new MovementCoreData
            {
                velocity = Vector3.zero,
                dashVelocity = Vector3.zero,
                currentSpeed = 0f,
                rotation = transform.rotation.eulerAngles.y,
                rotationVelocity = 0f,
                isGrounded = false,
                movementLocked = false,
                rotationLocked = false,
                rotationOverridden = false,
                rotationOverrideAngle = 0f
            };
        }

        public MovementCoreData GetPersistentState() => _persistentState;
        public void UpdatePersistentState(MovementCoreData newState) => _persistentState = newState;

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
            Vector3 totalMovement = (direction.normalized * data.currentSpeed + data.dashVelocity) * delta;
            _controller.Move(totalMovement);
        }

        public void ApplyRotation(ref MovementCoreData data, float targetAngle, float delta)
        {
            if (data.rotationLocked || float.IsNaN(targetAngle)) return;
            if (data.rotationOverridden) targetAngle = data.rotationOverrideAngle;

            data.rotation = Mathf.SmoothDampAngle(data.rotation, targetAngle, ref data.rotationVelocity, rotationSmoothTime);
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
