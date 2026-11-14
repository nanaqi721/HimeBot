namespace HimeBot.Game.Koihime.Domain;

/// <summary>
/// 游戏中的粮食库存，数值单位固定为“万”。
/// </summary>
/// <param name="CurrentAmount">当前粮食数量，单位为万</param>
/// <param name="Capacity">粮食库存上限，单位为万</param>
public sealed record GrainStock(
    int CurrentAmount,
    int Capacity);