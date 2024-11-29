using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking;

public sealed class GameTicker : SharedGameTicker
{
    public override void Initialize()
    {
        base.Initialize();
        _isServer = true;
        PlayerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Timer.Spawn(0, () => PlayerManager.JoinGame(session));
                Log.Info($"{session.Name} was connected");
                break;
            case SessionStatus.InGame:
                AddSession(session);
                break;
        }
    }
}