using UnityEngine;
using System;

namespace Player.Combat
{
    [RequireComponent(typeof(CombatPlayableDirector))]
    [AddComponentMenu("Player/Combat/Combat Timeline Player")]
    public class CombatTimelinePlayer : MonoBehaviour, ICombatEventListener
    {
        [SerializeField] private CombatPlayableDirector director;
        
        public event Action<string, float, string> OnCombatEventReceived;
        public event Action OnAnimationComplete;

        private void Awake()
        {
            if (!director) director = GetComponent<CombatPlayableDirector>();
        }

        public void Play(CombatTimelineData data, Action onComplete = null)
        {
            if (data == null) return;

            director.OnPlayComplete += () => {
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
            };
            director.Play(data);
        }

        public void Stop()
        {
            director.Stop();
        }

        public void OnCombatEvent(string eventName, float floatParam, string stringParam)
        {
            OnCombatEventReceived?.Invoke(eventName, floatParam, stringParam);
        }
    }
}
