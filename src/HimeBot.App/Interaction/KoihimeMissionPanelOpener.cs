using HimeBot.App.Observation.Home;
using HimeBot.App.Observation.Mission;
using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Input;

namespace HimeBot.App.Interaction;

/// <summary>
/// 确认当前位于首页后，点击固定的任务入口。
/// </summary>
public sealed class KoihimeMissionPanelOpener
{
    private const int MissionButtonClientX = 1235;
    private const int MissionButtonClientY = 565;

    private static readonly TimeSpan VerificationTimeout =
        TimeSpan.FromSeconds(3);

    private static readonly TimeSpan VerificationInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly HomeSceneObserver _homeSceneObserver;
    private readonly MissionPanelObserver _missionPanelObserver;
    private readonly SendInputAdapter _inputAdapter;

    public KoihimeMissionPanelOpener(
        HomeSceneObserver homeSceneObserver,
        MissionPanelObserver missionPanelObserver,
        SendInputAdapter inputAdapter)
    {
        ArgumentNullException.ThrowIfNull(homeSceneObserver);
        ArgumentNullException.ThrowIfNull(missionPanelObserver);
        ArgumentNullException.ThrowIfNull(inputAdapter);

        _homeSceneObserver = homeSceneObserver;
        _missionPanelObserver = missionPanelObserver;
        _inputAdapter = inputAdapter;
    }

    public async Task<MissionPanelOpenResult> OpenAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        HomeSceneObservationResult observation =
            await _homeSceneObserver.ObserveAsync(
                window,
                cancellationToken);

        if (!observation.IsSuccess)
        {
            return new MissionPanelOpenResult(
                false,
                $"点击任务入口前的场景观察失败：" +
                $"{observation.Message}",
                observation.ScreenshotPath);
        }

        if (!observation.IsHome)
        {
            return new MissionPanelOpenResult(
                false,
                "当前没有确认处于首页，未点击任务入口。",
                observation.ScreenshotPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        GameInputResult inputResult =
            _inputAdapter.ClickClientPoint(
                window,
                MissionButtonClientX,
                MissionButtonClientY);

        if (!inputResult.IsSuccess)
        {
            return new MissionPanelOpenResult(
                false,
                $"首页已经确认，但任务入口点击失败：" +
                $"{inputResult.Message}",
                observation.ScreenshotPath);
        }

        return await WaitForMissionPanelAsync(
            window,
            observation.ScreenshotPath,
            cancellationToken);
    }

    private async Task<MissionPanelOpenResult>
        WaitForMissionPanelAsync(
            GameWindowInfo window,
            string? preClickScreenshotPath,
            CancellationToken cancellationToken)
    {
        string? latestScreenshotPath =
            preClickScreenshotPath;

        bool latestIsMissionPanel = false;

        using var verificationCancellation =
            CancellationTokenSource
                .CreateLinkedTokenSource(
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

                MissionPanelObservationResult observation =
                    await _missionPanelObserver.ObserveAsync(
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
                    return new MissionPanelOpenResult(
                        false,
                        $"任务入口点击已提交，但验证任务面板时" +
                        $"场景观察失败：{observation.Message}",
                        latestScreenshotPath);
                }

                latestIsMissionPanel =
                    observation.IsMissionPanel;

                if (latestIsMissionPanel)
                {
                    return new MissionPanelOpenResult(
                        true,
                        "任务入口点击已提交，" +
                        "并已确认任务面板打开；" +
                        $"当前奖励状态：" +
                        $"{observation.RewardAvailability}。",
                        latestScreenshotPath);
                }
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return new MissionPanelOpenResult(
                false,
                $"任务入口点击已提交，但在 " +
                $"{VerificationTimeout.TotalSeconds:0.#} 秒内" +
                $"没有确认任务面板打开；" +
                $"最后一次是否确认任务面板：" +
                $"{latestIsMissionPanel}。",
                latestScreenshotPath);
        }
    }
}
