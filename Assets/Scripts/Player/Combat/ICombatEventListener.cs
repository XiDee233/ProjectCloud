namespace Player.Combat
{
    public interface ICombatEventListener
    {
        void OnCombatEvent(string eventName, float floatParam, string stringParam);
    }
}
