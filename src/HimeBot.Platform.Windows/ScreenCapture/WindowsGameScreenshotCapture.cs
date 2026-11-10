using HimeBot.Core.Windowing;
using SharpGen.Runtime;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

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

        if (!GraphicsCaptureSession.IsSupported())
        {
            return Task.FromResult(
                Failure(
                    "当前系统或图形设备不支持 Windows Graphics Capture。"));
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
            cancellationToken.ThrowIfCancellationRequested();

            using Process process =
                Process.GetProcessById(window.ProcessId);

            process.Refresh();
            IntPtr windowHandle =
                process.MainWindowHandle;

            if (windowHandle == IntPtr.Zero)
            {
                return Failure(
                    "游戏进程仍在运行，但主窗口不可用。");
            }

            GraphicsCaptureItem captureItem =
                GraphicsCaptureItemFactory.CreateForWindow(
                    windowHandle);

            var captureSize = captureItem.Size;

            if (captureSize.Width <= 0 ||
                captureSize.Height <= 0)
            {
                return Failure(
                    "Windows Graphics Capture 返回的窗口尺寸无效。");
            }

            int firstFrameWidth;
            int firstFrameHeight;
            int textureWidth;
            int textureHeight;
            string textureFormat;
            int mappedRowPitch;
            int packedRowBytes;
            byte[] framePixels;

            using (Direct3D11CaptureDevice captureDevice =
                   Direct3D11CaptureDevice.Create())
            using (ManualResetEventSlim firstFrameReady =
                   new(false))
            using (Direct3D11CaptureFramePool framePool =
                   Direct3D11CaptureFramePool.CreateFreeThreaded(
                       captureDevice.WinRtDevice,
                       DirectXPixelFormat.B8G8R8A8UIntNormalized,
                       1,
                       captureSize))
            using (GraphicsCaptureSession captureSession =
                   framePool.CreateCaptureSession(
                       captureItem))
            {
                framePool.FrameArrived += (_, _) =>
                    firstFrameReady.Set();

                captureSession.StartCapture();

                bool frameArrived =
                    firstFrameReady.Wait(
                        TimeSpan.FromSeconds(5),
                        cancellationToken);

                if (!frameArrived)
                {
                    return Failure(
                        "等待 Windows Graphics Capture 第一帧超时。");
                }

                using Direct3D11CaptureFrame frame =
                    framePool.TryGetNextFrame();

                firstFrameWidth =
                    frame.ContentSize.Width;

                firstFrameHeight =
                    frame.ContentSize.Height;

                if (firstFrameWidth <= 0 ||
                    firstFrameHeight <= 0)
                {
                    return Failure(
                        "Windows Graphics Capture 返回的第一帧尺寸无效。");
                }

                using ID3D11Texture2D frameTexture =
                    Direct3D11SurfaceInterop.GetTexture(
                        frame.Surface);

                Texture2DDescription textureDescription =
                    frameTexture.Description;

                textureWidth =
                    checked((int)textureDescription.Width);

                textureHeight =
                    checked((int)textureDescription.Height);

                textureFormat =
                    textureDescription.Format.ToString();

                if (textureWidth != firstFrameWidth ||
                    textureHeight != firstFrameHeight)
                {
                    return Failure(
                        $"WGC 帧尺寸与原生纹理尺寸不一致：" +
                        $"帧为 {firstFrameWidth} × {firstFrameHeight}，" +
                        $"纹理为 {textureWidth} × {textureHeight}。");
                }

                if (textureDescription.Format !=
                    Vortice.DXGI.Format.B8G8R8A8_UNorm)
                {
                    return Failure(
                        $"WGC 返回了尚未支持的纹理格式：" +
                        $"{textureDescription.Format}。");
                }

                Texture2DDescription stagingDescription =
                    textureDescription;

                stagingDescription.Usage =
                    ResourceUsage.Staging;

                stagingDescription.BindFlags =
                    BindFlags.None;

                stagingDescription.CPUAccessFlags =
                    CpuAccessFlags.Read;

                stagingDescription.MiscFlags =
                    ResourceOptionFlags.None;

                using ID3D11Texture2D stagingTexture =
                    captureDevice.NativeDevice.CreateTexture2D(
                        stagingDescription);

                using ID3D11DeviceContext deviceContext =
                    captureDevice.NativeDevice.ImmediateContext;

                deviceContext.CopyResource(
                    stagingTexture,
                    frameTexture);

                deviceContext.Map(
                    stagingTexture,
                    0,
                    MapMode.Read,
                    MapFlags.None,
                    out MappedSubresource mappedSubresource)
                    .CheckError();

                try
                {
                    if (mappedSubresource.DataPointer ==
                        IntPtr.Zero)
                    {
                        return Failure(
                            "映射 staging texture 后返回了空数据指针。");
                    }

                    mappedRowPitch =
                        checked((int)mappedSubresource.RowPitch);

                    packedRowBytes =
                        checked(textureWidth * 4);

                    if (mappedRowPitch < packedRowBytes)
                    {
                        return Failure(
                            $"映射后的行跨度无效：" +
                            $"{mappedRowPitch}，" +
                            $"至少需要 {packedRowBytes} 字节。");
                    }

                    framePixels = new byte[
                        checked(packedRowBytes * textureHeight)];

                    cancellationToken.ThrowIfCancellationRequested();

                    for (int row = 0;
                         row < textureHeight;
                         row++)
                    {
                        IntPtr sourceRow = IntPtr.Add(
                            mappedSubresource.DataPointer,
                            checked(row * mappedRowPitch));

                        int destinationOffset =
                            checked(row * packedRowBytes);

                        Marshal.Copy(
                            sourceRow,
                            framePixels,
                            destinationOffset,
                            packedRowBytes);
                    }
                }
                finally
                {
                    deviceContext.Unmap(
                        stagingTexture,
                        0);
                }
            }

            string fullOutputPath =
                Path.GetFullPath(outputPath);

            string? directoryPath =
                Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using var bitmap = new Bitmap(
                textureWidth,
                textureHeight,
                PixelFormat.Format32bppArgb);

            var bitmapRectangle = new Rectangle(
                0,
                0,
                textureWidth,
                textureHeight);

            BitmapData bitmapData = bitmap.LockBits(
                bitmapRectangle,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int bitmapRowPitch =
                    Math.Abs(bitmapData.Stride);

                if (bitmapRowPitch < packedRowBytes)
                {
                    return Failure(
                        $"Bitmap 行跨度无效：" +
                        $"{bitmapRowPitch}，" +
                        $"至少需要 {packedRowBytes} 字节。");
                }

                for (int row = 0;
                     row < textureHeight;
                     row++)
                {
                    int sourceOffset =
                        checked(row * packedRowBytes);

                    IntPtr destinationRow = IntPtr.Add(
                        bitmapData.Scan0,
                        checked(row * bitmapData.Stride));

                    Marshal.Copy(
                        framePixels,
                        sourceOffset,
                        destinationRow,
                        packedRowBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            cancellationToken.ThrowIfCancellationRequested();

            bitmap.Save(
                fullOutputPath,
                ImageFormat.Png);

            return new GameScreenshotCaptureResult(
                true,
                $"WGC PNG 已保存：" +
                $"{textureWidth} × {textureHeight}，{textureFormat}；" +
                $"GPU 行跨度：{mappedRowPitch} 字节；" +
                $"路径：{fullOutputPath}",
                fullOutputPath,
                textureWidth,
                textureHeight);
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
        catch (SharpGenException exception)
        {
            return Failure(
                $"Direct3D 11 设备创建失败：" +
                $"{exception.Message} " +
                $"(HRESULT: 0x{exception.HResult:X8})");
        }
        catch (COMException exception)
        {
            return Failure(
                $"Windows Graphics Capture 初始化失败：" +
                $"{exception.Message} " +
                $"(HRESULT: 0x{exception.HResult:X8})");
        }
        catch (ExternalException exception)
        {
            return Failure(
                $"PNG 编码失败：{exception.Message}");
        }
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
