using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Combat/Combo/Input Atom")]
    public class ComboInputAtom : ScriptableObject
    {
        public ComboInputEventType type;
        public ComboInputSource source;

        [Min(0f)]
        public float maxAgeOverride = 0f;
    }
}
