using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 粮食库存文字的规范化与解析结果。
/// </summary>
/// <param name="IsSuccess">是否成功解析</param>
/// <param name="Message">解析结果说明</param>
/// <param name="NormalizedText">移除空白后的规范化文字</param>
/// <param name="Stock">成功时得到的粮食库存</param>
public sealed record GrainStockParseResult(
    bool IsSuccess,
    string Message,
    string NormalizedText,
    GrainStock? Stock);