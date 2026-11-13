using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HimeBot.App.FilePicking;
using HimeBot.Core.Launching;
using HimeBot.Core.Ports;
using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Input;
using HimeBot.Platform.Windows.ScreenCapture;
using HimeBot.Platform.Windows.Windowing;
using System.IO;

namespace HimeBot.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IGameLaunchTargetPicker _gameLaunchTargetPicker;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGameWindowLocator _gameWindowLocator;
    private readonly WindowsGameScreenshotCapture _gameScreenshotCapture;
    private readonly SendInputAdapter _sendInputAdapter;
    private readonly WindowsGameWindowController _gameWindowController;

    private string _selectedLaunchTargetPath = string.Empty;
    private string _statusMessage = "请选择 DMM 游戏快捷方式。";
    private string _targetClientWidthText = "1280";
    private string _targetClientHeightText = "720";
    private GameWindowInfo? _locatedGameWindow;

    public MainViewModel(
        IGameLaunchTargetPicker gameLaunchTargetPicker,
        IGameLauncher gameLauncher,
        IGameWindowLocator gameWindowLocator,
        WindowsGameScreenshotCapture gameScreenshotCapture,
        SendInputAdapter sendInputAdapter,
        WindowsGameWindowController gameWindowController)
    {
        _gameLaunchTargetPicker = gameLaunchTargetPicker;
        _gameLauncher = gameLauncher;
        _gameWindowLocator = gameWindowLocator;
        _gameScreenshotCapture = gameScreenshotCapture;
        _sendInputAdapter = sendInputAdapter;
        _gameWindowController = gameWindowController;

        SelectLaunchTargetCommand =
            new RelayCommand(SelectLaunchTarget);

        LaunchGameCommand =
            new RelayCommand(
                LaunchGame,
                CanLaunchGame);

        FindGameWindowCommand =
            new RelayCommand(FindGameWindow);

        CaptureGameScreenshotCommand =
            new AsyncRelayCommand(
                CaptureGameScreenshotAsync,
                CanCaptureGameScreenshot);

        TestMoveCursorCommand =
            new RelayCommand(
                TestMoveCursor,
                CanUseGameInput);

        TestClickCursorCommand =
            new RelayCommand(
                TestClickCursor,
                CanUseGameInput);

        SetGameWindowTopmostCommand =
            new RelayCommand(
                SetGameWindowTopmost,
                CanControlGameWindow);

        RemoveGameWindowTopmostCommand =
            new RelayCommand(
                RemoveGameWindowTopmost,
                CanControlGameWindow);

        SetGameWindowClientSizeCommand =
            new RelayCommand(
                SetGameWindowClientSize,
                CanControlGameWindow);
    }

    public string SelectedLaunchTargetPath
    {
        get => _selectedLaunchTargetPath;

        private set
        {
            // SetProperty：比较旧值与新值，若不同则更新，返回true
            if (SetProperty(
                    ref _selectedLaunchTargetPath,
                    value))
            {
                LaunchGameCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public string TargetClientWidthText
    {
        get => _targetClientWidthText;

        set => SetProperty(
            ref _targetClientWidthText,
            value);
    }

    public string TargetClientHeightText
    {
        get => _targetClientHeightText;

        set => SetProperty(
            ref _targetClientHeightText,
            value);
    }

    public IRelayCommand SelectLaunchTargetCommand { get; }

    public IRelayCommand LaunchGameCommand { get; }

    public IRelayCommand FindGameWindowCommand { get; }

    public IAsyncRelayCommand CaptureGameScreenshotCommand
    {
        get;
    }

    public IRelayCommand TestMoveCursorCommand { get; }

    public IRelayCommand TestClickCursorCommand { get; }

    public IRelayCommand SetGameWindowTopmostCommand { get; }

    public IRelayCommand RemoveGameWindowTopmostCommand { get; }

    public IRelayCommand SetGameWindowClientSizeCommand { get; }


    private void SelectLaunchTarget()
    {
        string? selectedPath =
            _gameLaunchTargetPicker.PickLaunchTarget();

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        if (!IsDmmGameShortcut(selectedPath))
        {
            SelectedLaunchTargetPath = string.Empty;
            StatusMessage =
                "选择的文件不是有效的 DMM 游戏快捷方式。";

            return;
        }

        SelectedLaunchTargetPath = selectedPath;
        StatusMessage = "已选择 DMM 游戏快捷方式。";
    }

    private bool CanLaunchGame()
    {
        return !string.IsNullOrWhiteSpace(
            SelectedLaunchTargetPath);
    }

    private void LaunchGame()
    {
        GameLaunchResult result =
            _gameLauncher.Launch(
                SelectedLaunchTargetPath);

        StatusMessage = result.Message;
    }

    private static bool IsDmmGameShortcut(
        string launchTargetPath)
    {
        if (!string.Equals(
                Path.GetExtension(launchTargetPath),
                ".url",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            string? urlLine = File
                .ReadLines(launchTargetPath)
                .FirstOrDefault(line =>
                    line.StartsWith(
                        "URL=",
                        StringComparison.OrdinalIgnoreCase));

            if (urlLine is null)
            {
                return false;
            }

            string rawUri = urlLine["URL=".Length..].Trim();

            return Uri.TryCreate(
                       rawUri,
                       UriKind.Absolute,
                       out Uri? uri)
                   && string.Equals(
                       uri.Scheme,
                       "dmmgameplayer",
                       StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void FindGameWindow()
    {
        GameWindowLookupResult result =
            _gameWindowLocator.FindByProcessName(
                "koisen");

        if (!result.IsSuccess || result.Window is null)
        {
            SetLocatedGameWindow(null);
            StatusMessage = result.Message;
            return;
        }

        GameWindowInfo window = result.Window;
        SetLocatedGameWindow(window);

        StatusMessage =
            $"已找到游戏窗口。" +
            $"进程 ID：{window.ProcessId}；" +
            $"标题：{window.Title}；" +
            $"客户区：{window.ClientWidth} × {window.ClientHeight}。";
    }

    private void SetLocatedGameWindow(
    GameWindowInfo? window)
    {
        _locatedGameWindow = window;

        // 再次调用CanCaptureGameScreenshot方法，判断是否能点击按钮
        CaptureGameScreenshotCommand
            .NotifyCanExecuteChanged();

        TestMoveCursorCommand
            .NotifyCanExecuteChanged();

        TestClickCursorCommand
            .NotifyCanExecuteChanged();

        SetGameWindowTopmostCommand
            .NotifyCanExecuteChanged();

        RemoveGameWindowTopmostCommand
            .NotifyCanExecuteChanged();

        SetGameWindowClientSizeCommand
            .NotifyCanExecuteChanged();
    }

    private bool CanCaptureGameScreenshot()
    {
        return _locatedGameWindow is not null;
    }

    private async Task CaptureGameScreenshotAsync(
        CancellationToken cancellationToken)
    {
        GameWindowInfo? window =
            _locatedGameWindow;

        if (window is null)
        {
            StatusMessage =
                "请先查找游戏窗口。";

            return;
        }

        string captureDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    // 用户LocalData目录
                    Environment.SpecialFolder.LocalApplicationData),
                "HimeBot",
                "captures");

        string fileName =
            $"koihime-" +
            $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";

        string outputPath =
            Path.Combine(
                captureDirectory,
                fileName);

        GameScreenshotCaptureResult result =
            await _gameScreenshotCapture
                .CaptureToPngAsync(
                    window,
                    outputPath,
                    cancellationToken);

        StatusMessage = result.IsSuccess
            ? $"{result.Message} " +
              $"图片尺寸：{result.Width} × {result.Height}。"
            : result.Message;
    }

    private bool CanUseGameInput()
    {
        return _locatedGameWindow is not null;
    }

    private void TestMoveCursor()
    {
        GameWindowInfo? window =
            _locatedGameWindow;

        if (window is null)
        {
            StatusMessage =
                "请先查找游戏窗口。";

            return;
        }

        int clientX =
            window.ClientWidth / 2;

        int clientY =
            window.ClientHeight / 2;

        GameInputResult result =
            _sendInputAdapter
                .MoveCursorToClientPoint(
                    window,
                    clientX,
                    clientY);

        StatusMessage = result.Message;
    }

    private void TestClickCursor()
    {
        GameWindowInfo? window =
            _locatedGameWindow;

        if (window is null)
        {
            StatusMessage =
                "请先查找游戏窗口。";

            return;
        }

        int clientX =
            window.ClientWidth / 2;

        int clientY =
            window.ClientHeight / 2;

        GameInputResult result =
            _sendInputAdapter
                .ClickClientPoint(
                    window,
                    clientX,
                    clientY);

        StatusMessage = result.Message;
    }

    private bool CanControlGameWindow()
    {
        return _locatedGameWindow is not null;
    }

    private void SetGameWindowTopmost()
    {
        ChangeGameWindowTopmost(true);
    }

    private void RemoveGameWindowTopmost()
    {
        ChangeGameWindowTopmost(false);
    }

    private void ChangeGameWindowTopmost(
        bool isTopmost)
    {
        GameWindowInfo? window =
            _locatedGameWindow;

        if (window is null)
        {
            StatusMessage =
                "请先查找游戏窗口。";

            return;
        }

        GameWindowControlResult result =
            _gameWindowController.SetTopmost(
                window,
                isTopmost);

        StatusMessage = result.Message;
    }

    private void SetGameWindowClientSize()
    {
        GameWindowInfo? window =
            _locatedGameWindow;

        if (window is null)
        {
            StatusMessage =
                "请先查找游戏窗口。";

            return;
        }

        if (!int.TryParse(
                TargetClientWidthText,
                out int clientWidth))
        {
            StatusMessage =
                "客户区宽度必须是整数。";

            return;
        }

        if (!int.TryParse(
                TargetClientHeightText,
                out int clientHeight))
        {
            StatusMessage =
                "客户区高度必须是整数。";

            return;
        }

        GameWindowControlResult controlResult =
            _gameWindowController.SetClientSize(
                window,
                clientWidth,
                clientHeight);

        if (!controlResult.IsSuccess)
        {
            StatusMessage = controlResult.Message;
            return;
        }

        GameWindowLookupResult refreshResult =
            _gameWindowLocator.FindByProcessName(
                "koisen");

        if (!refreshResult.IsSuccess ||
            refreshResult.Window is null)
        {
            SetLocatedGameWindow(null);

            StatusMessage =
                $"{controlResult.Message} " +
                $"但刷新窗口信息失败：" +
                $"{refreshResult.Message}";

            return;
        }

        SetLocatedGameWindow(
            refreshResult.Window);

        StatusMessage =
            $"{controlResult.Message} " +
            $"窗口信息已刷新。";
    }
}
