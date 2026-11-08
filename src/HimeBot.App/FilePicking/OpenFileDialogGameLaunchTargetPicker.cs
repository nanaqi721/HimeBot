using Microsoft.Win32;

namespace HimeBot.App.FilePicking;

/// <summary>
/// 使用文件对话框选择游戏启动目标。
/// </summary>
public sealed class OpenFileDialogGameLaunchTargetPicker
    : IGameLaunchTargetPicker
{
    public string? PickLaunchTarget()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 DMM 游戏快捷方式",
            Filter = "DMM 游戏快捷方式 (*.url)|*.url",
            InitialDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory),
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            ValidateNames = true
        };

        bool? result = dialog.ShowDialog();

        return result == true
            ? dialog.FileName
            : null;
    }
}
