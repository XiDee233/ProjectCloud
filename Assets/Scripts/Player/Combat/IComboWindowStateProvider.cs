namespace Player.Combat
{
    /// <summary>
    /// 连招窗口状态提供者接口
    /// 用于 ComboWindowTrack 通过 TrackBindingType 绑定，在 ProcessFrame 中更新窗口状态
    /// </summary>
    public interface IComboWindowStateProvider
    {
        /// <summary>
        /// 设置连招窗口状态
        /// </summary>
        /// <param name="isOpen">窗口是否开启</param>
        void SetComboWindowState(bool isOpen);
    }
}
