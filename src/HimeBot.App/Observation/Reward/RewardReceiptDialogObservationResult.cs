namespace HimeBot.App.Observation.Reward;

/// <summary>
/// 一次奖励领取结果弹窗观察的结果。
/// </summary>
/// <param name="IsSuccess">
/// 截图和“閉じる”模板匹配是否成功执行。
/// </param>
/// <param name="IsVisible">
/// 是否已经取得足够证据确认弹窗可见。
/// </param>
/// <param name="Message">观察说明或失败原因。</param>
/// <param name="ScreenshotPath">本次观察使用的截图路径。</param>
public sealed record RewardReceiptDialogObservationResult(
    bool IsSuccess,
    bool IsVisible,
    string Message,
    string? ScreenshotPath);
