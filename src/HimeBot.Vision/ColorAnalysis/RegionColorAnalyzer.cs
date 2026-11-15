using OpenCvSharp;

namespace HimeBot.Vision.ColorAnalysis;

/// <summary>
/// 使用 OpenCV 计算图片固定矩形区域的平均 RGB 颜色。
/// </summary>
public sealed class RegionColorAnalyzer
{
    public Task<RegionColorAnalysisResult> AnalyzeAsync(
        string sourcePath,
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

        if (x < 0 || y < 0)
        {
            return Task.FromResult(
                Failure("分析区域的 X 和 Y 不能小于 0。"));
        }

        if (width <= 0 || height <= 0)
        {
            return Task.FromResult(
                Failure("分析区域的宽度和高度必须大于 0。"));
        }

        return Task.Run(
            () => Analyze(
                sourcePath,
                x,
                y,
                width,
                height,
                cancellationToken),
            cancellationToken);
    }

    private static RegionColorAnalysisResult Analyze(
        string sourcePath,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullSourcePath =
                Path.GetFullPath(sourcePath);

            if (!File.Exists(fullSourcePath))
            {
                return Failure(
                    $"源图片不存在：{fullSourcePath}");
            }

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
                    $"分析区域超出图片范围。" +
                    $"图片尺寸：{sourceImage.Width} × " +
                    $"{sourceImage.Height}；" +
                    $"分析区域：X={x}，Y={y}，" +
                    $"Width={width}，Height={height}。");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var regionRectangle =
                new Rect(
                    x,
                    y,
                    width,
                    height);

            using Mat regionImage =
                new Mat(
                    sourceImage,
                    regionRectangle);

            Scalar meanColor =
                Cv2.Mean(regionImage);

            cancellationToken.ThrowIfCancellationRequested();

            // OpenCV 彩色图片的通道顺序是 BGR，
            // 因此 Val0 是蓝色，Val1 是绿色，Val2 是红色。
            double meanBlue = meanColor.Val0;
            double meanGreen = meanColor.Val1;
            double meanRed = meanColor.Val2;

            return new RegionColorAnalysisResult(
                true,
                $"区域颜色分析完成；" +
                $"区域：X={x}，Y={y}，" +
                $"Width={width}，Height={height}；" +
                $"平均 RGB=({meanRed:0.0}, " +
                $"{meanGreen:0.0}, " +
                $"{meanBlue:0.0})。",
                meanRed,
                meanGreen,
                meanBlue);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenCVException exception)
        {
            return Failure(
                $"OpenCV 颜色分析失败：{exception.Message}");
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
                $"图片路径或分析区域无效：{exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(
                $"读取图片失败：{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                $"没有权限读取图片：{exception.Message}");
        }
    }

    private static RegionColorAnalysisResult Failure(
        string message)
    {
        return new RegionColorAnalysisResult(
            false,
            message,
            0,
            0,
            0);
    }
}