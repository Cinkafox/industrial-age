using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Shared.Timing;

namespace Content.Client.GameTicking;

public sealed class GameTicker : SharedGameTicker
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    public override void Initialize()
    {
        base.Initialize();
        if (_baseClient.RunLevel == ClientRunLevel.SinglePlayerGame)
        {
            Timer.Spawn(0,StartSinglePlayer);
        }
    }

    private void StartSinglePlayer()
    {
        InitializeGame();
        AddSession(PlayerManager.LocalSession!);
    }
}