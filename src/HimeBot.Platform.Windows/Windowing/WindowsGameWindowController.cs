using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.Windowing;

public sealed class WindowsGameWindowController
{

    private const int MinimumClientWidth = 640;
    private const int MaximumClientWidth = 3840;

    private const int MinimumClientHeight = 360;
    private const int MaximumClientHeight = 2160;
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

    public GameWindowControlResult SetClientSize(
    GameWindowInfo window,
    int clientWidth,
    int clientHeight)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (clientWidth < MinimumClientWidth ||
            clientWidth > MaximumClientWidth)
        {
            return new GameWindowControlResult(
                false,
                $"客户区宽度必须在 " +
                $"{MinimumClientWidth}～" +
                $"{MaximumClientWidth} 之间。");
        }

        if (clientHeight < MinimumClientHeight ||
            clientHeight > MaximumClientHeight)
        {
            return new GameWindowControlResult(
                false,
                $"客户区高度必须在 " +
                $"{MinimumClientHeight}～" +
                $"{MaximumClientHeight} 之间。");
        }

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

            if (!NativeMethods.GetWindowRect(
                    windowHandle,
                    out NativeRect windowRectangle))
            {
                return CreateLastWin32Failure(
                    "读取游戏窗口尺寸失败");
            }

            if (!NativeMethods.GetClientRect(
                    windowHandle,
                    out NativeRect clientRectangle))
            {
                return CreateLastWin32Failure(
                    "读取游戏客户区尺寸失败");
            }

            int currentWindowWidth =
                windowRectangle.Right -
                windowRectangle.Left;

            int currentWindowHeight =
                windowRectangle.Bottom -
                windowRectangle.Top;

            int currentClientWidth =
                clientRectangle.Right -
                clientRectangle.Left;

            int currentClientHeight =
                clientRectangle.Bottom -
                clientRectangle.Top;

            if (currentWindowWidth <= 0 ||
                currentWindowHeight <= 0 ||
                currentClientWidth <= 0 ||
                currentClientHeight <= 0)
            {
                return new GameWindowControlResult(
                    false,
                    "游戏窗口当前尺寸无效。");
            }

            int nonClientWidth =
                currentWindowWidth -
                currentClientWidth;

            int nonClientHeight =
                currentWindowHeight -
                currentClientHeight;

            if (nonClientWidth < 0 ||
                nonClientHeight < 0)
            {
                return new GameWindowControlResult(
                    false,
                    "无法计算游戏窗口的非客户区尺寸。");
            }

            int targetWindowWidth =
                clientWidth +
                nonClientWidth;

            int targetWindowHeight =
                clientHeight +
                nonClientHeight;

            uint flags =
                NativeMethods.SwpNoMove |
                NativeMethods.SwpNoActivate |
                NativeMethods.SwpNoZOrder;

            if (!NativeMethods.SetWindowPos(
                    windowHandle,
                    IntPtr.Zero,
                    0,
                    0,
                    targetWindowWidth,
                    targetWindowHeight,
                    flags))
            {
                return CreateLastWin32Failure(
                    "设置游戏客户区尺寸失败");
            }

            if (!NativeMethods.GetClientRect(
                    windowHandle,
                    out NativeRect updatedClientRectangle))
            {
                return CreateLastWin32Failure(
                    "验证游戏客户区尺寸失败");
            }

            int actualClientWidth =
                updatedClientRectangle.Right -
                updatedClientRectangle.Left;

            int actualClientHeight =
                updatedClientRectangle.Bottom -
                updatedClientRectangle.Top;

            if (actualClientWidth != clientWidth ||
                actualClientHeight != clientHeight)
            {
                return new GameWindowControlResult(
                    false,
                    $"游戏没有接受目标客户区尺寸。" +
                    $"目标：{clientWidth} × {clientHeight}；" +
                    $"实际：{actualClientWidth} × " +
                    $"{actualClientHeight}。");
            }

            return new GameWindowControlResult(
                true,
                $"游戏客户区已调整为 " +
                $"{actualClientWidth} × " +
                $"{actualClientHeight}。");
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