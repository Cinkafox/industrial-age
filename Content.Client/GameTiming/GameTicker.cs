using Content.Shared.GameTicking;

namespace Content.Client.GameTiming;

public class GameTicker : SharedGameTicker
{
    public void StartSinglePlayer()
    {
        InitializeGame();
        AddSession(PlayerManager.LocalSession!);
    }
}