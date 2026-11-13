using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.Windowing;

public sealed class WindowsGameWindowController
{
    public GameWindowControlResult SetTopmost(
        GameWindowInfo window,
        bool isTopmost)
    {
        ArgumentNullException.ThrowIfNull(window);

        try
        {
            using Process process =
                Process.GetProcessById(
                    window.ProcessId);

            process.Refresh();

            IntPtr windowHandle =
                process.MainWindowHandle;

            if (windowHandle == IntPtr.Zero)
            {
                return new GameWindowControlResult(
                    false,
                    "游戏进程仍在运行，但主窗口不可用。");
            }

            IntPtr insertAfter = isTopmost
                ? NativeMethods.HwndTopmost
                : NativeMethods.HwndNotTopmost;

            uint flags =
                NativeMethods.SwpNoMove |
                NativeMethods.SwpNoSize |
                NativeMethods.SwpNoActivate;

            if (!NativeMethods.SetWindowPos(
                    windowHandle,
                    insertAfter,
                    0,
                    0,
                    0,
                    0,
                    flags))
            {
                return CreateLastWin32Failure(
                    isTopmost
                        ? "设置游戏窗口置顶失败"
                        : "取消游戏窗口置顶失败");
            }

            return new GameWindowControlResult(
                true,
                isTopmost
                    ? "游戏窗口已设置为置顶。"
                    : "游戏窗口已取消置顶。");
        }
        catch (ArgumentException exception)
        {
            return new GameWindowControlResult(
                false,
                $"找不到游戏进程：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return new GameWindowControlResult(
                false,
                $"无法访问游戏进程：{exception.Message}");
        }
        catch (Win32Exception exception)
        {
            return new GameWindowControlResult(
                false,
                $"Windows 窗口操作失败：{exception.Message}");
        }
    }

    private static GameWindowControlResult
        CreateLastWin32Failure(
            string operation)
    {
        int errorCode =
            Marshal.GetLastWin32Error();

        string errorMessage = errorCode == 0
            ? "Windows 没有提供错误码。"
            : new Win32Exception(errorCode).Message;

        return new GameWindowControlResult(
            false,
            $"{operation}：{errorMessage}");
    }
}