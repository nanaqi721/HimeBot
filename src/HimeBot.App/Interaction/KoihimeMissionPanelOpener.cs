using HimeBot.App.Observation;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
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

    private readonly KoihimeObserver _observer;
    private readonly SendInputAdapter _inputAdapter;

    public KoihimeMissionPanelOpener(
        KoihimeObserver observer,
        SendInputAdapter inputAdapter)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(inputAdapter);

        _observer = observer;
        _inputAdapter = inputAdapter;
    }

    public async Task<MissionPanelOpenResult> OpenAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        KoihimeObservationResult observation =
            await _observer.ObserveAsync(
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

        if (observation.Scene != GameScene.Home)
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

        GameScene latestScene = GameScene.Home;

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

                KoihimeObservationResult observation =
                    await _observer.ObserveAsync(
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

                latestScene =
                    observation.Scene;

                if (latestScene == GameScene.MissionPanel)
                {
                    return new MissionPanelOpenResult(
                        true,
                        "任务入口点击已提交，" +
                        "并已确认任务面板打开。",
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
                $"最后观察场景：{latestScene}。",
                latestScreenshotPath);
        }
    }
}
