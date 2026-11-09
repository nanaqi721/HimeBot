using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HimeBot.Platform.Windows.ScreenCapture;

public sealed class WindowsGameScreenshotCapture
{
    public Task<GameScreenshotCaptureResult> CaptureToPngAsync(
        GameWindowInfo window,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Task.FromResult(
                Failure("截图输出路径不能为空。"));
        }

        return Task.Run(
            () => CaptureToPng(
                window,
                outputPath,
                cancellationToken),
            cancellationToken);
    }

    private static GameScreenshotCaptureResult CaptureToPng(
        GameWindowInfo window,
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            // 如果发送取消请求，且成功取消会抛出 OperationCanceledException
            cancellationToken.ThrowIfCancellationRequested();

            using Process process =
                Process.GetProcessById(window.ProcessId);

            // 刷新进程 清除缓存 重新获取窗口句柄
            process.Refresh();
            IntPtr windowHandle =
                process.MainWindowHandle;

            if (windowHandle == IntPtr.Zero)
            {
                return Failure(
                    "游戏进程仍在运行，但主窗口不可用。");
            }

            if (!NativeMethods.GetClientRect(
                    windowHandle,
                    out NativeRect clientRectangle))
            {
                return Failure(
                    GetLastWin32ErrorMessage(
                        "读取游戏客户区失败"));
            }

            int width =
                clientRectangle.Right -
                clientRectangle.Left;

            int height =
                clientRectangle.Bottom -
                clientRectangle.Top;

            if (width <= 0 || height <= 0)
            {
                return Failure(
                    "游戏客户区尺寸无效。");
            }

            var clientOrigin = new NativePoint
            {
                X = 0,
                Y = 0
            };
            // 获取游戏窗口左上角坐标相对于整个屏幕的位置
            if (!NativeMethods.ClientToScreen(
                    windowHandle,
                    ref clientOrigin))
            {
                return Failure(
                    GetLastWin32ErrorMessage(
                        "转换客户区坐标失败"));
            }

            string fullOutputPath =
                Path.GetFullPath(outputPath);

            string? directoryPath =
                Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 创建空白位图
            using var bitmap = new Bitmap(
                width,
                height,
                PixelFormat.Format32bppArgb);

            using Graphics graphics =
                Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                clientOrigin.X,
                clientOrigin.Y,
                0,
                0,
                new Size(width, height),
                CopyPixelOperation.SourceCopy);

            cancellationToken.ThrowIfCancellationRequested();

            bitmap.Save(
                fullOutputPath,
                ImageFormat.Png);

            return new GameScreenshotCaptureResult(
                true,
                $"截图已保存：{fullOutputPath}",
                fullOutputPath,
                width,
                height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            return Failure(
                $"游戏进程或截图参数无效：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                $"无法访问游戏进程：{exception.Message}");
        }
        catch (Win32Exception exception)
        {
            return Failure(
                $"Windows 截图失败：{exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(
                $"无法保存截图：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                $"没有权限保存截图：{exception.Message}");
        }
        catch (ExternalException exception)
        {
            return Failure(
                $"PNG 编码失败：{exception.Message}");
        }
    }

    private static string GetLastWin32ErrorMessage(
        string operation)
    {
        int errorCode =
            Marshal.GetLastWin32Error();

        string errorMessage =
            new Win32Exception(errorCode).Message;

        return $"{operation}：{errorMessage}";
    }

    private static GameScreenshotCaptureResult Failure(
        string message)
    {
        return new GameScreenshotCaptureResult(
            false,
            message,
            null,
            0,
            0);
    }
}