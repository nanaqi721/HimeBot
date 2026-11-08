using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HimeBot.App.FilePicking;
using HimeBot.Core.Launching;
using HimeBot.Core.Ports;
using System.IO;

namespace HimeBot.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IGameLaunchTargetPicker _gameLaunchTargetPicker;
    private readonly IGameLauncher _gameLauncher;

    private string _selectedLaunchTargetPath = string.Empty;
    private string _statusMessage = "请选择 DMM 游戏快捷方式。";

    public MainViewModel(
        IGameLaunchTargetPicker gameLaunchTargetPicker,
        IGameLauncher gameLauncher)
    {
        _gameLaunchTargetPicker = gameLaunchTargetPicker;
        _gameLauncher = gameLauncher;

        SelectLaunchTargetCommand =
            new RelayCommand(SelectLaunchTarget);

        LaunchGameCommand =
            new RelayCommand(
                LaunchGame,
                CanLaunchGame);
    }

    public string SelectedLaunchTargetPath
    {
        get => _selectedLaunchTargetPath;

        private set
        {
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

    public IRelayCommand SelectLaunchTargetCommand { get; }

    public IRelayCommand LaunchGameCommand { get; }

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
}
