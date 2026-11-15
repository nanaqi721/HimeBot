using HimeBot.App.Observation.Shared;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Vision.ColorAnalysis;
using HimeBot.Vision.Matching;
using System.IO;

namespace HimeBot.App.Observation.Mission;

/// <summary>
/// 观察当前画面是否为任务面板，并在确认面板后判断奖励状态。
/// </summary>
public sealed class MissionPanelObserver
{
    private const int MissionClaimRegionX = 1070;
    private const int MissionClaimRegionY = 566;
    private const int MissionClaimRegionWidth = 141;
    private const int MissionClaimRegionHeight = 48;

    private readonly KoihimeFrameCapture _frameCapture;
    private readonly TemplateMatcher _templateMatcher;
    private readonly MissionPanelRecognizer _sceneRecognizer;
    private readonly RegionColorAnalyzer _colorAnalyzer;
    private readonly MissionRewardAvailabilityRecognizer
        _rewardAvailabilityRecognizer;

    public MissionPanelObserver(
        KoihimeFrameCapture frameCapture,
        TemplateMatcher templateMatcher,
        MissionPanelRecognizer sceneRecognizer,
        RegionColorAnalyzer colorAnalyzer,
        MissionRewardAvailabilityRecognizer
            rewardAvailabilityRecognizer)
    {
        ArgumentNullException.ThrowIfNull(frameCapture);
        ArgumentNullException.ThrowIfNull(templateMatcher);
        ArgumentNullException.ThrowIfNull(sceneRecognizer);
        ArgumentNullException.ThrowIfNull(colorAnalyzer);
        ArgumentNullException.ThrowIfNull(
            rewardAvailabilityRecognizer);

        _frameCapture = frameCapture;
        _templateMatcher = templateMatcher;
        _sceneRecognizer = sceneRecognizer;
        _colorAnalyzer = colorAnalyzer;
        _rewardAvailabilityRecognizer =
            rewardAvailabilityRecognizer;
    }

    public async Task<MissionPanelObservationResult> ObserveAsync(
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
                $"任务面板观察无法取得有效截图：" +
                $"{frameResult.Message}",
                frameResult.ScreenshotPath);
        }

        string templatePath =
            GetTemplatePath(
                "mission-panel-header.png");

        TemplateMatchResult matchResult =
            await _templateMatcher.FindBestMatchAsync(
                frameResult.ScreenshotPath,
                templatePath,
                cancellationToken);

        if (!matchResult.IsSuccess)
        {
            return Failure(
                $"任务面板模板匹配失败：" +
                $"{matchResult.Message}",
                frameResult.ScreenshotPath);
        }

        SceneRecognitionResult sceneResult =
            _sceneRecognizer.Recognize(
                matchResult.BestScore,
                matchResult.X,
                matchResult.Y);

        if (sceneResult.Scene != GameScene.MissionPanel)
        {
            return new MissionPanelObservationResult(
                true,
                false,
                MissionRewardAvailability.Unknown,
                $"任务面板观察完成，但没有确认 MissionPanel。" +
                $"{sceneResult.Message}",
                frameResult.ScreenshotPath);
        }

        RegionColorAnalysisResult colorResult =
            await _colorAnalyzer.AnalyzeAsync(
                frameResult.ScreenshotPath,
                MissionClaimRegionX,
                MissionClaimRegionY,
                MissionClaimRegionWidth,
                MissionClaimRegionHeight,
                cancellationToken);

        if (!colorResult.IsSuccess)
        {
            return new MissionPanelObservationResult(
                true,
                true,
                MissionRewardAvailability.Unknown,
                "已确认 MissionPanel，但奖励按钮颜色分析失败；" +
                $"奖励状态降级为 Unknown。{colorResult.Message}",
                frameResult.ScreenshotPath);
        }

        MissionRewardRecognitionResult rewardResult =
            _rewardAvailabilityRecognizer.Recognize(
                colorResult.MeanRed);

        return new MissionPanelObservationResult(
            true,
            true,
            rewardResult.Availability,
            $"已确认 MissionPanel；" +
            $"任务奖励状态：{rewardResult.Availability}。" +
            $"{rewardResult.Message} " +
            $"完整颜色证据：" +
            $"RGB=({colorResult.MeanRed:0.0}, " +
            $"{colorResult.MeanGreen:0.0}, " +
            $"{colorResult.MeanBlue:0.0})。",
            frameResult.ScreenshotPath);
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

    private static MissionPanelObservationResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new MissionPanelObservationResult(
            false,
            false,
            MissionRewardAvailability.Unknown,
            message,
            screenshotPath);
    }
}
