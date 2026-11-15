using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 根据模板匹配证据判断恋姬大战的当前场景。
/// </summary>

public sealed class HomeSceneRecognizer
{
    private const double HomeMinimumScore = 0.75;

    private const int ExpectedHomeMatchX = 948;
    private const int ExpectedHomeMatchY = 535;
    private const int HomePositionTolerance = 5;

    public SceneRecognitionResult Recognize(
        double templateScore,
        int templateX,
        int templateY)
    {
        if (!double.IsFinite(templateScore))
        {
            return new SceneRecognitionResult(
                GameScene.Unknown,
                "首页模板分数不是有效的有限数值。",
                templateScore,
                templateX,
                templateY);
        }

        bool scoreMatches =
            templateScore >= HomeMinimumScore;

        bool positionMatches =
            templateX >= 0 &&
            templateY >= 0 &&
            Math.Abs(
                templateX -
                ExpectedHomeMatchX) <=
                HomePositionTolerance &&
            Math.Abs(
                templateY -
                ExpectedHomeMatchY) <=
                HomePositionTolerance;

        if (scoreMatches && positionMatches)
        {
            return new SceneRecognitionResult(
                GameScene.Home,
                "首页模板分数和位置均符合要求。",
                templateScore,
                templateX,
                templateY);
        }

        return new SceneRecognitionResult(
            GameScene.Unknown,
            $"首页证据不足。" +
            $"要求分数不低于 {HomeMinimumScore:0.00}，" +
            $"且位置接近 " +
            $"({ExpectedHomeMatchX}, {ExpectedHomeMatchY})；" +
            $"实际分数为 {templateScore:0.0000}，" +
            $"位置为 ({templateX}, {templateY})。",
            templateScore,
            templateX,
            templateY);
    }
}