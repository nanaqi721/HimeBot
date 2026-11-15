using HimeBot.Game.Koihime.Domain;

namespace HimeBot.App.Observation;

/// <summary>
/// 一次完整场景观察的结果。
/// </summary>
/// <param name="IsSuccess">
/// 截图、模板匹配和场景解释是否都成功执行。
/// </param>
/// <param name="Scene">
/// 当前识别到的场景；技术失败时固定为 Unknown。
/// </param>
/// <param name="Message">
/// 观察结果说明或失败原因。
/// </param>
/// <param name="ScreenshotPath">
/// 成功保存截图后对应的文件路径。
/// </param>
public sealed record KoihimeObservationResult(
    bool IsSuccess,
    GameScene Scene,
    string Message,
    string? ScreenshotPath);
