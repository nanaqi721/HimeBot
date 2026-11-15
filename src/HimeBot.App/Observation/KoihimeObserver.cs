using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Platform.Windows.ScreenCapture;
using HimeBot.Vision.Matching;
using System.IO;

namespace HimeBot.App.Observation;

/// <summary>
/// 执行一次恋姬大战场景观察。
/// </summary>
public sealed class KoihimeObserver
{
    private const int ExpectedScreenshotWidth = 1280;
    private const int ExpectedScreenshotHeight = 720;

    private readonly WindowsGameScreenshotCapture _screenshotCapture;
    private readonly TemplateMatcher _templateMatcher;
    private readonly HomeSceneRecognizer _homeSceneRecognizer;
    private readonly MissionPanelRecognizer _missionPanelRecognizer;

    public KoihimeObserver(
        WindowsGameScreenshotCapture screenshotCapture,
        TemplateMatcher templateMatcher,
        HomeSceneRecognizer homeSceneRecognizer,
        MissionPanelRecognizer missionPanelRecognizer)
    {
        ArgumentNullException.ThrowIfNull(screenshotCapture);
        ArgumentNullException.ThrowIfNull(templateMatcher);
        ArgumentNullException.ThrowIfNull(homeSceneRecognizer);
        ArgumentNullException.ThrowIfNull(missionPanelRecognizer);

        _screenshotCapture = screenshotCapture;
        _templateMatcher = templateMatcher;
        _homeSceneRecognizer = homeSceneRecognizer;
        _missionPanelRecognizer = missionPanelRecognizer;
    }

    public async Task<KoihimeObservationResult> ObserveAsync(
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
            return Failure(captureResult.Message);
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

        string homeTemplatePath =
            GetTemplatePath(
                "home-settings-icon.png");

        TemplateMatchResult homeMatchResult =
            await _templateMatcher.FindBestMatchAsync(
                captureResult.FilePath,
                homeTemplatePath,
                cancellationToken);

        if (!homeMatchResult.IsSuccess)
        {
            return Failure(
                $"截图已经保存，但首页模板匹配失败：" +
                $"{homeMatchResult.Message}",
                captureResult.FilePath);
        }

        SceneRecognitionResult homeSceneResult =
            _homeSceneRecognizer.Recognize(
                homeMatchResult.BestScore,
                homeMatchResult.X,
                homeMatchResult.Y);

        string missionPanelTemplatePath =
            GetTemplatePath(
                "mission-panel-header.png");

        TemplateMatchResult missionPanelMatchResult =
            await _templateMatcher.FindBestMatchAsync(
                captureResult.FilePath,
                missionPanelTemplatePath,
                cancellationToken);

        if (!missionPanelMatchResult.IsSuccess)
        {
            return Failure(
                $"截图已经保存，但任务面板模板匹配失败：" +
                $"{missionPanelMatchResult.Message}",
                captureResult.FilePath);
        }

        SceneRecognitionResult missionPanelSceneResult =
            _missionPanelRecognizer.Recognize(
                missionPanelMatchResult.BestScore,
                missionPanelMatchResult.X,
                missionPanelMatchResult.Y);

        GameScene scene =
            ResolveScene(
                homeSceneResult,
                missionPanelSceneResult);

        return new KoihimeObservationResult(
            true,
            scene,
            $"场景观察完成：{scene}。",
            captureResult.FilePath);
    }

    private static GameScene ResolveScene(
        SceneRecognitionResult homeResult,
        SceneRecognitionResult missionPanelResult)
    {
        bool isHome =
            homeResult.Scene == GameScene.Home;

        bool isMissionPanel =
            missionPanelResult.Scene ==
            GameScene.MissionPanel;

        if (isHome && isMissionPanel)
        {
            return GameScene.Unknown;
        }

        if (isHome)
        {
            return GameScene.Home;
        }

        if (isMissionPanel)
        {
            return GameScene.MissionPanel;
        }

        return GameScene.Unknown;
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

    private static KoihimeObservationResult Failure(
        string message,
        string? screenshotPath = null)
    {
        return new KoihimeObservationResult(
            false,
            GameScene.Unknown,
            message,
            screenshotPath);
    }
}
