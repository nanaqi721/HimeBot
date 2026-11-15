using OpenCvSharp;

namespace HimeBot.Vision.Matching;

public sealed class TemplateMatcher
{
    public Task<TemplateMatchResult> FindBestMatchAsync(
        string sourcePath,
        string templatePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Task.FromResult(
                Failure("待搜索图片路径不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(templatePath))
        {
            return Task.FromResult(
                Failure("模板图片路径不能为空。"));
        }

        return Task.Run(
            () => FindBestMatch(
                sourcePath,
                templatePath,
                cancellationToken),
            cancellationToken);
    }

    private static TemplateMatchResult FindBestMatch(
        string sourcePath,
        string templatePath,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullSourcePath =
                Path.GetFullPath(sourcePath);

            string fullTemplatePath =
                Path.GetFullPath(templatePath);

            if (!File.Exists(fullSourcePath))
            {
                return Failure(
                    $"待搜索图片不存在：{fullSourcePath}");
            }

            if (!File.Exists(fullTemplatePath))
            {
                return Failure(
                    $"模板图片不存在：{fullTemplatePath}");
            }

            using Mat sourceImage =
                Cv2.ImRead(
                    fullSourcePath,
                    ImreadModes.Color);

            using Mat templateImage =
                Cv2.ImRead(
                    fullTemplatePath,
                    ImreadModes.Color);

            if (sourceImage.Empty())
            {
                return Failure(
                    $"OpenCV 无法读取待搜索图片：{fullSourcePath}");
            }

            if (templateImage.Empty())
            {
                return Failure(
                    $"OpenCV 无法读取模板图片：{fullTemplatePath}");
            }

            if (templateImage.Width > sourceImage.Width ||
                templateImage.Height > sourceImage.Height)
            {
                return Failure(
                    "模板图片不能大于待搜索图片。" +
                    $"待搜索图片：{sourceImage.Width} × " +
                    $"{sourceImage.Height}；" +
                    $"模板图片：{templateImage.Width} × " +
                    $"{templateImage.Height}。");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using var scoreMap = new Mat();

            Cv2.MatchTemplate(
                sourceImage,
                templateImage,
                scoreMap,
                TemplateMatchModes.CCoeffNormed);

            cancellationToken.ThrowIfCancellationRequested();

            Cv2.MinMaxLoc(
                scoreMap,
                out _,
                out double bestScore,
                out _,
                out Point bestLocation);

            return new TemplateMatchResult(
                true,
                $"模板匹配完成；最佳分数：{bestScore:0.0000}；" +
                $"位置：X={bestLocation.X}，Y={bestLocation.Y}。",
                bestScore,
                bestLocation.X,
                bestLocation.Y,
                templateImage.Width,
                templateImage.Height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenCVException exception)
        {
            return Failure(
                $"OpenCV 模板匹配失败：{exception.Message}");
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
        catch (ArgumentException exception)
        {
            return Failure(
                $"图片路径无效：{exception.Message}");
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

    private static TemplateMatchResult Failure(
        string message)
    {
        return new TemplateMatchResult(
            false,
            message,
            0,
            0,
            0,
            0,
            0);
    }
}