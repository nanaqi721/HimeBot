using HimeBot.App.Observation.Shared;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Vision.Matching;
using System.IO;

namespace HimeBot.App.Observation.Home;

/// <summary>
/// 观察当前画面是否为恋姬大战首页。
/// </summary>
public sealed class HomeSceneObserver
{
    private readonly KoihimeFrameCapture _frameCapture;
    private readonly TemplateMatcher _templateMatcher;
    private readonly HomeSceneRecognizer _sceneRecognizer;

    public HomeSceneObserver(
        KoihimeFrameCapture frameCapture,
        TemplateMatcher templateMatcher,
        HomeSceneRecognizer sceneRecognizer)
    {
        ArgumentNullException.ThrowIfNull(frameCapture);
        ArgumentNullException.ThrowIfNull(templateMatcher);
        ArgumentNullException.ThrowIfNull(sceneRecognizer);

        _frameCapture = frameCapture;
        _templateMatcher = templateMatcher;
        _sceneRecognizer = sceneRecognizer;
    }

    public async Task<HomeSceneObservationResult> ObserveAsync(
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
                $"首页观察无法取得有效截图：" +
                $"{frameResult.Message}",
                frameResult.ScreenshotPath);
        }

        string templatePath =
            GetTemplatePath(
                "home-settings-icon.png");

        TemplateMatchResult matchResult =
            await _templateMatcher.FindBestMatchAsync(
                frameResult.ScreenshotPath,
                templatePath,
                cancellationToken);

        if (!matchResult.IsSuccess)
        {
            return Failure(
                $"首页模板匹配失败：{matchResult.Message}",
                frameResult.ScreenshotPath);
        }

        SceneRecognitionResult sceneResult =
            _sceneRecognizer.Recognize(
                matchResult.BestScore,
                matchResult.X,
                matchResult.Y);

        bool isHome =
            sceneResult.Scene == GameScene.Home;

        return new HomeSceneObservationResult(
            true,
            isHome,
            isHome
                ? "首页观察完成，已确认当前为 Home。"
                : $"首页观察完成，但没有确认 Home。" +
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

    private static HomeSceneObservationResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new HomeSceneObservationResult(
            false,
            false,
            message,
            screenshotPath);
    }
}
