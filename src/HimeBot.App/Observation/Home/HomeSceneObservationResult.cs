namespace HimeBot.App.Observation.Home;

/// <summary>
/// 一次首页场景观察的结果。
/// </summary>
/// <param name="IsSuccess">
/// 截图和首页模板匹配是否成功执行。
/// </param>
/// <param name="IsHome">
/// 是否已经取得足够证据确认当前为首页。
/// </param>
/// <param name="Message">观察说明或失败原因。</param>
/// <param name="ScreenshotPath">本次观察使用的截图路径。</param>
public sealed record HomeSceneObservationResult(
    bool IsSuccess,
    bool IsHome,
    string Message,
    string? ScreenshotPath);
