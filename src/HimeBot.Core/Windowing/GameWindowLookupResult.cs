
namespace HimeBot.Core.Windowing;

/// <summary>
/// 游戏窗口查找结果
/// </summary>
/// <param name="IsSuccess">成功标识</param>
/// <param name="Message">窗口查找结果说明</param>
/// <param name="Window">找到的窗口信息</param>
public sealed record GameWindowLookupResult(
    bool IsSuccess,
    string Message,
    GameWindowInfo? Window);


