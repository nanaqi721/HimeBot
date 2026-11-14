using OpenCvSharp;

namespace HimeBot.Vision.ImageProcessing;

public sealed class ImagePreprocessor
{
    public Task<ImageProcessingResult> CreateGrayscaleCopyAsync(
        string sourcePath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Task.FromResult(
                Failure("源图片路径不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Task.FromResult(
                Failure("输出图片路径不能为空。"));
        }

        return Task.Run(
            () => CreateGrayscaleCopy(
                sourcePath,
                outputPath,
                cancellationToken),
            cancellationToken);
    }

    public Task<ImageProcessingResult> CreateBinaryCopyAsync(
        string sourcePath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Task.FromResult(
                Failure("源图片路径不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Task.FromResult(
                Failure("输出图片路径不能为空。"));
        }

        return Task.Run(
            () => CreateBinaryCopy(
                sourcePath,
                outputPath,
                cancellationToken),
            cancellationToken);
    }

    public Task<ImageProcessingResult> CreateRegionCopyAsync(
        string sourcePath,
        string outputPath,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Task.FromResult(
                Failure("源图片路径不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Task.FromResult(
                Failure("输出图片路径不能为空。"));
        }

        if (x < 0 || y < 0)
        {
            return Task.FromResult(
                Failure("裁剪区域的 X 和 Y 不能小于 0。"));
        }

        if (width <= 0 || height <= 0)
        {
            return Task.FromResult(
                Failure("裁剪区域的宽度和高度必须大于 0。"));
        }

        return Task.Run(
            () => CreateRegionCopy(
                sourcePath,
                outputPath,
                x,
                y,
                width,
                height,
                cancellationToken),
            cancellationToken);
    }

