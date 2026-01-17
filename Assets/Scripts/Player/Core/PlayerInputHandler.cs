using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Core
{
    public struct PlayerInput
    {
        public Vector2 movement;
        public Vector3 aimWorldDirection;
        public bool leftAttack;
        public bool rightAttack;

        public void Dispose() { }
    }

    /// <summary>
    /// 直接从 Unity Input System 获取键盘/鼠标/手柄信息，向 <see cref="ControlAuthority"/> 提供简化的输入数据。
    /// 引入了 Capcom/Nintendo 风格的输入缓冲 (Input Buffering) 机制。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInputHandler : MonoBehaviour, IInputProvider
    {
        [Header("Aim Settings")]
        [SerializeField, Range(0f, 1f)] private float gamepadAimThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float movementDeadzone = 0.1f;

        [Header("Buffer Settings")]
        [SerializeField] private float bufferWindow = 0.15f; // 预输入缓冲窗口

        public PlayerInput currentInput;

        // 时间戳记录：记录每个动作最后一次按下和释放的时间
        private float _lastPrimaryAttackPressTime = -1f;
        private float _lastPrimaryAttackReleaseTime = -1f;
        private float _lastSecondaryAttackPressTime = -1f;
        private float _lastSecondaryAttackReleaseTime = -1f;
        private float _lastDashPressTime = -1f;
        private float _lastDashReleaseTime = -1f;
        private float _lastGrapplePressTime = -1f;
        private float _lastGrappleReleaseTime = -1f;

        private void Update()
        {
            ApplyKeyboardMouseInput();
            ApplyGamepadInput();
        }

        private void ApplyKeyboardMouseInput()
        {
            var kbd = Keyboard.current;
            var mouse = Mouse.current;
            if (kbd == null || mouse == null)
                return;

            Vector2 move = Vector2.zero;
            if (kbd.wKey.isPressed) move.y += 1f;
            if (kbd.sKey.isPressed) move.y -= 1f;
            if (kbd.aKey.isPressed) move.x -= 1f;
            if (kbd.dKey.isPressed) move.x += 1f;
            currentInput.movement = move.magnitude > 1f ? move.normalized : move;

            currentInput.aimWorldDirection = CalculateMouseWorldDirection(mouse.position.ReadValue());

            // 物理状态更新
            currentInput.leftAttack = mouse.leftButton.isPressed;
            currentInput.rightAttack = mouse.rightButton.isPressed;

            // 时间戳记录 (用于缓冲)
            if (mouse.leftButton.wasPressedThisFrame) _lastPrimaryAttackPressTime = Time.unscaledTime;
            if (mouse.leftButton.wasReleasedThisFrame) _lastPrimaryAttackReleaseTime = Time.unscaledTime;

            if (mouse.rightButton.wasPressedThisFrame) _lastSecondaryAttackPressTime = Time.unscaledTime;
            if (mouse.rightButton.wasReleasedThisFrame) _lastSecondaryAttackReleaseTime = Time.unscaledTime;

            if (kbd.spaceKey.wasPressedThisFrame) _lastDashPressTime = Time.unscaledTime;
            if (kbd.spaceKey.wasReleasedThisFrame) _lastDashReleaseTime = Time.unscaledTime;

            if (kbd.eKey.wasPressedThisFrame) _lastGrapplePressTime = Time.unscaledTime;
            if (kbd.eKey.wasReleasedThisFrame) _lastGrappleReleaseTime = Time.unscaledTime;
        }

        private void ApplyGamepadInput()
        {
            var pad = Gamepad.current;
            if (pad == null)
                return;

            Vector2 moveInput = pad.leftStick.ReadValue();
            if (moveInput.magnitude > movementDeadzone)
            {
                currentInput.movement = moveInput;
            }

            Vector2 rightStick = pad.rightStick.ReadValue();
            bool hasAimInput = rightStick.sqrMagnitude > gamepadAimThreshold * gamepadAimThreshold;
            currentInput.aimWorldDirection = hasAimInput ? GetGamepadAimDirection(rightStick) : Vector3.zero;

            // 物理状态更新
            bool leftPressed = pad.rightTrigger.ReadValue() > 0.5f;
            bool rightPressed = pad.leftTrigger.ReadValue() > 0.5f;

            if (leftPressed && !currentInput.leftAttack) _lastPrimaryAttackPressTime = Time.unscaledTime;
            if (!leftPressed && currentInput.leftAttack) _lastPrimaryAttackReleaseTime = Time.unscaledTime;
            currentInput.leftAttack = leftPressed;

            if (rightPressed && !currentInput.rightAttack) _lastSecondaryAttackPressTime = Time.unscaledTime;
            if (!rightPressed && currentInput.rightAttack) _lastSecondaryAttackReleaseTime = Time.unscaledTime;
            currentInput.rightAttack = rightPressed;

            if (pad.buttonSouth.wasPressedThisFrame) _lastDashPressTime = Time.unscaledTime;
            if (pad.buttonSouth.wasReleasedThisFrame) _lastDashReleaseTime = Time.unscaledTime;

            if (pad.buttonWest.wasPressedThisFrame) _lastGrapplePressTime = Time.unscaledTime;
            if (pad.buttonWest.wasReleasedThisFrame) _lastGrappleReleaseTime = Time.unscaledTime;
        }

        private Vector3 GetGamepadAimDirection(Vector2 stickInput)
        {
            Camera cam = Camera.main;
            Vector3 aimDirection = new Vector3(stickInput.x, 0f, stickInput.y);
            if (cam == null)
                return aimDirection.normalized;

            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 worldDirection = right * stickInput.x + forward * stickInput.y;
            return worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : forward;
        }

        private Vector3 CalculateMouseWorldDirection(Vector2 mouseScreenPos)
        {
            Camera cam = Camera.main;
            if (cam == null || PlayerMovementCore.Instance == null) return Vector3.forward;

            Ray ray = cam.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, PlayerMovementCore.Instance.GroundMask))
            {
                Vector3 dir = hit.point - transform.position;
                dir.y = 0f;
                return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            }
            return transform.forward;
        }

        // IInputProvider 实现
        public Vector2 Movement => currentInput.movement;
        public Vector3 AimWorldDirection => currentInput.aimWorldDirection;

        public InputButtonState PrimaryAttack => GetState(currentInput.leftAttack, _lastPrimaryAttackPressTime, _lastPrimaryAttackReleaseTime);
        public InputButtonState SecondaryAttack => GetState(currentInput.rightAttack, _lastSecondaryAttackPressTime, _lastSecondaryAttackReleaseTime);
        public InputButtonState Dash => GetState(false, _lastDashPressTime, _lastDashReleaseTime);
        public InputButtonState Grapple => GetState(false, _lastGrapplePressTime, _lastGrappleReleaseTime);

        public bool IsActive => true;

        private InputButtonState GetState(bool isPressed, float lastPress, float lastRelease)
        {
            return new InputButtonState
            {
                isPressed = isPressed,
                wasPressed = lastPress > 0 && (Time.unscaledTime - lastPress) <= bufferWindow,
                wasReleased = lastRelease > 0 && (Time.unscaledTime - lastRelease) <= bufferWindow
            };
        }

        public void ConsumeInput(InputActionType actionType)
        {
            switch (actionType)
            {
                case InputActionType.PrimaryAttack: _lastPrimaryAttackPressTime = -1f; break;
                case InputActionType.SecondaryAttack: _lastSecondaryAttackPressTime = -1f; break;
                case InputActionType.Dash: _lastDashPressTime = -1f; break;
                case InputActionType.Grapple: _lastGrapplePressTime = -1f; break;
            }
        }
    }
}
