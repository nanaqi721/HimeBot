using HimeBot.App.Observation.Mission;
using HimeBot.App.Observation.Reward;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Domain;
using HimeBot.Platform.Windows.Input;

namespace HimeBot.App.Interaction;

/// <summary>
/// 在已经打开的任务面板中领取全部可领取奖励。
/// </summary>
public sealed class KoihimeMissionRewardClaimer
{
    // “一括完了”区域：
    // X = 1070, Y = 566, Width = 141, Height = 48
    private const int ClaimButtonClientX = 1140;
    private const int ClaimButtonClientY = 590;

    // “閉じる”模板区域：
    // X = 520, Y = 601, Width = 240, Height = 63
    private const int CloseButtonClientX = 640;
    private const int CloseButtonClientY = 632;

    private static readonly TimeSpan VerificationTimeout =
        TimeSpan.FromSeconds(3);

    private static readonly TimeSpan VerificationInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly MissionPanelObserver _missionPanelObserver;

    private readonly RewardReceiptDialogObserver
        _rewardReceiptDialogObserver;

    private readonly SendInputAdapter _inputAdapter;

    public KoihimeMissionRewardClaimer(
        MissionPanelObserver missionPanelObserver,
        RewardReceiptDialogObserver rewardReceiptDialogObserver,
        SendInputAdapter inputAdapter)
    {
        ArgumentNullException.ThrowIfNull(missionPanelObserver);

        ArgumentNullException.ThrowIfNull(
            rewardReceiptDialogObserver);

        ArgumentNullException.ThrowIfNull(inputAdapter);

        _missionPanelObserver = missionPanelObserver;
        _rewardReceiptDialogObserver =
            rewardReceiptDialogObserver;
        _inputAdapter = inputAdapter;
    }

