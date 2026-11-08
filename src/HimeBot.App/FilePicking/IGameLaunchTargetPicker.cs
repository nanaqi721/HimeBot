namespace HimeBot.App.FilePicking;

/// <summary>
/// 选择游戏启动目标。
/// </summary>
public interface IGameLaunchTargetPicker
{
    string? PickLaunchTarget();
}
