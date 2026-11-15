namespace HimeBot.Vision.Matching;

/// <summary>
/// 一次模板匹配的结果。
/// </summary>
/// <param name="IsSuccess">匹配过程是否成功执行</param>
/// <param name="Message">结果说明或失败原因</param>
/// <param name="BestScore">最佳相似度分数</param>
/// <param name="X">最佳位置左上角的 X 坐标</param>
/// <param name="Y">最佳位置左上角的 Y 坐标</param>
/// <param name="Width">模板宽度</param>
/// <param name="Height">模板高度</param>
public sealed record TemplateMatchResult(
    bool IsSuccess,
    string Message,
    double BestScore,
    int X,
    int Y,
    int Width,
    int Height);