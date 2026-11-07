using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimeBot.App.FilePicking;

/// <summary>
/// 游戏选择器
/// </summary>
public interface IGameExecutablePicker
{
    string? PickExecutable();
}