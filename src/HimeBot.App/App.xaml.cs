

using HimeBot.App.FilePicking;
using HimeBot.App.ViewModels;
using System.Windows;

namespace HimeBot.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var gameExecutablePicker = new OpenFileDialogGameExecutablePicker();
        var mainViewModel = new MainViewModel(gameExecutablePicker);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

