using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public partial class SharedGameTicker
{
    public void AddSession(ICommonSession session)
    {
        if (_isServer != _netManager.IsServer) 
            throw new Exception(); //Client tries to add a session for server?

        var euid = Spawn("EntFemale", new EntityCoordinates(MapUid, Vector2.Zero));
        PlayerManager.SetAttachedEntity(session, euid);
        ContentStateManager.SetState(session,"Content.Client.Game.GameState");
    }
}