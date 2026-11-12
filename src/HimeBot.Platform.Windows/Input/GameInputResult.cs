
namespace HimeBot.Platform.Windows.Input;

/// <summary>
/// 游戏输入操作执行结果
/// </summary>
/// <param name="IsSuccess">输入事件是否成功提交给Windows</param>
/// <param name="Message">输入结果说明</param>
public sealed record GameInputResult(
    bool IsSuccess,
    string Message);



