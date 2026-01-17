using UnityEngine;

namespace Player.Core
{
    /// <summary>
    /// 动作游戏级别的按钮状态结构。
    /// 包含物理按压状态、带缓冲的触发状态以及带缓冲的释放状态。
    /// </summary>
    public struct InputButtonState
    {
        public bool isPressed;      // 物理按住状态：用于蓄力 (Hold)
        public bool wasPressed;     // 缓冲期内是否有按下动作：用于触发 (Tap/Action)
        public bool wasReleased;    // 缓冲期内是否有释放动作：用于释放 (Release Action)

        public static InputButtonState None => new InputButtonState();
    }

    public interface IInputProvider
    {
        Vector2 Movement { get; }
        Vector3 AimWorldDirection { get; }
        
        InputButtonState PrimaryAttack { get; }
        InputButtonState SecondaryAttack { get; }
        InputButtonState Dash { get; }
        InputButtonState Grapple { get; }
        
        bool IsActive { get; }

        /// <summary>
        /// 消费特定动作的缓冲输入，防止重复触发。
        /// </summary>
        void ConsumeInput(InputActionType actionType);
    }

    public enum InputActionType
    {
        PrimaryAttack,
        SecondaryAttack,
        Dash,
        Grapple
    }
}
