namespace HimeBot.App.Observation.Mission;

/// <summary>
/// 一次任务面板返回按钮观察的结果。
/// </summary>
/// <param name="IsSuccess">截图和模板匹配是否成功执行。</param>
/// <param name="IsVisible">是否已经确认返回按钮可见。</param>
/// <param name="Message">观察说明或失败原因。</param>
/// <param name="ScreenshotPath">本次观察使用的截图路径。</param>
public sealed record MissionBackButtonObservationResult(
    bool IsSuccess,
    bool IsVisible,
    string Message,
    string? ScreenshotPath);
