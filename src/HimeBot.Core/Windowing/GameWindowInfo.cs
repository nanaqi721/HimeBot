
namespace HimeBot.Core.Windowing;

/// <summary>
/// 游戏窗口信息
/// </summary>
/// <param name="ProcessId">进程 ID</param>
/// <param name="Title">窗口标题</param>
/// <param name="ClientWidth">客户区宽度</param>
/// <param name="ClientHeight">客户区高度</param>
public sealed record GameWindowInfo(
    int ProcessId,
    string Title,
    int ClientWidth,
    int ClientHeight);
