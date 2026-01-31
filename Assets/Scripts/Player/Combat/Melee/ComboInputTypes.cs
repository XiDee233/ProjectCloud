using UnityEngine;

namespace Player.Combat.Melee
{
    public enum ComboInputEventType
    {
        Tap,
        Hold,
        Release
    }

    public enum ComboInputSource
    {
        PrimaryAttack,
        SecondaryAttack,
        Dash,
        Grapple
    }

    public struct ComboInputEvent
    {
        public ComboInputEventType type;
        public ComboInputSource source;
        public float time;
        public Vector2 move;

        public ComboInputEvent(ComboInputEventType type, ComboInputSource source, float time, Vector2 move)
        {
            this.type = type;
            this.source = source;
            this.time = time;
            this.move = move;
        }
    }
}
