using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public partial class SharedGameTicker
{
    public void AddSession(ICommonSession session)
    {
        Logger.Debug(_isServer + " " + _netManager.IsServer);
        if (_isServer != _netManager.IsServer) 
            throw new Exception(); //Client tries to add a session for server?
        
        ContentStateManager.SetState(session,"Content.Client.Game.GameState");
    }
}