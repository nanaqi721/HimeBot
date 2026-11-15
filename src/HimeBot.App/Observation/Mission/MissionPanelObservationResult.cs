using HimeBot.Game.Koihime.Domain;

namespace HimeBot.App.Observation.Mission;

/// <summary>
/// 一次任务面板观察的结果。
/// </summary>
/// <param name="IsSuccess">
/// 截图和任务面板模板匹配是否成功执行。
/// </param>
/// <param name="IsMissionPanel">
/// 是否已经取得足够证据确认当前为任务面板。
/// </param>
/// <param name="RewardAvailability">
/// 已确认任务面板时，“一括完了”按钮表示的奖励状态。
/// </param>
/// <param name="Message">观察说明或失败原因。</param>
/// <param name="ScreenshotPath">本次观察使用的截图路径。</param>
public sealed record MissionPanelObservationResult(
    bool IsSuccess,
    bool IsMissionPanel,
    MissionRewardAvailability RewardAvailability,
    string Message,
    string? ScreenshotPath);
