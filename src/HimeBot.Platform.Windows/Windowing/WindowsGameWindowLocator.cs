using HimeBot.Core.Ports;
using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.Windowing;

public sealed class WindowsGameWindowLocator
    : IGameWindowLocator
{
    public GameWindowLookupResult FindByProcessName(
        string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return new GameWindowLookupResult(
                false,
                "进程名称不能为空。",
                null);
        }

        Process[] processes;

        try
        {
            processes =
                Process.GetProcessesByName(processName);
        }
        catch (InvalidOperationException exception)
        {
            return new GameWindowLookupResult(
                false,
                $"无法查询游戏进程：{exception.Message}",
                null);
        }

        if (processes.Length == 0)
        {
            return new GameWindowLookupResult(
                false,
                $"没有找到进程：{processName}",
                null);
        }

        var candidates = new List<WindowCandidate>();

        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    process.Refresh();

                    // 获取窗口句柄
                    IntPtr windowHandle =
                        process.MainWindowHandle;

                    if (windowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    candidates.Add(
                        new WindowCandidate(
                            process.Id,
                            process.MainWindowTitle,
                            windowHandle));
                }
                catch (InvalidOperationException)
                {
                    // 查询期间进程可能已经退出。
                }
                catch (Win32Exception)
                {
                    // 当前进程可能无法访问。
                }
            }
        }

        if (candidates.Count == 0)
        {
            return new GameWindowLookupResult(
                false,
                "已找到游戏进程，但主窗口尚未就绪。",
                null);
        }

        if (candidates.Count > 1)
        {
            return new GameWindowLookupResult(
                false,
                $"找到了 {candidates.Count} 个游戏窗口，无法确定目标。",
                null);
        }

        WindowCandidate candidate = candidates[0];

        if (!NativeMethods.GetClientRect(
                candidate.Handle,
                out NativeRect clientRectangle))
        {
            int errorCode =
                Marshal.GetLastWin32Error();

            string errorMessage =
                new Win32Exception(errorCode).Message;

            return new GameWindowLookupResult(
                false,
                $"读取游戏客户区失败：{errorMessage}",
                null);
        }

        int clientWidth =
            clientRectangle.Right - clientRectangle.Left;

        int clientHeight =
            clientRectangle.Bottom - clientRectangle.Top;

        if (clientWidth <= 0 || clientHeight <= 0)
        {
            return new GameWindowLookupResult(
                false,
                "游戏客户区尺寸无效。",
                null);
        }

        var window = new GameWindowInfo(
            candidate.ProcessId,
            candidate.Title,
            clientWidth,
            clientHeight);

        return new GameWindowLookupResult(
            true,
            $"已找到游戏窗口：{clientWidth} × {clientHeight}",
            window);
    }

    private sealed record WindowCandidate(
        int ProcessId,
        string Title,
        IntPtr Handle);
}