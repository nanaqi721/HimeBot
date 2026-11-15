

using HimeBot.App.FilePicking;
using HimeBot.App.Interaction;
using HimeBot.App.Observation;
using HimeBot.App.ViewModels;
using HimeBot.Game.Koihime.Perception;
using HimeBot.Platform.Windows.Input;
using HimeBot.Platform.Windows.Launching;
using HimeBot.Platform.Windows.ScreenCapture;
using HimeBot.Platform.Windows.Windowing;
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

        var homeSceneRecognizer = new HomeSceneRecognizer();

        var missionPanelRecognizer =
            new MissionPanelRecognizer();

        var koihimeObserver =
            new KoihimeObserver(
                gameScreenShotCapture,
                templateMatcher,
                homeSceneRecognizer,
                missionPanelRecognizer);

        var missionPanelOpener =
            new KoihimeMissionPanelOpener(
                koihimeObserver,
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
            koihimeObserver,
            missionPanelOpener,
            textRecognizer,
            grainStockParser);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

