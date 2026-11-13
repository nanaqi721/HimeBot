using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.Input;

public sealed class SendInputAdapter
{
    public GameInputResult MoveCursorToClientPoint(
        GameWindowInfo window,
        int clientX,
        int clientY)
    {
        ArgumentNullException.ThrowIfNull(window);

        try
        {
            using Process process =
                Process.GetProcessById(window.ProcessId);

            process.Refresh();

            IntPtr windowHandle =
                process.MainWindowHandle;

            if (windowHandle == IntPtr.Zero)
            {
                return new GameInputResult(
                    false,
                    "游戏进程仍在运行，但主窗口不可用。");
            }

            if (!NativeMethods.GetClientRect(
                    windowHandle,
                    out NativeRect clientRectangle))
            {
                return CreateLastWin32Failure(
                    "读取游戏客户区失败");
            }

            int clientWidth =
                clientRectangle.Right -
                clientRectangle.Left;

            int clientHeight =
                clientRectangle.Bottom -
                clientRectangle.Top;

            if (clientWidth <= 0 ||
                clientHeight <= 0)
            {
                return new GameInputResult(
                    false,
                    "游戏客户区尺寸无效。");
            }

            if (clientX < 0 ||
                clientY < 0 ||
                clientX >= clientWidth ||
                clientY >= clientHeight)
            {
                return new GameInputResult(
                    false,
                    $"目标客户区坐标超出范围：" +
                    $"({clientX}, {clientY})；" +
                    $"客户区为 {clientWidth} × {clientHeight}。");
            }

            var screenPoint = new NativePoint
            {
                X = clientX,
                Y = clientY
            };

            if (!NativeMethods.ClientToScreen(
                    windowHandle,
                    ref screenPoint))
            {
                return CreateLastWin32Failure(
                    "转换客户区坐标失败");
            }

            // 获取虚拟桌面最左最上，长度宽度
            int virtualLeft =
                NativeMethods.GetSystemMetrics(
                    NativeMethods.SmXVirtualScreen);

            int virtualTop =
                NativeMethods.GetSystemMetrics(
                    NativeMethods.SmYVirtualScreen);

            int virtualWidth =
                NativeMethods.GetSystemMetrics(
                    NativeMethods.SmCxVirtualScreen);

            int virtualHeight =
                NativeMethods.GetSystemMetrics(
                    NativeMethods.SmCyVirtualScreen);

            if (virtualWidth <= 1 ||
                virtualHeight <= 1)
            {
                return new GameInputResult(
                    false,
                    "无法取得有效的虚拟桌面尺寸。");
            }

            // 计算该点相对于整个虚拟屏幕的位置
            // 单显示器时，虚拟桌面通常就是该显示器；
            // 多显示器时，虚拟桌面是包住所有显示器的整体矩形，
            // 其起点和大小会随分辨率、相对位置及排列方式变化。
            long relativeX =
                (long)screenPoint.X - virtualLeft;

            long relativeY =
                (long)screenPoint.Y - virtualTop;

            if (relativeX < 0 ||
                relativeY < 0 ||
                relativeX >= virtualWidth ||
                relativeY >= virtualHeight)
            {
                return new GameInputResult(
                    false,
                    $"目标屏幕坐标不在虚拟桌面内：" +
                    $"({screenPoint.X}, {screenPoint.Y})。");
            }

            // 对坐标进行归一化
            int normalizedX =
                (int)(
                    relativeX * 65535 /
                    (virtualWidth - 1));

            int normalizedY =
                (int)(
                    relativeY * 65535 /
                    (virtualHeight - 1));

            var input = new NativeInput
            {
                Type = NativeMethods.InputMouse,
                Data = new NativeInputUnion
                {
                    Mouse = new NativeMouseInput
                    {
                        X = normalizedX,
                        Y = normalizedY,
                        MouseData = 0,
                        Flags =
                            NativeMethods.MouseEventMove |
                            NativeMethods.MouseEventAbsolute |
                            NativeMethods.MouseEventVirtualDesk,
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };

            NativeInput[] inputs = new NativeInput[] { input };

            uint insertedCount =
                NativeMethods.SendInput(
                    (uint)inputs.Length,
                    inputs,
                    Marshal.SizeOf<NativeInput>());

            if (insertedCount != inputs.Length)
            {
                return CreateLastWin32Failure(
                    $"鼠标移动事件提交不完整，" +
                    $"只提交了 {insertedCount} 个事件");
            }

            return new GameInputResult(
                true,
                $"Windows 已接受鼠标移动事件。" +
                $"客户区坐标：({clientX}, {clientY})；" +
                $"屏幕坐标：({screenPoint.X}, {screenPoint.Y})。");
        }
        catch (ArgumentException exception)
        {
            return new GameInputResult(
                false,
                $"找不到游戏进程：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return new GameInputResult(
                false,
                $"无法访问游戏进程：{exception.Message}");
        }
        catch (Win32Exception exception)
        {
            return new GameInputResult(
                false,
                $"Windows 输入操作失败：{exception.Message}");
        }
    }

    public GameInputResult ClickClientPoint(
    GameWindowInfo window,
    int clientX,
    int clientY)
    {
        GameInputResult moveResult =
            MoveCursorToClientPoint(
                window,
                clientX,
                clientY);

        if (!moveResult.IsSuccess)
        {
            return moveResult;
        }

        var mouseDownInput = new NativeInput
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeInputUnion
            {
                Mouse = new NativeMouseInput
                {
                    X = 0,
                    Y = 0,
                    MouseData = 0,
                    Flags =
                        NativeMethods.MouseEventLeftDown,
                    Time = 0,
                    ExtraInfo = UIntPtr.Zero
                }
            }
        };

        var mouseUpInput = new NativeInput
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeInputUnion
            {
                Mouse = new NativeMouseInput
                {
                    X = 0,
                    Y = 0,
                    MouseData = 0,
                    Flags =
                        NativeMethods.MouseEventLeftUp,
                    Time = 0,
                    ExtraInfo = UIntPtr.Zero
                }
            }
        };

        NativeInput[] inputs =
            new NativeInput[]
            {
            mouseDownInput,
            mouseUpInput
            };

        uint insertedCount =
            NativeMethods.SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<NativeInput>());

        if (insertedCount != (uint)inputs.Length)
        {
            return CreateLastWin32Failure(
                $"鼠标点击事件提交不完整，" +
                $"只提交了 {insertedCount} 个事件");
        }

        return new GameInputResult(
            true,
            $"Windows 已接受鼠标左键点击事件。" +
            $"客户区坐标：({clientX}, {clientY})。");
    }

    private static GameInputResult CreateLastWin32Failure(
        string operation)
    {
        int errorCode =
            Marshal.GetLastWin32Error();

        string errorMessage = errorCode == 0
            ? "Windows 没有提供错误码，输入可能受到权限级别限制。"
            : new Win32Exception(errorCode).Message;

        return new GameInputResult(
            false,
            $"{operation}：{errorMessage}");
    }
}