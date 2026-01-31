using UnityEngine;

namespace Player.Combat.Melee
{
    [CreateAssetMenu(menuName = "Player/Combat/Combo/Transition")]
    public class ComboTransition : ScriptableObject
    {
        public ComboInputSequence input;
        public ComboConditionType condition = ComboConditionType.None;

        [Min(0)]
        public int priority = 0;

        public ComboNode toNode;
    }
}
