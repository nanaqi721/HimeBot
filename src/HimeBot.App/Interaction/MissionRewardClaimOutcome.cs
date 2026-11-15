namespace HimeBot.App.Interaction;

/// <summary>
/// 一次任务奖励领取流程的最终结果。
/// </summary>
public enum MissionRewardClaimOutcome
{
    /// <summary>
    /// 观察、点击或后置条件验证失败。
    /// </summary>
    Failed,

    /// <summary>
    /// 已确认任务面板，但当前没有可领取奖励。
    /// 这是正常结果，不是执行错误。
    /// </summary>
    NoRewardAvailable,

    /// <summary>
    /// 已领取奖励、关闭结果弹窗，并确认按钮变为不可领取。
    /// </summary>
    Claimed
}