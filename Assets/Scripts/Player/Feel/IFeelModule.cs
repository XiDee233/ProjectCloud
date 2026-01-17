using UnityEngine;

namespace Player.Feel
{
    /// <summary>
    /// 插件式Feel模块接口。
    /// 逻辑功能组件实现此接口后，可动态注册到MovementFeelController。
    /// </summary>
    public interface IFeelModule
    {
        /// <summary>
        /// 当模块被注册到控制器时调用。用于初始化和引用获取。
        /// </summary>
        void OnRegistered(MovementFeelController controller);

        /// <summary>
        /// 当模块从控制器注销时调用。
        /// </summary>
        void OnUnregistered(MovementFeelController controller);

        /// <summary>
        /// 每帧更新逻辑。由MovementFeelController统一驱动。
        /// </summary>
        void TickModule();
    }
}
