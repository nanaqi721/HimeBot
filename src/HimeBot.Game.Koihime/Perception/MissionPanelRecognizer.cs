using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据任务面板固定标题模板的分数和位置判断任务面板场景。
/// </summary>
public sealed class MissionPanelRecognizer
{
    private const double MissionPanelMinimumScore = 0.90;

    private const int ExpectedMissionPanelMatchX = 24;
    private const int ExpectedMissionPanelMatchY = 14;
    private const int MissionPanelPositionTolerance = 5;

    public SceneRecognitionResult Recognize(
        double templateScore,
        int templateX,
        int templateY)
    {
        if (!double.IsFinite(templateScore))
        {
            return new SceneRecognitionResult(
                GameScene.Unknown,
                "任务面板模板分数不是有效的有限数值。",
                templateScore,
                templateX,
                templateY);
        }

        bool scoreMatches =
            templateScore >= MissionPanelMinimumScore;

        bool positionMatches =
            templateX >= 0 &&
            templateY >= 0 &&
            Math.Abs(
                templateX -
                ExpectedMissionPanelMatchX) <=
                MissionPanelPositionTolerance &&
            Math.Abs(
                templateY -
                ExpectedMissionPanelMatchY) <=
                MissionPanelPositionTolerance;

        if (scoreMatches && positionMatches)
        {
            return new SceneRecognitionResult(
                GameScene.MissionPanel,
                "任务面板模板分数和位置均符合要求。",
                templateScore,
                templateX,
                templateY);
        }

        return new SceneRecognitionResult(
            GameScene.Unknown,
            $"任务面板证据不足。" +
            $"要求分数不低于 " +
            $"{MissionPanelMinimumScore:0.00}，" +
            $"且位置接近 " +
            $"({ExpectedMissionPanelMatchX}, " +
            $"{ExpectedMissionPanelMatchY})；" +
            $"实际分数为 {templateScore:0.0000}，" +
            $"位置为 ({templateX}, {templateY})。",
            templateScore,
            templateX,
            templateY);
    }
}
