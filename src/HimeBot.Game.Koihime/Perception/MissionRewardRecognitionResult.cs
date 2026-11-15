using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据“一括完了”按钮颜色得到的奖励状态判定结果。
/// </summary>
/// <param name="Availability">
/// 当前奖励领取状态。
/// </param>
/// <param name="Message">
/// 判定结果或证据不足的原因。
/// </param>
/// <param name="MeanRed">
/// 按钮固定区域中红色通道的平均值，范围应为 0 到 255。
/// </param>
public sealed record MissionRewardRecognitionResult(
    MissionRewardAvailability Availability,
    string Message,
    double MeanRed);