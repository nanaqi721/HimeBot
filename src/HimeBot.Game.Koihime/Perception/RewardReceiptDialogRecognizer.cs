using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据“閉じる”按钮模板的匹配分数和位置，
/// 判断当前是否显示奖励领取结果弹窗。
/// </summary>
public sealed class RewardReceiptDialogRecognizer
{
    private const double MinimumTemplateScore = 0.90;

    private const int ExpectedTemplateX = 520;
    private const int ExpectedTemplateY = 601;
    private const int PositionTolerance = 5;

    public SceneRecognitionResult Recognize(
        double templateScore,
        int templateX,
        int templateY)
    {
        if (!double.IsFinite(templateScore))
        {
            return new SceneRecognitionResult(
                GameScene.Unknown,
                "奖励领取弹窗模板分数不是有效的有限数值。",
                templateScore,
                templateX,
                templateY);
        }

        bool scoreMatches =
            templateScore >= MinimumTemplateScore;

        bool positionMatches =
            templateX >= 0 &&
            templateY >= 0 &&
            Math.Abs(
                templateX -
                ExpectedTemplateX) <=
                PositionTolerance &&
            Math.Abs(
                templateY -
                ExpectedTemplateY) <=
                PositionTolerance;

        if (scoreMatches && positionMatches)
        {
            return new SceneRecognitionResult(
                GameScene.RewardReceiptDialog,
                "“閉じる”模板分数和位置均符合要求，" +
                "已确认奖励领取结果弹窗。",
                templateScore,
                templateX,
                templateY);
        }

        return new SceneRecognitionResult(
            GameScene.Unknown,
            $"奖励领取弹窗证据不足。" +
            $"要求分数不低于 {MinimumTemplateScore:0.00}，" +
            $"且位置接近 " +
            $"({ExpectedTemplateX}, {ExpectedTemplateY})；" +
            $"实际分数为 {templateScore:0.0000}，" +
            $"位置为 ({templateX}, {templateY})。",
            templateScore,
            templateX,
            templateY);
    }
}