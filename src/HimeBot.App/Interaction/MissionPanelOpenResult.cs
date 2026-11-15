namespace HimeBot.App.Interaction;

/// <summary>
/// 尝试从首页点击任务入口的结果。
/// </summary>
/// <param name="IsSuccess">
/// 点击前确认首页、鼠标输入提交成功，且点击后已经观察到任务面板。
/// </param>
/// <param name="Message">操作结果或失败原因。</param>
/// <param name="ScreenshotPath">
/// 最近一次场景观察对应的截图路径。
/// </param>
public sealed record MissionPanelOpenResult(
    bool IsSuccess,
    string Message,
    string? ScreenshotPath);
