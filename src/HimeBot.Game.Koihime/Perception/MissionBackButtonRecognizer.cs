namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据返回按钮模板的匹配分数和位置，
/// 判断任务面板左上角返回按钮是否可见。
/// </summary>
public sealed class MissionBackButtonRecognizer
{
    private const double MinimumTemplateScore = 0.90;

    // 运行时先裁剪左上角 (15, 13, 76, 76) 搜索区，
    // 因此这里使用模板在裁剪区域内的相对位置。
    private const int ExpectedTemplateX = 12;
    private const int ExpectedTemplateY = 12;
    private const int PositionTolerance = 5;

    public MissionBackButtonRecognitionResult Recognize(
        double templateScore,
        int templateX,
        int templateY)
    {
        if (!double.IsFinite(templateScore))
        {
            return new MissionBackButtonRecognitionResult(
                false,
                "返回按钮模板分数不是有效的有限数值。",
                templateScore,
                templateX,
                templateY);
        }

        bool scoreMatches =
            templateScore >= MinimumTemplateScore;

        bool positionMatches =
            templateX >= 0 &&
            templateY >= 0 &&
            Math.Abs(templateX - ExpectedTemplateX) <=
                PositionTolerance &&
            Math.Abs(templateY - ExpectedTemplateY) <=
                PositionTolerance;

        if (scoreMatches && positionMatches)
        {
            return new MissionBackButtonRecognitionResult(
                true,
                "返回按钮模板分数和位置均符合要求。",
                templateScore,
                templateX,
                templateY);
        }

        return new MissionBackButtonRecognitionResult(
            false,
            $"返回按钮证据不足。" +
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
