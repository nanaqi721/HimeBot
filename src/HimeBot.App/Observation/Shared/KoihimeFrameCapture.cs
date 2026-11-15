using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.ScreenCapture;
using System.IO;

namespace HimeBot.App.Observation.Shared;

/// <summary>
/// 为恋姬大战场景观察生成一张新的标准客户区截图。
/// </summary>
public sealed class KoihimeFrameCapture
{
    private const int ExpectedScreenshotWidth = 1280;
    private const int ExpectedScreenshotHeight = 720;

    private readonly WindowsGameScreenshotCapture _screenshotCapture;

    public KoihimeFrameCapture(
        WindowsGameScreenshotCapture screenshotCapture)
    {
        ArgumentNullException.ThrowIfNull(screenshotCapture);

        _screenshotCapture = screenshotCapture;
    }

    public async Task<KoihimeFrameCaptureResult> CaptureAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        string captureDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HimeBot",
                "captures");

        string fileName =
            $"koihime-observation-" +
            $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";

        string outputPath =
            Path.Combine(
                captureDirectory,
                fileName);

        GameScreenshotCaptureResult captureResult =
            await _screenshotCapture.CaptureToPngAsync(
                window,
                outputPath,
                cancellationToken);

        if (!captureResult.IsSuccess)
        {
            return Failure(
                captureResult.Message,
                captureResult.FilePath);
        }

        if (string.IsNullOrWhiteSpace(captureResult.FilePath))
        {
            return Failure(
                "截图成功，但没有返回图片路径。");
        }

        if (captureResult.Width != ExpectedScreenshotWidth ||
            captureResult.Height != ExpectedScreenshotHeight)
        {
            return Failure(
                $"当前场景识别只支持 " +
                $"{ExpectedScreenshotWidth} × " +
                $"{ExpectedScreenshotHeight} 客户区截图；" +
                $"实际截图为 {captureResult.Width} × " +
                $"{captureResult.Height}。",
                captureResult.FilePath);
        }

        return new KoihimeFrameCaptureResult(
            true,
            $"客户区截图已经保存并通过 " +
            $"{ExpectedScreenshotWidth} × " +
            $"{ExpectedScreenshotHeight} 尺寸校验。",
            captureResult.FilePath);
    }

    private static KoihimeFrameCaptureResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new KoihimeFrameCaptureResult(
            false,
            message,
            screenshotPath);
    }
}
