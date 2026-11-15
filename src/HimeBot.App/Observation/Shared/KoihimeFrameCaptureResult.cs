namespace HimeBot.App.Observation.Shared;

/// <summary>
/// 一次恋姬大战客户区截图及标准尺寸校验的结果。
/// </summary>
/// <param name="IsSuccess">
/// 截图是否成功，并且尺寸是否符合当前识别规则。
/// </param>
/// <param name="Message">截图结果或失败原因。</param>
/// <param name="ScreenshotPath">
/// 已生成的截图路径；尺寸不符合要求时仍保留该路径。
/// </param>
public sealed record KoihimeFrameCaptureResult(
    bool IsSuccess,
    string Message,
    string? ScreenshotPath);
