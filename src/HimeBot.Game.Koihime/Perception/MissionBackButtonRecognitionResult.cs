namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 任务面板左上角返回按钮的判定结果。
/// </summary>
/// <param name="IsVisible">返回按钮的分数和位置是否符合要求。</param>
/// <param name="Message">判定说明。</param>
/// <param name="TemplateScore">模板匹配分数。</param>
/// <param name="TemplateX">最佳匹配位置的 X 坐标。</param>
/// <param name="TemplateY">最佳匹配位置的 Y 坐标。</param>
public sealed record MissionBackButtonRecognitionResult(
    bool IsVisible,
    string Message,
    double TemplateScore,
    int TemplateX,
    int TemplateY);
