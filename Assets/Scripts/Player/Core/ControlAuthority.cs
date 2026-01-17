using UnityEngine;

namespace Player.Core
{
    [AddComponentMenu("Player/Core/Control Authority")]
    public class ControlAuthority : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour defaultProvider;
        public IInputProvider CurrentProvider { get; private set; }

        public event System.Action<IInputProvider> OnProviderChanged;

        private void Awake()
        {
            if (defaultProvider is IInputProvider provider)
            {
                SetProvider(provider);
            }
        }

        public void SetProvider(IInputProvider provider)
        {
            if (provider == null || provider == CurrentProvider)
                return;

            CurrentProvider = provider;
            if (OnProviderChanged != null)
                OnProviderChanged(provider);
        }

        public void ReleaseProvider()
        {
            CurrentProvider = null;
            if (OnProviderChanged != null)
                OnProviderChanged(null);
        }
    }
}
