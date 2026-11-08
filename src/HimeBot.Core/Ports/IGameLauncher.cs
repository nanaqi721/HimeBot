
using HimeBot.Core.Launching;

namespace HimeBot.Core.Ports;

public interface IGameLauncher
{
    GameLaunchResult Launch(string launchTargetPath);
}



