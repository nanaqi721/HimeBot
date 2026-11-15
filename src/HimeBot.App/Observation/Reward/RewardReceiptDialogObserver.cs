using HimeBot.App.Observation.Shared;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Vision.Matching;
using System.IO;

namespace HimeBot.App.Observation.Reward;

/// <summary>
/// 观察当前画面是否显示奖励领取结果弹窗。
/// </summary>
public sealed class RewardReceiptDialogObserver
{
    private readonly KoihimeFrameCapture _frameCapture;
    private readonly TemplateMatcher _templateMatcher;
    private readonly RewardReceiptDialogRecognizer _sceneRecognizer;

    public RewardReceiptDialogObserver(
        KoihimeFrameCapture frameCapture,
        TemplateMatcher templateMatcher,
        RewardReceiptDialogRecognizer sceneRecognizer)
    {
        ArgumentNullException.ThrowIfNull(frameCapture);
        ArgumentNullException.ThrowIfNull(templateMatcher);
        ArgumentNullException.ThrowIfNull(sceneRecognizer);

        _frameCapture = frameCapture;
        _templateMatcher = templateMatcher;
        _sceneRecognizer = sceneRecognizer;
    }

    public async Task<RewardReceiptDialogObservationResult>
        ObserveAsync(
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
                $"奖励弹窗观察无法取得有效截图：" +
                $"{frameResult.Message}",
                frameResult.ScreenshotPath);
        }

        string templatePath =
            GetTemplatePath(
                "reward-receipt-close-button.png");

        TemplateMatchResult matchResult =
            await _templateMatcher.FindBestMatchAsync(
                frameResult.ScreenshotPath,
                templatePath,
                cancellationToken);

        if (!matchResult.IsSuccess)
        {
            return Failure(
                $"奖励弹窗模板匹配失败：" +
                $"{matchResult.Message}",
                frameResult.ScreenshotPath);
        }

        SceneRecognitionResult sceneResult =
            _sceneRecognizer.Recognize(
                matchResult.BestScore,
                matchResult.X,
                matchResult.Y);

        bool isVisible =
            sceneResult.Scene ==
            GameScene.RewardReceiptDialog;

        return new RewardReceiptDialogObservationResult(
            true,
            isVisible,
            isVisible
                ? "奖励弹窗观察完成，已确认 RewardReceiptDialog。"
                : $"奖励弹窗观察完成，但没有确认弹窗。" +
                  $"{sceneResult.Message}",
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

    private static RewardReceiptDialogObservationResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new RewardReceiptDialogObservationResult(
            false,
            false,
            message,
            screenshotPath);
    }
}
