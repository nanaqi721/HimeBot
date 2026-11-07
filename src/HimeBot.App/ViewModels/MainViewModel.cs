

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HimeBot.App.FilePicking;
using System.IO;

namespace HimeBot.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{ 
    private const string ExpectedExecutableName = "koisen.exe";

    private readonly IGameExecutablePicker _gameExecutablePicker;

    private string _selectedExecutablePath = string.Empty;
    private string _statusMessage = "请选择 koisen.exe";

    public MainViewModel(IGameExecutablePicker gameExecutablePicker)
    {
        _gameExecutablePicker = gameExecutablePicker;

        SelectExecutableCommand = new RelayCommand(SelectExecutable);
        
    }

    public string SelectedExecutablePath
    {
        get => _selectedExecutablePath;
        private set => SetProperty(
            ref _selectedExecutablePath,
            value);
    }
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public IRelayCommand SelectExecutableCommand { get; }

    private void SelectExecutable()
    {
        string? selectedPath =
            _gameExecutablePicker.PickExecutable();

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        string selectedFileName =
            Path.GetFileName(selectedPath);

        if (!string.Equals(
                selectedFileName,
                ExpectedExecutableName,
                StringComparison.OrdinalIgnoreCase))
        {
            SelectedExecutablePath = string.Empty;
            StatusMessage =
                $"请选择名为 {ExpectedExecutableName} 的游戏程序。";

            return;
        }

        SelectedExecutablePath = selectedPath;
        StatusMessage = $"已选择 {ExpectedExecutableName}。";
    }
}
