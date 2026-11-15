using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HimeBot.App.FilePicking;
using HimeBot.App.Interaction;
using HimeBot.App.Observation.Home;
using HimeBot.App.Observation.Mission;
using HimeBot.App.Observation.Reward;
using HimeBot.Core.Launching;
using HimeBot.Core.Ports;
using HimeBot.Core.Windowing;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Platform.Windows.Input;
using HimeBot.Platform.Windows.ScreenCapture;
using HimeBot.Platform.Windows.Windowing;
using HimeBot.Vision.ImageProcessing;
using HimeBot.Vision.Matching;
using HimeBot.Vision.Ocr;
using System.IO;

namespace HimeBot.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const int GrainStockRegionX = 540;
    private const int GrainStockRegionY = 12;
    private const int GrainStockRegionWidth = 195;
    private const int GrainStockRegionHeight = 48;

    private readonly IGameLaunchTargetPicker _gameLaunchTargetPicker;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGameWindowLocator _gameWindowLocator;
    private readonly WindowsGameScreenshotCapture _gameScreenshotCapture;
    private readonly SendInputAdapter _sendInputAdapter;
    private readonly WindowsGameWindowController _gameWindowController;
    private readonly ImagePreprocessor _imagePreprocessor;
    private readonly TemplateMatcher _templateMatcher;
    private readonly HomeSceneRecognizer _homeSceneRecognizer;
    private readonly HomeSceneObserver _homeSceneObserver;
    private readonly MissionPanelObserver _missionPanelObserver;
    private readonly RewardReceiptDialogObserver
        _rewardReceiptDialogObserver;
    private readonly KoihimeMissionPanelOpener _missionPanelOpener;
    private readonly TesseractTextRecognizer _textRecognizer;
    private readonly GrainStockParser _grainStockParser;

    private string _selectedLaunchTargetPath = string.Empty;
    private string _statusMessage = "请选择 DMM 游戏快捷方式。";
    private string? _latestCapturePath;
    private string? _latestOcrRegionPath;
    private ClientSizeOption? _selectedClientSizeOption;
    private GameWindowInfo? _locatedGameWindow;

    public MainViewModel(
        IGameLaunchTargetPicker gameLaunchTargetPicker,
        IGameLauncher gameLauncher,
        IGameWindowLocator gameWindowLocator,
        WindowsGameScreenshotCapture gameScreenshotCapture,
        SendInputAdapter sendInputAdapter,
        WindowsGameWindowController gameWindowController,
        ImagePreprocessor imagePreprocessor,
        TemplateMatcher templateMatcher,
        HomeSceneRecognizer homeSceneRecognizer,
        HomeSceneObserver homeSceneObserver,
        MissionPanelObserver missionPanelObserver,
        RewardReceiptDialogObserver rewardReceiptDialogObserver,
        KoihimeMissionPanelOpener missionPanelOpener,
        TesseractTextRecognizer textRecognizer,
        GrainStockParser grainStockParser)
    {
        _gameLaunchTargetPicker = gameLaunchTargetPicker;
        _gameLauncher = gameLauncher;
        _gameWindowLocator = gameWindowLocator;
        _gameScreenshotCapture = gameScreenshotCapture;
        _sendInputAdapter = sendInputAdapter;
        _gameWindowController = gameWindowController;
        _imagePreprocessor = imagePreprocessor;
        _templateMatcher = templateMatcher;
        _homeSceneRecognizer = homeSceneRecognizer;
        _homeSceneObserver = homeSceneObserver;
        _missionPanelObserver = missionPanelObserver;
        _rewardReceiptDialogObserver =
            rewardReceiptDialogObserver;
        _missionPanelOpener = missionPanelOpener;
        _textRecognizer = textRecognizer;
        _grainStockParser = grainStockParser;

        ClientSizeOption defaultClientSize =
            new ClientSizeOption(
                1280,
                720);

        ClientSizeOptions =
            new ClientSizeOption[]
            {
                new ClientSizeOption(1024, 576),
                defaultClientSize,
                new ClientSizeOption(1360, 765),
                new ClientSizeOption(1366, 768),
                new ClientSizeOption(1440, 810),
                new ClientSizeOption(1600, 900),
                new ClientSizeOption(1920, 1080),
                new ClientSizeOption(2560, 1440)
            };

        _selectedClientSizeOption =
            defaultClientSize;

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

        ObserveHomeSceneCommand =
            new AsyncRelayCommand(
                ObserveHomeSceneAsync,
                CanObserveScene);

        ObserveMissionPanelCommand =
            new AsyncRelayCommand(
                ObserveMissionPanelAsync,
                CanObserveScene);

        ObserveRewardReceiptDialogCommand =
            new AsyncRelayCommand(
                ObserveRewardReceiptDialogAsync,
                CanObserveScene);

        OpenMissionPanelCommand =
            new AsyncRelayCommand(
                OpenMissionPanelAsync,
                CanOpenMissionPanel);

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

        CreateGrayscaleCopyCommand =
            new AsyncRelayCommand(
                CreateGrayscaleCopyAsync,
                CanCreateProcessedCopy);

        CreateBinaryCopyCommand =
            new AsyncRelayCommand(
                CreateBinaryCopyAsync,
                CanCreateProcessedCopy);

        CreateOcrRegionCopyCommand =
            new AsyncRelayCommand(
                CreateOcrRegionCopyAsync,
                CanCreateProcessedCopy);

        MatchHomeTemplateCommand =
            new AsyncRelayCommand(
                MatchHomeTemplateAsync,
                CanMatchHomeTemplate);

        RecognizeOcrRegionCommand =
            new AsyncRelayCommand(
                RecognizeOcrRegionAsync,
                CanRecognizeOcrRegion);
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

    public IReadOnlyList<ClientSizeOption> ClientSizeOptions
    {
        get;
    }

    public ClientSizeOption? SelectedClientSizeOption
    {
        get => _selectedClientSizeOption;

        set => SetProperty(
            ref _selectedClientSizeOption,
            value);
    }

    public IRelayCommand SelectLaunchTargetCommand { get; }

    public IRelayCommand LaunchGameCommand { get; }

    public IRelayCommand FindGameWindowCommand { get; }

    public IAsyncRelayCommand CaptureGameScreenshotCommand
    {
        get;
    }

    public IAsyncRelayCommand ObserveHomeSceneCommand { get; }

    public IAsyncRelayCommand ObserveMissionPanelCommand { get; }

    public IAsyncRelayCommand ObserveRewardReceiptDialogCommand { get; }

    public IAsyncRelayCommand OpenMissionPanelCommand { get; }

    public IRelayCommand TestMoveCursorCommand { get; }

    public IRelayCommand TestClickCursorCommand { get; }

    public IRelayCommand SetGameWindowTopmostCommand { get; }

    public IRelayCommand RemoveGameWindowTopmostCommand { get; }

    public IRelayCommand SetGameWindowClientSizeCommand { get; }

    public IAsyncRelayCommand CreateGrayscaleCopyCommand { get; }

    public IAsyncRelayCommand CreateBinaryCopyCommand { get; }

    public IAsyncRelayCommand CreateOcrRegionCopyCommand { get; }

    public IAsyncRelayCommand MatchHomeTemplateCommand { get; }

    public IAsyncRelayCommand RecognizeOcrRegionCommand { get; }

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

        ObserveHomeSceneCommand
            .NotifyCanExecuteChanged();

        ObserveMissionPanelCommand
            .NotifyCanExecuteChanged();

        ObserveRewardReceiptDialogCommand
            .NotifyCanExecuteChanged();

        OpenMissionPanelCommand
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

        SetLatestCapturePath(null);

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

        if (!result.IsSuccess)
        {
            StatusMessage = result.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(result.FilePath))
        {
            StatusMessage =
                "截图成功，但没有返回图片路径。";
            return;
        }

        SetLatestCapturePath(result.FilePath);

        StatusMessage =
            $"{result.Message} " +
            $"图片尺寸：{result.Width} × {result.Height}。";
    }

    private bool CanObserveScene()
    {
        return _locatedGameWindow is not null;
    }

    private async Task ObserveHomeSceneAsync(
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

        HomeSceneObservationResult result =
            await _homeSceneObserver.ObserveAsync(
                window,
                cancellationToken);

        ApplyObservationScreenshot(
            result.ScreenshotPath);

        StatusMessage =
            $"{result.Message} " +
            $"截图路径：{result.ScreenshotPath}。";
    }

    private async Task ObserveMissionPanelAsync(
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

        MissionPanelObservationResult result =
            await _missionPanelObserver.ObserveAsync(
                window,
                cancellationToken);

        ApplyObservationScreenshot(
            result.ScreenshotPath);

        StatusMessage =
            $"{result.Message} " +
            $"截图路径：{result.ScreenshotPath}。";
    }

    private async Task ObserveRewardReceiptDialogAsync(
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

        RewardReceiptDialogObservationResult result =
            await _rewardReceiptDialogObserver.ObserveAsync(
                window,
                cancellationToken);

        ApplyObservationScreenshot(
            result.ScreenshotPath);

        StatusMessage =
            $"{result.Message} " +
            $"截图路径：{result.ScreenshotPath}。";
    }

    private void ApplyObservationScreenshot(
        string? screenshotPath)
    {
        if (!string.IsNullOrWhiteSpace(screenshotPath))
        {
            SetLatestCapturePath(
                screenshotPath);
        }
    }

    private bool CanOpenMissionPanel()
    {
        return _locatedGameWindow is not null;
    }

    private async Task OpenMissionPanelAsync(
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

        MissionPanelOpenResult result =
            await _missionPanelOpener.OpenAsync(
                window,
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(
                result.ScreenshotPath))
        {
            SetLatestCapturePath(
                result.ScreenshotPath);
        }

        StatusMessage = result.Message;
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

        ClientSizeOption? selectedSize =
            SelectedClientSizeOption;

        if (selectedSize is null)
        {
            StatusMessage =
                "请选择客户区大小。";

            return;
        }

        GameWindowControlResult controlResult =
            _gameWindowController.SetClientSize(
                window,
                selectedSize.Width,
                selectedSize.Height);

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

        GameWindowInfo refreshedWindow =
            refreshResult.Window;

        SetLocatedGameWindow(
            refreshedWindow);

        StatusMessage = controlResult.IsSuccess
            ? $"{controlResult.Message} " +
              $"窗口信息已刷新。"
            : $"{controlResult.Message} " +
              $"当前实际客户区：" +
              $"{refreshedWindow.ClientWidth} × " +
              $"{refreshedWindow.ClientHeight}。";
    }

    private void SetLatestCapturePath(
        string? filePath)
    {
        _latestCapturePath = filePath;

        SetLatestOcrRegionPath(null);

        CreateGrayscaleCopyCommand
            .NotifyCanExecuteChanged();

        CreateBinaryCopyCommand
            .NotifyCanExecuteChanged();

        CreateOcrRegionCopyCommand
            .NotifyCanExecuteChanged();

        MatchHomeTemplateCommand
            .NotifyCanExecuteChanged();
    }

    private void SetLatestOcrRegionPath(
        string? filePath)
    {
        _latestOcrRegionPath = filePath;

        RecognizeOcrRegionCommand
            .NotifyCanExecuteChanged();
    }

    private bool CanCreateProcessedCopy()
    {
        return !string.IsNullOrWhiteSpace(
            _latestCapturePath);
    }

    private bool CanMatchHomeTemplate()
    {
        return !string.IsNullOrWhiteSpace(
            _latestCapturePath);
    }

    private async Task MatchHomeTemplateAsync(
        CancellationToken cancellationToken)
    {
        string? sourcePath =
            _latestCapturePath;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            StatusMessage =
                "请先保存一张客户区截图。";

            return;
        }

        string templatePath =
            Path.Combine(
                AppContext.BaseDirectory,
                "assets",
                "koihime",
                "templates",
                "home-settings-icon.png");

        TemplateMatchResult result =
            await _templateMatcher
                .FindBestMatchAsync(
                    sourcePath,
                    templatePath,
                    cancellationToken);

        if (!result.IsSuccess)
        {
            StatusMessage = result.Message;
            return;
        }

        SceneRecognitionResult sceneResult =
            _homeSceneRecognizer.Recognize(
                result.BestScore,
                result.X,
                result.Y);

        StatusMessage =
            $"场景识别结果：{sceneResult.Scene}；" +
            $"{sceneResult.Message} " +
            $"最佳分数：{sceneResult.Score:0.0000}；" +
            $"匹配位置：X={sceneResult.MatchX}，" +
            $"Y={sceneResult.MatchY}；" +
            $"模板尺寸：{result.Width} × {result.Height}。";
    }

    private async Task CreateGrayscaleCopyAsync(
        CancellationToken cancellationToken)
    {
        string? sourcePath =
            _latestCapturePath;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            StatusMessage =
                "请先保存一张客户区截图。";

            return;
        }

        string outputDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HimeBot",
                "processed");

        string outputFileName =
            $"{Path.GetFileNameWithoutExtension(sourcePath)}" +
            "-grayscale.png";

        string outputPath =
            Path.Combine(
                outputDirectory,
                outputFileName);

        ImageProcessingResult result =
            await _imagePreprocessor
                .CreateGrayscaleCopyAsync(
                    sourcePath,
                    outputPath,
                    cancellationToken);

        StatusMessage = result.Message;
    }

    private async Task CreateBinaryCopyAsync(
        CancellationToken cancellationToken)
    {
        string? sourcePath =
            _latestCapturePath;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            StatusMessage =
                "请先保存一张客户区截图。";

            return;
        }

        string outputDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HimeBot",
                "processed");

        string outputFileName =
            $"{Path.GetFileNameWithoutExtension(sourcePath)}" +
            "-binary.png";

        string outputPath =
            Path.Combine(
                outputDirectory,
                outputFileName);

        ImageProcessingResult result =
            await _imagePreprocessor
                .CreateBinaryCopyAsync(
                    sourcePath,
                    outputPath,
                    cancellationToken);

        StatusMessage = result.Message;
    }

    private async Task CreateOcrRegionCopyAsync(
        CancellationToken cancellationToken)
    {
        string? sourcePath =
            _latestCapturePath;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            StatusMessage =
                "请先保存一张客户区截图。";

            return;
        }

        SetLatestOcrRegionPath(null);

        string outputDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HimeBot",
                "processed");

        string outputFileName =
            $"{Path.GetFileNameWithoutExtension(sourcePath)}" +
            "-resource-roi.png";

        string outputPath =
            Path.Combine(
                outputDirectory,
                outputFileName);

        ImageProcessingResult result =
            await _imagePreprocessor
                .CreateRegionCopyAsync(
                    sourcePath,
                    outputPath,
                    GrainStockRegionX,
                    GrainStockRegionY,
                    GrainStockRegionWidth,
                    GrainStockRegionHeight,
                    cancellationToken);

        if (!result.IsSuccess)
        {
            StatusMessage = result.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(result.OutputPath))
        {
            StatusMessage =
                "资源区域裁剪成功，但没有返回输出路径。";

            return;
        }

        SetLatestOcrRegionPath(result.OutputPath);
        StatusMessage = result.Message;
    }

    private bool CanRecognizeOcrRegion()
    {
        return !string.IsNullOrWhiteSpace(
            _latestOcrRegionPath);
    }

    private async Task RecognizeOcrRegionAsync(
        CancellationToken cancellationToken)
    {
        string? imagePath =
            _latestOcrRegionPath;

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            StatusMessage =
                "请先裁剪资源数字区域。";

            return;
        }

        OcrRecognitionResult result =
            await _textRecognizer
                .RecognizeSingleLineAsync(
                    imagePath,
                    cancellationToken);

        if (!result.IsSuccess)
        {
            StatusMessage = result.Message;
            return;
        }

        string displayText =
            string.IsNullOrWhiteSpace(result.RawText)
                ? "（未识别到文字）"
                : result.RawText.ReplaceLineEndings(" ");

        GrainStockParseResult parseResult =
            _grainStockParser.Parse(result.RawText);

        if (!parseResult.IsSuccess ||
            parseResult.Stock is null)
        {
            StatusMessage =
                $"{result.Message} " +
                $"原始文字：{displayText}；" +
                $"平均置信度：{result.MeanConfidence:P1}；" +
                $"粮食解析失败：{parseResult.Message} " +
                $"规范化文字：{parseResult.NormalizedText}。";

            return;
        }

        StatusMessage =
            $"{result.Message} " +
            $"原始文字：{displayText}；" +
            $"平均置信度：{result.MeanConfidence:P1}；" +
            $"规范化文字：{parseResult.NormalizedText}；" +
            $"当前粮食：{parseResult.Stock.CurrentAmount}万；" +
            $"容量：{parseResult.Stock.Capacity}万。";
    }
}
