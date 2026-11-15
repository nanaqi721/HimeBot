namespace HimeBot.Vision.ColorAnalysis;

/// <summary>
/// 图片固定区域的平均颜色分析结果。
/// </summary>
/// <param name="IsSuccess">
/// 图片读取、区域校验和颜色计算是否成功。
/// </param>
/// <param name="Message">
/// 分析结果说明或失败原因。
/// </param>
/// <param name="MeanRed">
/// 红色通道平均值，范围为 0 到 255。
/// </param>
/// <param name="MeanGreen">
/// 绿色通道平均值，范围为 0 到 255。
/// </param>
/// <param name="MeanBlue">
/// 蓝色通道平均值，范围为 0 到 255。
/// </param>
public sealed record RegionColorAnalysisResult(
    bool IsSuccess,
    string Message,
    double MeanRed,
    double MeanGreen,
    double MeanBlue);