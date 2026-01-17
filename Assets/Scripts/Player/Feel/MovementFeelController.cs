using UnityEngine;
using System.Collections.Generic;
using MoreMountains.Feedbacks;

namespace Player.Feel
{
    [AddComponentMenu("Player/Feel/Movement Feel Controller")]
    public class MovementFeelController : MonoBehaviour
    {
        private List<IFeelModule> _modules = new List<IFeelModule>();

        private void Awake()
        {
            // 自动寻找并注册挂载在当前物体上的所有模块
            var foundModules = GetComponents<IFeelModule>();
            foreach (var module in foundModules)
            {
                RegisterModule(module);
            }
        }

        private void Update()
        {
            // 驱动所有已注册模块的更新逻辑
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].TickModule();
            }
        }

        /// <summary>
        /// 动态注册一个新的Feel模块
        /// </summary>
        public void RegisterModule(IFeelModule module)
        {
            if (module != null && !_modules.Contains(module))
            {
                _modules.Add(module);
                module.OnRegistered(this);
            }
        }

        /// <summary>
        /// 注销一个Feel模块
        /// </summary>
        public void UnregisterModule(IFeelModule module)
        {
            if (module != null && _modules.Contains(module))
            {
                _modules.Remove(module);
                module.OnUnregistered(this);
            }
        }

        /// <summary>
        /// 提供给模块使用的统一反馈执行接口（可选，方便集中管理）
        /// </summary>
        public void PlayFeedback(MMFeedbacks feedbacks)
        {
            if (feedbacks != null)
            {
                feedbacks.PlayFeedbacks();
            }
        }
    }
}
