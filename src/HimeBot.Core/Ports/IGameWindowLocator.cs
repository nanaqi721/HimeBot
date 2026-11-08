using HimeBot.Core.Windowing;

namespace HimeBot.Core.Ports;

/// <summary>
/// 游戏窗口定位器接口
/// </summary>
public interface IGameWindowLocator
{
    /// <summary>
    /// 根据进程名称查找游戏窗口
    /// </summary>
    /// <param name="processName">进程名称</param>
    /// <returns>窗口查找结果</returns>
    GameWindowLookupResult FindByProcessName(
        string processName);

}