    /// <summary>
    /// 观察任务面板、领取奖励、关闭弹窗，并验证领取结束状态。
    /// </summary>
    public async Task<MissionRewardClaimResult> ClaimAsync(
        GameWindowInfo window,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        MissionPanelObservationResult initialObservation =
            await _missionPanelObserver.ObserveAsync(
                window,
                cancellationToken);

        if (!initialObservation.IsSuccess)
        {
            return Failed(
                $"领取前的任务面板观察失败：" +
                $"{initialObservation.Message}",
                initialObservation.ScreenshotPath);
        }

        if (!initialObservation.IsMissionPanel)
        {
            return Failed(
                "当前没有确认处于任务面板，" +
                "为了避免误点击，本次不执行奖励领取。",
                initialObservation.ScreenshotPath);
        }

        if (initialObservation.RewardAvailability ==
            MissionRewardAvailability.Unavailable)
        {
            return new MissionRewardClaimResult(
                MissionRewardClaimOutcome.NoRewardAvailable,
                "已经确认任务面板，但当前没有可领取奖励。",
                initialObservation.ScreenshotPath);
        }

        if (initialObservation.RewardAvailability ==
            MissionRewardAvailability.Unknown)
        {
            return Failed(
                "已经确认任务面板，但奖励状态为 Unknown；" +
                "为了避免误点击，本次不执行奖励领取。",
                initialObservation.ScreenshotPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        GameInputResult claimClickResult =
            _inputAdapter.ClickClientPoint(
                window,
                ClaimButtonClientX,
                ClaimButtonClientY);

        if (!claimClickResult.IsSuccess)
        {
            return Failed(
                $"已经确认存在可领取奖励，" +
                $"但点击“一括完了”失败：" +
                $"{claimClickResult.Message}",
                initialObservation.ScreenshotPath);
        }

        WaitResult dialogWaitResult =
            await WaitForRewardDialogAsync(
                window,
                initialObservation.ScreenshotPath,
                cancellationToken);

        if (!dialogWaitResult.IsSuccess)
        {
            return Failed(
                dialogWaitResult.Message,
                dialogWaitResult.ScreenshotPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        GameInputResult closeClickResult =
            _inputAdapter.ClickClientPoint(
                window,
                CloseButtonClientX,
                CloseButtonClientY);

        if (!closeClickResult.IsSuccess)
        {
            return Failed(
                $"已经确认奖励领取弹窗，" +
                $"但点击“閉じる”失败：" +
                $"{closeClickResult.Message}",
                dialogWaitResult.ScreenshotPath);
        }

        WaitResult completionWaitResult =
            await WaitForUnavailableMissionPanelAsync(
                window,
                dialogWaitResult.ScreenshotPath,
                cancellationToken);

        if (!completionWaitResult.IsSuccess)
        {
            return Failed(
                completionWaitResult.Message,
                completionWaitResult.ScreenshotPath);
        }

        return new MissionRewardClaimResult(
            MissionRewardClaimOutcome.Claimed,
            "奖励领取完成：已经点击“一括完了”、" +
            "确认并关闭奖励弹窗，" +
            "且任务面板按钮已经变为不可领取状态。",
            completionWaitResult.ScreenshotPath);
    }

    private async Task<WaitResult>
        WaitForRewardDialogAsync(
            GameWindowInfo window,
            string? previousScreenshotPath,
            CancellationToken cancellationToken)
    {
        string? latestScreenshotPath =
            previousScreenshotPath;

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

                RewardReceiptDialogObservationResult observation =
                    await _rewardReceiptDialogObserver.ObserveAsync(
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
                    return new WaitResult(
                        false,
                        "“一括完了”点击已经提交，" +
                        "但观察奖励领取弹窗时发生失败：" +
                        observation.Message,
                        latestScreenshotPath);
                }

                if (observation.IsVisible)
                {
                    return new WaitResult(
                        true,
                        "已经确认奖励领取弹窗。",
                        latestScreenshotPath);
                }
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return new WaitResult(
                false,
                $"“一括完了”点击已经提交，" +
                $"但在 {VerificationTimeout.TotalSeconds:0.#} 秒内" +
                $"没有确认奖励领取弹窗。",
                latestScreenshotPath);
        }
    }

    private async Task<WaitResult>
        WaitForUnavailableMissionPanelAsync(
            GameWindowInfo window,
            string? previousScreenshotPath,
            CancellationToken cancellationToken)
    {
        string? latestScreenshotPath =
            previousScreenshotPath;

        string latestState =
            "尚未取得有效的任务面板观察结果";

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
                    return new WaitResult(
                        false,
                        "“閉じる”点击已经提交，" +
                        "但重新观察任务面板时发生失败：" +
                        observation.Message,
                        latestScreenshotPath);
                }

                latestState = observation.IsMissionPanel
                    ? $"MissionPanel / " +
                      $"{observation.RewardAvailability}"
                    : "没有确认 MissionPanel";

                if (observation.IsMissionPanel &&
                    observation.RewardAvailability ==
                    MissionRewardAvailability.Unavailable)
                {
                    return new WaitResult(
                        true,
                        "已经确认任务面板恢复，" +
                        "并且没有剩余可领取奖励。",
                        latestScreenshotPath);
                }
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return new WaitResult(
                false,
                $"“閉じる”点击已经提交，" +
                $"但在 {VerificationTimeout.TotalSeconds:0.#} 秒内" +
                $"没有确认领取结束；" +
                $"最后状态：{latestState}。",
                latestScreenshotPath);
        }
    }

    private static MissionRewardClaimResult Failed(
        string message,
        string? screenshotPath)
    {
        return new MissionRewardClaimResult(
            MissionRewardClaimOutcome.Failed,
            message,
            screenshotPath);
    }

    /// <summary>
    /// 私有轮询结果，只在领取任务内部使用。
    /// </summary>
    private sealed record WaitResult(
        bool IsSuccess,
        string Message,
        string? ScreenshotPath);
}