namespace HimeBot.Game.Koihime.Domain;

/// <summary>
/// 当前任务面板中的奖励领取状态。
/// </summary>
public enum MissionRewardAvailability
{
    /// <summary>
    /// 现有证据不足，不能安全决定是否点击。
    /// </summary>
    Unknown,

    /// <summary>
    /// “一括完了”按钮明亮，可以领取奖励。
    /// </summary>
    Available,

    /// <summary>
    /// “一括完了”按钮变暗，没有可领取奖励。
    /// </summary>
    Unavailable
}