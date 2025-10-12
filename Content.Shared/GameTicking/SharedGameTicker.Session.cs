using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public partial class SharedGameTicker
{
    public void AddSession(ICommonSession session)
    {
        if (IsServer != NetManager.IsServer) 
            throw new Exception(); //Client tries to add a session for server?

        var euid = Spawn("EntMale", new EntityCoordinates(GridUid, Vector2.Zero));
        var b = LightSystem.EnsureLight( euid);
        LightSystem.SetColor(euid, Color.White, b);
        LightSystem.SetRadius(euid, 48f, b);
        LightSystem.SetEnergy(euid, 0.5f, b);
        
        PlayerManager.SetAttachedEntity(session, euid);
        ContentStateManager.SetState(session,"Content.Client.Game.GameState");
    }
}