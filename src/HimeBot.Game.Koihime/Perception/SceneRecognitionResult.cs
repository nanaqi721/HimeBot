using HimeBot.Game.Koihime.Domain;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 游戏场景识别结果及其原始证据。
/// </summary>
/// <param name="Scene">识别得到的游戏场景</param>
/// <param name="Message">判断说明</param>
/// <param name="Score">首页模板匹配分数</param>
/// <param name="MatchX">模板最佳匹配位置的 X 坐标</param>
/// <param name="MatchY">模板最佳匹配位置的 Y 坐标</param>
public sealed record SceneRecognitionResult(
    GameScene Scene,
    string Message,
    double Score,
    int MatchX,
    int MatchY);