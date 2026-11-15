using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据“一括完了”按钮区域的平均红色通道值，
/// 判断当前是否存在可领取的任务奖励。
/// </summary>
/// <remarks>
/// 只有在已经确认当前是任务面板，且没有检测到领取弹窗后，
/// 才能调用该判定器。
/// </remarks>
public sealed class MissionRewardAvailabilityRecognizer
{
    // 可领取样本的平均红色值约为 221。
    // 先使用 170 作为保守下限。
    private const double AvailableMinimumMeanRed = 170.0;

    // 无奖励样本的平均红色值约为 111。
    // 先使用 145 作为保守上限。
    private const double UnavailableMaximumMeanRed = 145.0;

    public MissionRewardRecognitionResult Recognize(
        double meanRed)
    {
        if (!double.IsFinite(meanRed) ||
            meanRed < 0.0 ||
            meanRed > 255.0)
        {
            return new MissionRewardRecognitionResult(
                MissionRewardAvailability.Unknown,
                "按钮区域的平均红色值无效；" +
                "有效范围应为 0 到 255。",
                meanRed);
        }

        if (meanRed >= AvailableMinimumMeanRed)
        {
            return new MissionRewardRecognitionResult(
                MissionRewardAvailability.Available,
                $"“一括完了”按钮处于明亮状态，" +
                $"平均红色值为 {meanRed:0.0}。",
                meanRed);
        }

        if (meanRed <= UnavailableMaximumMeanRed)
        {
            return new MissionRewardRecognitionResult(
                MissionRewardAvailability.Unavailable,
                $"“一括完了”按钮处于变暗状态，" +
                $"平均红色值为 {meanRed:0.0}。",
                meanRed);
        }

        return new MissionRewardRecognitionResult(
            MissionRewardAvailability.Unknown,
            $"按钮亮度位于不确定区间，" +
            $"平均红色值为 {meanRed:0.0}；" +
            $"为了避免误点击，本次不执行领取。",
            meanRed);
    }
}