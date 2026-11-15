

using HimeBot.App.FilePicking;
using HimeBot.App.Interaction;
using HimeBot.App.Observation.Home;
using HimeBot.App.Observation.Mission;
using HimeBot.App.Observation.Reward;
using HimeBot.App.Observation.Shared;
using HimeBot.App.ViewModels;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Platform.Windows.Input;
using HimeBot.Platform.Windows.Launching;
using HimeBot.Platform.Windows.ScreenCapture;
using HimeBot.Platform.Windows.Windowing;
using HimeBot.Vision.ColorAnalysis;
using HimeBot.Vision.ImageProcessing;
using HimeBot.Vision.Matching;
using HimeBot.Vision.Ocr;
using System.Windows;

namespace HimeBot.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var gameLaunchTargetPicker =
            new OpenFileDialogGameLaunchTargetPicker();

        var gameLauncher = new WindowsGameLauncher();

        var gameWindowLocator =
            new WindowsGameWindowLocator();

        var gameScreenShotCapture = new WindowsGameScreenshotCapture();

        var sendInputAdapter = new SendInputAdapter();

        var gameWindowController = new WindowsGameWindowController();

        var imagePreprocessor = new ImagePreprocessor();

        var templateMatcher = new TemplateMatcher();

        var regionColorAnalyzer =
            new RegionColorAnalyzer();

        var homeSceneRecognizer = new HomeSceneRecognizer();

        var missionPanelRecognizer =
            new MissionPanelRecognizer();

        var missionBackButtonRecognizer =
            new MissionBackButtonRecognizer();

        var rewardReceiptDialogRecognizer =
            new RewardReceiptDialogRecognizer();

        var missionRewardAvailabilityRecognizer =
            new MissionRewardAvailabilityRecognizer();

        var frameCapture =
            new KoihimeFrameCapture(
                gameScreenShotCapture);

        var homeSceneObserver =
            new HomeSceneObserver(
                frameCapture,
                templateMatcher,
                homeSceneRecognizer);

        var missionPanelObserver =
            new MissionPanelObserver(
                frameCapture,
                templateMatcher,
                missionPanelRecognizer,
                regionColorAnalyzer,
                missionRewardAvailabilityRecognizer);

        var missionBackButtonObserver =
            new MissionBackButtonObserver(
                frameCapture,
                imagePreprocessor,
                templateMatcher,
                missionBackButtonRecognizer);

        var rewardReceiptDialogObserver =
            new RewardReceiptDialogObserver(
                frameCapture,
                templateMatcher,
                rewardReceiptDialogRecognizer);

        var missionPanelOpener =
            new KoihimeMissionPanelOpener(
                homeSceneObserver,
                missionPanelObserver,
                sendInputAdapter);

        var missionRewardClaimer =
            new KoihimeMissionRewardClaimer(
                missionPanelObserver,
                rewardReceiptDialogObserver,
                sendInputAdapter);

        var missionHomeNavigator =
            new KoihimeMissionHomeNavigator(
                missionPanelObserver,
                missionBackButtonObserver,
                homeSceneObserver,
                sendInputAdapter);

        var textRecognizer = new TesseractTextRecognizer();

        var grainStockParser = new GrainStockParser();

        var mainViewModel = new MainViewModel(
            gameLaunchTargetPicker,
            gameLauncher,
            gameWindowLocator,
            gameScreenShotCapture,
            sendInputAdapter,
            gameWindowController,
            imagePreprocessor,
            templateMatcher,
            homeSceneRecognizer,
            homeSceneObserver,
            missionPanelObserver,
            rewardReceiptDialogObserver,
            missionPanelOpener,
            missionRewardClaimer,
            missionHomeNavigator,
            textRecognizer,
            grainStockParser);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

