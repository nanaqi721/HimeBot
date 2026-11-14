namespace HimeBot.Vision.Ocr;

/// <summary>
/// 单次 OCR 识别结果。
/// </summary>
/// <param name="IsSuccess">OCR 调用是否成功</param>
/// <param name="Message">结果说明</param>
/// <param name="RawText">OCR 返回的原始文字</param>
/// <param name="MeanConfidence">平均置信度</param>
public sealed record OcrRecognitionResult(
    bool IsSuccess,
    string Message,
    string RawText,
    float MeanConfidence);