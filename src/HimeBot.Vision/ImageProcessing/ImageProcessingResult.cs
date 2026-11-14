namespace HimeBot.Vision.ImageProcessing;

/// <summary>
/// 单次图像处理结果。
/// </summary>
/// <param name="IsSuccess">处理是否成功</param>
/// <param name="Message">结果说明</param>
/// <param name="OutputPath">成功时生成的图片路径</param>
/// <param name="Width">输出图片宽度</param>
/// <param name="Height">输出图片高度</param>
public sealed record ImageProcessingResult(
    bool IsSuccess,
    string Message,
    string? OutputPath,
    int Width,
    int Height);
