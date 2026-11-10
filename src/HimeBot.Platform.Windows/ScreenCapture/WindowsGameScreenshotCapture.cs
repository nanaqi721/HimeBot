using HimeBot.Core.Windowing;
using HimeBot.Platform.Windows.Native;
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

            // 通过窗口句柄创建 WGC 对象
            GraphicsCaptureItem captureItem =
                GraphicsCaptureItemFactory.CreateForWindow(
                    windowHandle);

            // 获取窗口大小
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

            // 创建并使用D3D11Device
            // D3D11Device给Vortice/Direct3D底层代码使用
            // 再包装成Direct3DDevice给WGC使用
            using (Direct3D11CaptureDevice captureDevice =
                   Direct3D11CaptureDevice.Create())
            using (ManualResetEventSlim firstFrameReady =
                   new(false))
            // 创建一个 WGC 截图帧池（frame pool），WGC 用来存放刚捕获到的 GPU 图像帧的一组缓冲区。

            // captureSize：指定帧池中纹理的初始宽度和高度。
            // CreateFreeThreaded 表示帧池自己使用内部工作线程接收截图帧，不依赖 WPF UI 线程的消息循环
            using (Direct3D11CaptureFramePool framePool =
                   Direct3D11CaptureFramePool.CreateFreeThreaded(
                       captureDevice.WinRtDevice,
                       DirectXPixelFormat.B8G8R8A8UIntNormalized,
                       1,
                       captureSize))
            // 请创建一次捕获任务，从 captureItem 指定的游戏窗口获取画面，
            // 并把捕获到的帧放进 framePool。
            using (GraphicsCaptureSession captureSession =
                   framePool.CreateCaptureSession(
                       captureItem))
            {
                // 订阅事件，新帧进入framePool，FrameArrived被触发，事件执行
                framePool.FrameArrived += (_, _) =>
                    firstFrameReady.Set();
                // 开始捕获
                captureSession.StartCapture();

                // 等待通知到达
                bool frameArrived =
                    firstFrameReady.Wait(
                        TimeSpan.FromSeconds(5),
                        cancellationToken);

                if (!frameArrived)
                {
                    return Failure(
                        "等待 Windows Graphics Capture 第一帧超时。");
                }
                // WGC 捕获到的窗口画面所在的 GPU 图像资源
                // 它主要包含：
                //            ContentSize：这一帧的有效画面尺寸；
                //            Surface：保存画面像素的 Direct3D GPU 表面；
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

                // 取得 GPU 表面
                using ID3D11Texture2D frameTexture =
                    Direct3D11SurfaceInterop.GetTexture(
                        frame.Surface);

                // 获取 Direct3D 纹理的描述信息
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

                // 用于创建 GPU 到 CPU 中转用的暂存纹理
                Texture2DDescription stagingDescription =
                    textureDescription;

                // 这是一项用于 CPU/GPU 数据传输的暂存资源
                stagingDescription.Usage =
                    ResourceUsage.Staging;

                // 不会把它当成渲染目标或着色器纹理使用
                stagingDescription.BindFlags =
                    BindFlags.None;

                // 允许 CPU 读取
                stagingDescription.CPUAccessFlags =
                    CpuAccessFlags.Read;

                // 不需要原纹理的其他特殊能力
                stagingDescription.MiscFlags =
                    ResourceOptionFlags.None;

                // 让当前 Direct3D 设备按照描述信息创建一张二维纹理，此时里面的内容还是空的
                using ID3D11Texture2D stagingTexture =
                    captureDevice.NativeDevice.CreateTexture2D(
                        stagingDescription);

                // 取得 Direct3D 设备的 即时上下文，向 Direct3D 发出操作纹理的命令。
                using ID3D11DeviceContext deviceContext =
                    captureDevice.NativeDevice.ImmediateContext;

                // 把原始纹理转变为cpu可读的暂存纹理
                deviceContext.CopyResource(
                    stagingTexture,
                    frameTexture);

                // 取得内存地址和Rowpitch
                // DataPointer：指向纹理第一个字节的原生内存地址
                // Rowpitch：表示相邻两行起始位置之间的字节距离，其中可能包含 GPU 添加的对齐填充。
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
                        // IntPtr 表示原生内存地址。
                        IntPtr sourceRow = IntPtr.Add(
                            mappedSubresource.DataPointer,
                            checked(row * mappedRowPitch));

                        // 计算目标数组的写入位置
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

            if (!TryGetClientCropRectangle(
                    windowHandle,
                    textureWidth,
                    textureHeight,
                    out Rectangle clientCropRectangle,
                    out string cropErrorMessage))
            {
                return Failure(cropErrorMessage);
            }

            int clientRowBytes =
                checked(clientCropRectangle.Width * 4);

            string fullOutputPath =
                Path.GetFullPath(outputPath);

            string? directoryPath =
                Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using var bitmap = new Bitmap(
                clientCropRectangle.Width,
                clientCropRectangle.Height,
                PixelFormat.Format32bppArgb);

            var bitmapRectangle = new Rectangle(
                0,
                0,
                clientCropRectangle.Width,
                clientCropRectangle.Height);

            // bitmapData.Scan0：第一行像素的内存地址
            // bitmapData.Stride：相邻两行起点之间的字节距离
            BitmapData bitmapData = bitmap.LockBits(
                bitmapRectangle,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int bitmapRowPitch =
                    Math.Abs(bitmapData.Stride);

                if (bitmapRowPitch < clientRowBytes)
                {
                    return Failure(
                        $"Bitmap 行跨度无效：" +
                        $"{bitmapRowPitch}，" +
                        $"至少需要 {clientRowBytes} 字节。");
                }

                for (int row = 0;
                     row < clientCropRectangle.Height;
                     row++)
                {
                    int sourceOffset =
                        checked(
                            ((clientCropRectangle.Y + row) *
                             packedRowBytes) +
                            (clientCropRectangle.X * 4));

                    IntPtr destinationRow = IntPtr.Add(
                        bitmapData.Scan0,
                        checked(row * bitmapData.Stride));

                    Marshal.Copy(
                        framePixels,
                        sourceOffset,
                        destinationRow,
                        clientRowBytes);
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
                $"WGC 客户区 PNG 已保存：" +
                $"{clientCropRectangle.Width} × " +
                $"{clientCropRectangle.Height}；" +
                $"完整帧：{textureWidth} × {textureHeight}，" +
                $"{textureFormat}；" +
                $"裁剪起点：({clientCropRectangle.X}, " +
                $"{clientCropRectangle.Y})；" +
                $"GPU 行跨度：{mappedRowPitch} 字节；" +
                $"路径：{fullOutputPath}",
                fullOutputPath,
                clientCropRectangle.Width,
                clientCropRectangle.Height);
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

    private static bool TryGetClientCropRectangle(
        IntPtr windowHandle,
        int frameWidth,
        int frameHeight,
        out Rectangle cropRectangle,
        out string errorMessage)
    {

        /**
         * 先设置默认值可以保证：
         * 失败时不会返回旧的裁剪矩形；
         * 成功时错误消息为空；
         * 所有提前 return false 的路径都有有效输出。
        **/
        cropRectangle = Rectangle.Empty;
        errorMessage = string.Empty;

        if (!NativeMethods.GetClientRect(
                windowHandle,
                out NativeRect clientRectangle))
        {
            errorMessage =
                GetLastWin32ErrorMessage(
                    "读取游戏客户区失败");

            return false;
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
            errorMessage =
                "游戏客户区尺寸无效。";

            return false;
        }

        var clientOrigin = new NativePoint
        {
            X = 0,
            Y = 0
        };

        // 转换为屏幕坐标
        if (!NativeMethods.ClientToScreen(
                windowHandle,
                ref clientOrigin))
        {
            errorMessage =
                GetLastWin32ErrorMessage(
                    "转换客户区坐标失败");

            return false;
        }

        int hresult =
            NativeMethods.DwmGetWindowAttribute(
                windowHandle,
                NativeMethods.DwmwaExtendedFrameBounds,
                out NativeRect windowBounds,
                Marshal.SizeOf<NativeRect>());

        if (hresult < 0)
        {
            string hresultMessage =
                Marshal.GetExceptionForHR(hresult)?.Message ??
                "未知错误";

            errorMessage =
                $"读取窗口可见边界失败：" +
                $"{hresultMessage} " +
                $"(HRESULT: 0x{hresult:X8})";

            return false;
        }

        // 计算客户区相对于窗口区坐标

        // 客户区坐标 - 窗口区坐标
        int cropX =
            clientOrigin.X - windowBounds.Left;

        // 窗口区坐标 - 客户区坐标
        int cropY =
            clientOrigin.Y - windowBounds.Top;

        if (cropX < 0 ||
            cropY < 0 ||
            cropX + clientWidth > frameWidth ||
            cropY + clientHeight > frameHeight)
        {
            errorMessage =
                $"客户区裁剪范围超出 WGC 帧：" +
                $"帧为 {frameWidth} × {frameHeight}，" +
                $"裁剪区域为 ({cropX}, {cropY}, " +
                $"{clientWidth}, {clientHeight})。";

            return false;
        }

        cropRectangle = new Rectangle(
            cropX,
            cropY,
            clientWidth,
            clientHeight);

        return true;
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
