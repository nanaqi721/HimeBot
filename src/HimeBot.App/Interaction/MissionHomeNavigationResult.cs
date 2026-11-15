namespace HimeBot.App.Interaction;

/// <summary>
/// 尝试从任务面板返回首页的结果。
/// </summary>
/// <param name="IsSuccess">
/// 是否确认任务面板、确认返回按钮、成功点击并最终观察到首页。
/// </param>
/// <param name="Message">操作结果或失败原因。</param>
/// <param name="ScreenshotPath">最近一次观察使用的截图路径。</param>
public sealed record MissionHomeNavigationResult(
    bool IsSuccess,
    string Message,
    string? ScreenshotPath);
