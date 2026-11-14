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
