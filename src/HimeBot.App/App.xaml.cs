

using HimeBot.App.FilePicking;
using HimeBot.App.ViewModels;
using HimeBot.Platform.Windows.Launching;
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

        var mainViewModel = new MainViewModel(
            gameLaunchTargetPicker,
            gameLauncher);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

