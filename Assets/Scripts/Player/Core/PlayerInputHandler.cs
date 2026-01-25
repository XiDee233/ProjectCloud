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

    public enum InputSource
    {
        KBM,
        Gamepad
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
        public InputSource currentSource = InputSource.KBM;
        public event System.Action<InputSource> OnInputSourceChanged;

        // 时间戳记录：记录每个动作最后一次按下和释放的时间
        private float _lastPrimaryAttackPressTime = -1f;
        private float _lastPrimaryAttackReleaseTime = -1f;
        private float _lastSecondaryAttackPressTime = -1f;
        private float _lastSecondaryAttackReleaseTime = -1f;
        private float _lastDashPressTime = -1f;
        private float _lastDashReleaseTime = -1f;
        private float _lastGrapplePressTime = -1f;
        private float _lastGrappleReleaseTime = -1f;

        private Vector2 _lastMousePosition;
        private const float MouseMoveThreshold = 1f; // 鼠标移动阈值，防止微小抖动干扰

        private void Start()
        {
            if (Mouse.current != null)
            {
                _lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        private void Update()
        {
            // 每一帧开始时重置瞬时输入状态，确保在没有任何输入时停止动作
            currentInput.movement = Vector2.zero;
            currentInput.leftAttack = false;
            currentInput.rightAttack = false;

            ApplyKeyboardMouseInput();
            ApplyGamepadInput();
        }

        private void SetInputSource(InputSource newSource)
        {
            if (currentSource != newSource)
            {
                currentSource = newSource;
                OnInputSourceChanged?.Invoke(newSource);
                // Debug.Log($"Input Source Switched to: {newSource}");
            }
        }

        private void ApplyKeyboardMouseInput()
        {
            var kbd = Keyboard.current;
            var mouse = Mouse.current;
            if (kbd == null || mouse == null)
                return;

            bool kbmActive = false;

            // 1. 移动处理：键盘输入
            Vector2 move = Vector2.zero;
            bool hasKbdMove = false;
            if (kbd.wKey.isPressed) { move.y += 1f; hasKbdMove = true; }
            if (kbd.sKey.isPressed) { move.y -= 1f; hasKbdMove = true; }
            if (kbd.aKey.isPressed) { move.x -= 1f; hasKbdMove = true; }
            if (kbd.dKey.isPressed) { move.x += 1f; hasKbdMove = true; }

            if (hasKbdMove)
            {
                currentInput.movement = move.magnitude > 1f ? move.normalized : move;
                kbmActive = true;
            }

            // 2. 瞄准处理：只有当鼠标移动或点击时才更新
            Vector2 currentMousePos = mouse.position.ReadValue();
            float mouseDelta = (currentMousePos - _lastMousePosition).sqrMagnitude;
            bool mouseMoved = mouseDelta > MouseMoveThreshold * MouseMoveThreshold;
            bool mouseClicked = mouse.leftButton.isPressed || mouse.rightButton.isPressed;

            if (mouseMoved || mouseClicked)
            {
                currentInput.aimWorldDirection = CalculateMouseWorldDirection(currentMousePos);
                _lastMousePosition = currentMousePos;
                kbmActive = true;
            }

            // 3. 动作键物理状态
            if (mouse.leftButton.isPressed) { currentInput.leftAttack = true; kbmActive = true; }
            if (mouse.rightButton.isPressed) { currentInput.rightAttack = true; kbmActive = true; }

            // 检查键盘动作键 (空格和E)
            if (kbd.spaceKey.wasPressedThisFrame || kbd.eKey.wasPressedThisFrame) kbmActive = true;

            if (kbmActive) SetInputSource(InputSource.KBM);

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

            bool padActive = false;

            // 1. 移动处理：手柄左摇杆
            Vector2 moveInput = pad.leftStick.ReadValue();
            if (moveInput.magnitude > movementDeadzone)
            {
                currentInput.movement = moveInput;
                padActive = true;
            }

            // 2. 瞄准处理：手柄右摇杆
            Vector2 rightStick = pad.rightStick.ReadValue();
            bool hasAimInput = rightStick.sqrMagnitude > gamepadAimThreshold * gamepadAimThreshold;
            if (hasAimInput)
            {
                currentInput.aimWorldDirection = GetGamepadAimDirection(rightStick);
                padActive = true;
            }

            // 3. 动作键物理状态
            bool leftPressed = pad.rightTrigger.ReadValue() > 0.5f;
            bool rightPressed = pad.leftTrigger.ReadValue() > 0.5f;

            if (leftPressed || rightPressed) padActive = true;
            if (pad.buttonSouth.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed || pad.buttonEast.isPressed) padActive = true;

            if (padActive) SetInputSource(InputSource.Gamepad);

            // 记录按下/释放时间戳 (用于缓冲)
            if (pad.rightTrigger.wasPressedThisFrame) _lastPrimaryAttackPressTime = Time.unscaledTime;
            if (pad.rightTrigger.wasReleasedThisFrame) _lastPrimaryAttackReleaseTime = Time.unscaledTime;

            if (pad.leftTrigger.wasPressedThisFrame) _lastSecondaryAttackPressTime = Time.unscaledTime;
            if (pad.leftTrigger.wasReleasedThisFrame) _lastSecondaryAttackReleaseTime = Time.unscaledTime;

            currentInput.leftAttack |= leftPressed;
            currentInput.rightAttack |= rightPressed;

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
