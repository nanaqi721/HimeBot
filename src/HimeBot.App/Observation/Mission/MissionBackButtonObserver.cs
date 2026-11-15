using HimeBot.App.Observation.Shared;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Vision.ImageProcessing;
using HimeBot.Vision.Matching;
using System.IO;

namespace HimeBot.App.Observation.Mission;

/// <summary>
/// 观察任务面板左上角返回按钮是否可见。
/// </summary>
public sealed class MissionBackButtonObserver
{
    private const int SearchRegionX = 15;
    private const int SearchRegionY = 13;
    private const int SearchRegionWidth = 76;
    private const int SearchRegionHeight = 76;

    private readonly KoihimeFrameCapture _frameCapture;
    private readonly ImagePreprocessor _imagePreprocessor;
    private readonly TemplateMatcher _templateMatcher;
    private readonly MissionBackButtonRecognizer _recognizer;

    public MissionBackButtonObserver(
        KoihimeFrameCapture frameCapture,
        ImagePreprocessor imagePreprocessor,
        TemplateMatcher templateMatcher,
        MissionBackButtonRecognizer recognizer)
    {
        ArgumentNullException.ThrowIfNull(frameCapture);
        ArgumentNullException.ThrowIfNull(imagePreprocessor);
        ArgumentNullException.ThrowIfNull(templateMatcher);
        ArgumentNullException.ThrowIfNull(recognizer);

        _frameCapture = frameCapture;
        _imagePreprocessor = imagePreprocessor;
        _templateMatcher = templateMatcher;
        _recognizer = recognizer;
    }

    public async Task<MissionBackButtonObservationResult> ObserveAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        KoihimeFrameCaptureResult frameResult =
            await _frameCapture.CaptureAsync(
                window,
                cancellationToken);

        if (!frameResult.IsSuccess ||
            string.IsNullOrWhiteSpace(frameResult.ScreenshotPath))
        {
            return Failure(
                $"返回按钮观察无法取得有效截图：" +
                $"{frameResult.Message}",
                frameResult.ScreenshotPath);
        }

        string searchRegionPath =
            GetSearchRegionPath(
                frameResult.ScreenshotPath);

        ImageProcessingResult regionResult =
            await _imagePreprocessor.CreateRegionCopyAsync(
                frameResult.ScreenshotPath,
                searchRegionPath,
                SearchRegionX,
                SearchRegionY,
                SearchRegionWidth,
                SearchRegionHeight,
                cancellationToken);

        if (!regionResult.IsSuccess ||
            string.IsNullOrWhiteSpace(regionResult.OutputPath))
        {
            return Failure(
                $"返回按钮搜索区域裁剪失败：" +
                $"{regionResult.Message}",
                frameResult.ScreenshotPath);
        }

        string templatePath =
            GetTemplatePath(
                "mission-back-button.png");

        TemplateMatchResult matchResult =
            await _templateMatcher.FindBestMatchAsync(
                regionResult.OutputPath,
                templatePath,
                cancellationToken);

        if (!matchResult.IsSuccess)
        {
            return Failure(
                $"返回按钮模板匹配失败：" +
                $"{matchResult.Message}",
                frameResult.ScreenshotPath);
        }

        MissionBackButtonRecognitionResult recognition =
            _recognizer.Recognize(
                matchResult.BestScore,
                matchResult.X,
                matchResult.Y);

        return new MissionBackButtonObservationResult(
            true,
            recognition.IsVisible,
            recognition.IsVisible
                ? $"返回按钮观察完成，已确认按钮可见；" +
                  $"搜索区域：{regionResult.OutputPath}。"
                : $"返回按钮观察完成，但没有确认按钮。" +
                  $"{recognition.Message}",
            frameResult.ScreenshotPath);
    }

    private static string GetSearchRegionPath(
        string screenshotPath)
    {
        string directory =
            Path.GetDirectoryName(screenshotPath) ??
            AppContext.BaseDirectory;

        string fileName =
            $"{Path.GetFileNameWithoutExtension(screenshotPath)}" +
            $"-mission-back-region.png";

        return Path.Combine(
            directory,
            fileName);
    }

    private static string GetTemplatePath(
        string fileName)
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "koihime",
            "templates",
            fileName);
    }

    private static MissionBackButtonObservationResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new MissionBackButtonObservationResult(
            false,
            false,
            message,
            screenshotPath);
    }
}
