

namespace HimeBot.Platform.Windows.ScreenCapture;

/// <summary>
/// 单帧截图结果。
/// </summary>
/// <param name="IsSuccess">截图是否成功</param>
/// <param name="Message">结果说明</param>
/// <param name="FilePath">成功时保存的文件路径</param>
/// <param name="Width">成功时图片宽度</param>
/// <param name="Height">成功时图片高度</param>
public sealed record GameScreenshotCaptureResult(
    bool IsSuccess,
    string Message,
    string? FilePath,
    int Width,
    int Height);
