using Microsoft.Win32;

namespace HimeBot.App.FilePicking;

/// <summary>
/// 文本对话框选择游戏程序
/// </summary>
public sealed class OpenFileDialogGameExecutablePicker
    : IGameExecutablePicker
{
    public string? PickExecutable()
    {

        var dialog = new OpenFileDialog
        {
            // 对话框标题
            Title = "选择恋姬大战游戏程序", 
            // 只显示名为 koisen.exe 的文件
            Filter = "恋姬大战游戏程序（koisen.exe）|koisen.exe",
            // 默认在文件名输入框中显示 koisen.exe
            FileName = "koisen.exe",
            // 不允许确认不存在的文件
            CheckFileExists = true,
            // 确保目录存在
            CheckPathExists = true,
            Multiselect = false
        };

        //true        用户确认选择
        // false / null  用户取消或关闭窗口
        bool? result = dialog.ShowDialog();

        return result == true ? dialog.FileName : null;
    }
}

