using HimeBot.App.Observation.Home;
using HimeBot.App.Observation.Mission;
using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Input;

namespace HimeBot.App.Interaction;

/// <summary>
/// 确认任务面板和返回按钮后点击返回，并验证首页出现。
/// </summary>
public sealed class KoihimeMissionHomeNavigator
{
    private const int BackButtonClientX = 53;
    private const int BackButtonClientY = 50;

    private static readonly TimeSpan VerificationTimeout =
        TimeSpan.FromSeconds(3);

    private static readonly TimeSpan VerificationInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly MissionPanelObserver _missionPanelObserver;
    private readonly MissionBackButtonObserver _backButtonObserver;
    private readonly HomeSceneObserver _homeSceneObserver;
    private readonly SendInputAdapter _inputAdapter;

    public KoihimeMissionHomeNavigator(
        MissionPanelObserver missionPanelObserver,
        MissionBackButtonObserver backButtonObserver,
        HomeSceneObserver homeSceneObserver,
        SendInputAdapter inputAdapter)
    {
        ArgumentNullException.ThrowIfNull(missionPanelObserver);
        ArgumentNullException.ThrowIfNull(backButtonObserver);
        ArgumentNullException.ThrowIfNull(homeSceneObserver);
        ArgumentNullException.ThrowIfNull(inputAdapter);

        _missionPanelObserver = missionPanelObserver;
        _backButtonObserver = backButtonObserver;
        _homeSceneObserver = homeSceneObserver;
        _inputAdapter = inputAdapter;
    }

    public async Task<MissionHomeNavigationResult> NavigateAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        MissionPanelObservationResult panelObservation =
            await _missionPanelObserver.ObserveAsync(
                window,
                cancellationToken);

        if (!panelObservation.IsSuccess)
        {
            return Failure(
                $"返回首页前的任务面板观察失败：" +
                $"{panelObservation.Message}",
                panelObservation.ScreenshotPath);
        }

        if (!panelObservation.IsMissionPanel)
        {
            return Failure(
                "当前没有确认处于任务面板，未点击返回按钮。",
                panelObservation.ScreenshotPath);
        }

        MissionBackButtonObservationResult buttonObservation =
            await _backButtonObserver.ObserveAsync(
                window,
                cancellationToken);

        if (!buttonObservation.IsSuccess)
        {
            return Failure(
                $"任务面板已经确认，但返回按钮观察失败：" +
                $"{buttonObservation.Message}",
                buttonObservation.ScreenshotPath ??
                panelObservation.ScreenshotPath);
        }

        if (!buttonObservation.IsVisible)
        {
            return Failure(
                "任务面板已经确认，但没有确认返回按钮；" +
                "为了避免误点击，本次不执行返回。",
                buttonObservation.ScreenshotPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        GameInputResult inputResult =
            _inputAdapter.ClickClientPoint(
                window,
                BackButtonClientX,
                BackButtonClientY);

        if (!inputResult.IsSuccess)
        {
            return Failure(
                $"任务面板和返回按钮已经确认，" +
                $"但点击失败：{inputResult.Message}",
                buttonObservation.ScreenshotPath);
        }

        return await WaitForHomeAsync(
            window,
            buttonObservation.ScreenshotPath,
            cancellationToken);
    }

    private async Task<MissionHomeNavigationResult> WaitForHomeAsync(
        GameWindowInfo window,
        string? preClickScreenshotPath,
        CancellationToken cancellationToken)
    {
        string? latestScreenshotPath =
            preClickScreenshotPath;

        using var verificationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        verificationCancellation.CancelAfter(
            VerificationTimeout);

        try
        {
            while (true)
            {
                await Task.Delay(
                    VerificationInterval,
                    verificationCancellation.Token);

                HomeSceneObservationResult observation =
                    await _homeSceneObserver.ObserveAsync(
                        window,
                        verificationCancellation.Token);

                if (!string.IsNullOrWhiteSpace(
                        observation.ScreenshotPath))
                {
                    latestScreenshotPath =
                        observation.ScreenshotPath;
                }

                if (!observation.IsSuccess)
                {
                    return Failure(
                        $"返回按钮点击已提交，" +
                        $"但验证首页时观察失败：" +
                        $"{observation.Message}",
                        latestScreenshotPath);
                }

                if (observation.IsHome)
                {
                    return new MissionHomeNavigationResult(
                        true,
                        "返回按钮点击已提交，并已确认首页出现。",
                        latestScreenshotPath);
                }
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(
                $"返回按钮点击已提交，但在 " +
                $"{VerificationTimeout.TotalSeconds:0.#} 秒内" +
                $"没有确认首页出现。",
                latestScreenshotPath);
        }
    }

    private static MissionHomeNavigationResult Failure(
        string message,
        string? screenshotPath)
    {
        return new MissionHomeNavigationResult(
            false,
            message,
            screenshotPath);
    }
}
