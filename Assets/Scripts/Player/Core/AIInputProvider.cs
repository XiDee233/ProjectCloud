using UnityEngine;

namespace Player.Core
{
    [AddComponentMenu("Player/Core/AI Input Provider")]
    public class AIInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private Vector2 movement;
        [SerializeField] private bool primaryAttack;
        [SerializeField] private bool secondaryAttack;
        [SerializeField] private bool dash;
        [SerializeField] private bool grapple;
        [SerializeField] private Vector3 aimWorldDirection;

        public Vector2 Movement => movement;
        public Vector3 AimWorldDirection => aimWorldDirection;

        public InputButtonState PrimaryAttack => new InputButtonState { isPressed = primaryAttack, wasPressed = primaryAttack };
        public InputButtonState SecondaryAttack => new InputButtonState { isPressed = secondaryAttack, wasPressed = secondaryAttack };
        public InputButtonState Dash => new InputButtonState { isPressed = dash, wasPressed = dash };
        public InputButtonState Grapple => new InputButtonState { isPressed = grapple, wasPressed = grapple };

        public bool IsActive => enabled;

        public void SetMovement(Vector2 move) => movement = move;
        public void SetAimWorld(Vector3 direction)
        {
            aimWorldDirection = direction;
        }
        public void SetPrimaryAttack(bool value) => primaryAttack = value;
        public void SetSecondaryAttack(bool value) => secondaryAttack = value;
        public void SetDash(bool value) => dash = value;
        public void SetGrapple(bool value) => grapple = value;

        public void ConsumeInput(InputActionType actionType)
        {
            switch (actionType)
            {
                case InputActionType.PrimaryAttack: primaryAttack = false; break;
                case InputActionType.SecondaryAttack: secondaryAttack = false; break;
                case InputActionType.Dash: dash = false; break;
                case InputActionType.Grapple: grapple = false; break;
            }
        }
    }
}
