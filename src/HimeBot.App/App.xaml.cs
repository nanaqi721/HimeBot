

using HimeBot.App.FilePicking;
using HimeBot.App.ViewModels;
using HimeBot.Platform.Windows.Launching;
using HimeBot.Platform.Windows.Windowing;
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

        var mainViewModel = new MainViewModel(
            gameLaunchTargetPicker,
            gameLauncher,
            gameWindowLocator);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

