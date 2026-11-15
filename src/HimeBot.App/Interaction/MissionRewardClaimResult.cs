namespace HimeBot.App.Interaction;

/// <summary>
/// 一次任务奖励领取流程的结果。
/// </summary>
/// <param name="Outcome">领取流程的最终状态。</param>
/// <param name="Message">执行结果或失败原因。</param>
/// <param name="ScreenshotPath">
/// 最近一次观察所使用的截图路径。
/// </param>
public sealed record MissionRewardClaimResult(
    MissionRewardClaimOutcome Outcome,
    string Message,
    string? ScreenshotPath)
{
    /// <summary>
    /// 领取成功和没有奖励都属于正常完成。
    /// </summary>
    public bool IsSuccess =>
        Outcome is
            MissionRewardClaimOutcome.Claimed or
            MissionRewardClaimOutcome.NoRewardAvailable;
}