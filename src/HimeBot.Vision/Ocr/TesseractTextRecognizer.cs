using TesseractOCR;
using TesseractOCR.Enums;
using PixImage = TesseractOCR.Pix.Image;

namespace HimeBot.Vision.Ocr;

/// <summary>
/// 使用 Tesseract 识别日文单行文字。
/// </summary>
public sealed class TesseractTextRecognizer
{
    private const string JapaneseModelFileName = "jpn.traineddata";

    public Task<OcrRecognitionResult> RecognizeSingleLineAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => RecognizeSingleLine(imagePath, cancellationToken),
            cancellationToken);
    }

    private static OcrRecognitionResult RecognizeSingleLine(
        string imagePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return CreateFailure("图片路径不能为空。");
        }

        if (!File.Exists(imagePath))
        {
            return CreateFailure($"找不到待识别图片：{imagePath}");
        }

        string tessdataPath = Path.Combine(
            AppContext.BaseDirectory,
            "tessdata");

        string modelPath = Path.Combine(
            tessdataPath,
            JapaneseModelFileName);

        if (!File.Exists(modelPath))
        {
            return CreateFailure($"找不到日文 OCR 模型：{modelPath}");
        }

        try
        {
            using var engine = new Engine(
                tessdataPath,
                Language.Japanese,
                EngineMode.LstmOnly);

            using PixImage image = PixImage.LoadFromFile(imagePath);

            cancellationToken.ThrowIfCancellationRequested();

            using Page page = engine.Process(
                image,
                PageSegMode.SingleLine);

            cancellationToken.ThrowIfCancellationRequested();

            string rawText = page.Text.Trim();

            return new OcrRecognitionResult(
                true,
                "OCR 识别完成。",
                rawText,
                page.MeanConfidence);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return CreateFailure($"OCR 识别失败：{exception.Message}");
        }
    }

    private static OcrRecognitionResult CreateFailure(string message)
    {
        return new OcrRecognitionResult(
            false,
            message,
            string.Empty,
            0);
    }
}