    private static ImageProcessingResult CreateGrayscaleCopy(
        string sourcePath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullSourcePath = Path.GetFullPath(sourcePath);
            string fullOutputPath = Path.GetFullPath(outputPath);

            if (!File.Exists(fullSourcePath))
            {
                return Failure($"源图片不存在：{fullSourcePath}");
            }

            if (string.Equals(
                fullSourcePath,
                fullOutputPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "输出路径不能与源图片路径相同。");
            }

            // 首次调用时会加载 OpenCV 的原生运行库。
            string openCvVersion = Cv2.GetVersionString() ?? "Unknown";

            using Mat sourceImage =
                Cv2.ImRead(
                    fullSourcePath,
                    ImreadModes.Color);

            if (sourceImage.Empty())
            {
                return Failure(
                    $"OpenCV 无法读取图片：{fullSourcePath}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var grayscaleImage = new Mat();

            Cv2.CvtColor(
                sourceImage,
                grayscaleImage,
                ColorConversionCodes.BGR2GRAY);

            string? outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool isSaved =
                Cv2.ImWrite(
                    fullOutputPath,
                    grayscaleImage);

            if (!isSaved)
            {
                return Failure(
                    $"OpenCV 无法保存图片：{fullOutputPath}");
            }

            return new ImageProcessingResult(
                true,
                $"灰度图已保存；OpenCV 版本：{openCvVersion}；" +
                $"路径：{fullOutputPath}",
                fullOutputPath,
                grayscaleImage.Width,
                grayscaleImage.Height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenCVException exception)
        {
            return Failure(
                $"OpenCV 图像处理失败：{exception.Message}");
        }
        catch (DllNotFoundException exception)
        {
            return Failure(
                $"OpenCV 原生运行库未找到：{exception.Message}");
        }
        catch (BadImageFormatException exception)
        {
            return Failure(
                $"OpenCV 原生运行库与当前 x64 平台不匹配：" +
                $"{exception.Message}");
        }
        catch (TypeInitializationException exception)
        {
            return Failure(
                $"OpenCV 原生运行库初始化失败：" +
                $"{exception.InnerException?.Message ?? exception.Message}");
        }
        catch (ArgumentException exception)
        {
            return Failure(
                $"图像路径无效：{exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(
                $"读取或保存图片失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                $"没有权限读取或保存图片：{exception.Message}");
        }
    }

    private static ImageProcessingResult CreateBinaryCopy(
        string sourcePath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullSourcePath = Path.GetFullPath(sourcePath);
            string fullOutputPath = Path.GetFullPath(outputPath);

            if (!File.Exists(fullSourcePath))
            {
                return Failure($"源图片不存在：{fullSourcePath}");
            }

            if (string.Equals(
                fullSourcePath,
                fullOutputPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "输出路径不能与源图片路径相同。");
            }

            // 首次调用时会加载 OpenCV 的原生运行库。
            string openCvVersion = Cv2.GetVersionString() ?? "Unknown";

            using Mat grayscaleImage =
                Cv2.ImRead(
                    fullSourcePath,
                    ImreadModes.Grayscale);

            if (grayscaleImage.Empty())
            {
                return Failure(
                    $"OpenCV 无法读取图片：{fullSourcePath}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var binaryImage = new Mat();

            double threshold =
                Cv2.Threshold(
                    grayscaleImage,
                    binaryImage,
                    0,
                    255,
                    ThresholdTypes.Binary |
                    ThresholdTypes.Otsu);

            string? outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool isSaved =
                Cv2.ImWrite(
                    fullOutputPath,
                    binaryImage);

            if (!isSaved)
            {
                return Failure(
                    $"OpenCV 无法保存二值图：{fullOutputPath}");
            }

            return new ImageProcessingResult(
                true,
                $"二值图已保存；Otsu 阈值：{threshold:0.##}；" +
                $"OpenCV 版本：{openCvVersion}；" +
                $"路径：{fullOutputPath}",
                fullOutputPath,
                binaryImage.Width,
                binaryImage.Height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenCVException exception)
        {
            return Failure(
                $"OpenCV 图像处理失败：{exception.Message}");
        }
        catch (DllNotFoundException exception)
        {
            return Failure(
                $"OpenCV 原生运行库未找到：{exception.Message}");
        }
        catch (BadImageFormatException exception)
        {
            return Failure(
                $"OpenCV 原生运行库与当前 x64 平台不匹配：" +
                $"{exception.Message}");
        }
        catch (TypeInitializationException exception)
        {
            return Failure(
                $"OpenCV 原生运行库初始化失败：" +
                $"{exception.InnerException?.Message ?? exception.Message}");
        }
        catch (ArgumentException exception)
        {
            return Failure(
                $"图像路径无效：{exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(
                $"读取或保存图片失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                $"没有权限读取或保存图片：{exception.Message}");
        }
    }

    private static ImageProcessingResult CreateRegionCopy(
        string sourcePath,
        string outputPath,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullSourcePath = Path.GetFullPath(sourcePath);
            string fullOutputPath = Path.GetFullPath(outputPath);

            if (!File.Exists(fullSourcePath))
            {
                return Failure(
                    $"源图片不存在：{fullSourcePath}");
            }

            if (string.Equals(
                fullSourcePath,
                fullOutputPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    "输出路径不能与源图片路径相同。");
            }

            string openCvVersion =
                Cv2.GetVersionString() ?? "Unknown";

            using Mat sourceImage =
                Cv2.ImRead(
                    fullSourcePath,
                    ImreadModes.Color);

            if (sourceImage.Empty())
            {
                return Failure(
                    $"OpenCV 无法读取图片：{fullSourcePath}");
            }

            if (x >= sourceImage.Width ||
                y >= sourceImage.Height ||
                width > sourceImage.Width - x ||
                height > sourceImage.Height - y)
            {
                return Failure(
                    $"裁剪区域超出图片范围。" +
                    $"图片尺寸：{sourceImage.Width} × {sourceImage.Height}；" +
                    $"裁剪区域：X={x}，Y={y}，" +
                    $"Width={width}，Height={height}。");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var region =
                new Rect(
                    x,
                    y,
                    width,
                    height);

            using Mat regionImage =
                new Mat(
                    sourceImage,
                    region);

            string? outputDirectory =
                Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool isSaved =
                Cv2.ImWrite(
                    fullOutputPath,
                    regionImage);

            if (!isSaved)
            {
                return Failure(
                    $"OpenCV 无法保存裁剪图片：{fullOutputPath}");
            }

            return new ImageProcessingResult(
                true,
                $"OCR 区域已保存；" +
                $"区域：X={x}，Y={y}，" +
                $"Width={width}，Height={height}；" +
                $"OpenCV 版本：{openCvVersion}；" +
                $"路径：{fullOutputPath}",
                fullOutputPath,
                regionImage.Width,
                regionImage.Height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenCVException exception)
        {
            return Failure(
                $"OpenCV 图像处理失败：{exception.Message}");
        }
        catch (DllNotFoundException exception)
        {
            return Failure(
                $"OpenCV 原生运行库未找到：{exception.Message}");
        }
        catch (BadImageFormatException exception)
        {
            return Failure(
                $"OpenCV 原生运行库与当前 x64 平台不匹配：" +
                $"{exception.Message}");
        }
        catch (TypeInitializationException exception)
        {
            return Failure(
                $"OpenCV 原生运行库初始化失败：" +
                $"{exception.InnerException?.Message ?? exception.Message}");
        }
        catch (ArgumentException exception)
        {
            return Failure(
                $"图片路径或裁剪区域无效：{exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(
                $"读取或保存图片失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                $"没有权限读取或保存图片：{exception.Message}");
        }
    }

    private static ImageProcessingResult Failure(
        string message)
    {
        return new ImageProcessingResult(
            false,
            message,
            null,
            0,
            0);
    }
}
