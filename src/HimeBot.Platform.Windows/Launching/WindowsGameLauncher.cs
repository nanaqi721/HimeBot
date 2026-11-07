

using HimeBot.Core.Launching;
using HimeBot.Core.Ports;
using System.ComponentModel;
using System.Diagnostics;

namespace HimeBot.Platform.Windows.Launching;

public sealed class WindowsGameLauncher : IGameLauncher
{
    public GameLaunchResult Launch(string executablePath)
    {
        // 判断输入路径是否为空
        if(string.IsNullOrWhiteSpace(executablePath))
        {
            return new GameLaunchResult(
                false,
                "请选择游戏程序。");
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(executablePath);
        }
        catch (Exception exception) when (
        // path是空字符串""
        exception is ArgumentException
        // 路径格式错误
        or NotSupportedException
        // 路径太长
        or PathTooLongException)
        {
            return new GameLaunchResult(
                false,
                $"游戏路径无效：{exception.Message}");
        }

        // 判断路径是否存在
        if(!File.Exists(fullPath)) 
        { 
            return new GameLaunchResult(
                false,
                $"找不到游戏程序：{fullPath}");
        }

        string? workingDirectory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(workingDirectory)) 
        {
            return new GameLaunchResult(
                false,
                $"无法确定游戏程序所在目录。");
        }

        var startInfo = new ProcessStartInfo 
        { 
            FileName = fullPath, // 启动路径
            WorkingDirectory = workingDirectory, // 工作目录
            UseShellExecute = true // 是否使用shell打开
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return new GameLaunchResult(
                    false,
                    "windows 未能创建游戏进程。");
            }

            return new GameLaunchResult(
                true,
                "游戏启动请求已发送。");
        }
        catch (Win32Exception exception)
        {
            return new GameLaunchResult(
                false,
                $"启动游戏失败：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return new GameLaunchResult(
                false,
                $"启动游戏失败：{exception.Message}");
        }


    }
}

