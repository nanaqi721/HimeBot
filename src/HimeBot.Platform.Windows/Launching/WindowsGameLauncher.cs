using HimeBot.Core.Launching;
using HimeBot.Core.Ports;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace HimeBot.Platform.Windows.Launching;

public sealed class WindowsGameLauncher : IGameLauncher
{
    public GameLaunchResult Launch(string launchTargetPath)
    {
        if (string.IsNullOrWhiteSpace(launchTargetPath))
        {
            return new GameLaunchResult(
                false,
                "请选择游戏启动目标。");
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(launchTargetPath);
        }
        //ArgumentException：路径参数异常
        // NotSupportedException：路径格式不支持
        // PathTooLongException：路径太长
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return new GameLaunchResult(
                false,
                $"游戏启动目标路径无效：{exception.Message}");
        }

        if (!File.Exists(fullPath))
        {
            return new GameLaunchResult(
                false,
                $"找不到游戏启动目标：{fullPath}");
        }

        string? workingDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return new GameLaunchResult(
                false,
                "无法确定游戏启动目标所在目录。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fullPath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);

            // Shell 可能把请求交给已经运行的 DMM 进程，此时不会返回新的 Process 对象。
            // 没有抛出异常只表示 Windows 已接受启动请求，不表示游戏已经完成启动。
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
        // 启动配置或当前操作状态不允许执行，例如 ProcessStartInfo 配置不完整。
        catch (InvalidOperationException exception)
        {
            return new GameLaunchResult(
                false,
                $"启动游戏失败：{exception.Message}");
        }
    }
}
