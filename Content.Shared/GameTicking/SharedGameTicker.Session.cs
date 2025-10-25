using System.Numerics;
using Content.Shared.World;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameTicking;

public partial class SharedGameTicker
{
    public void AddSession(ICommonSession session)
    {
        if (IsServer != NetManager.IsServer) 
            throw new Exception(); //Client tries to add a session for server?
        
        SetCar(session, "EntCarGoal");
    }

    public void SetCar(ICommonSession session, EntProtoId carId)
    {
        if (IsServer != NetManager.IsServer) 
            throw new Exception(); //Client tries to add a session for server?

        var spawnPos = Vector2i.Zero;
        var worldGenData = Comp<WorldGenComponent>(GridUid).WorldGenData;

        //TODO: Player spawn condition prototype
        for (var x = 0; x < 200; x++)
        {
            spawnPos = SpiralScanner.GetPosition(x);
            
            if(worldGenData.GetTile(spawnPos) != "sand" ||
               worldGenData.GetTile(spawnPos) + Vector2i.Up != "sand" ||
               worldGenData.GetTile(spawnPos) + Vector2i.Right != "sand" ||
               worldGenData.GetTile(spawnPos) + Vector2i.Down != "sand" ||
               worldGenData.GetTile(spawnPos) + Vector2i.Left != "sand") 
                continue;
            
            break;
        }

  
        var euid = Spawn(carId, new EntityCoordinates(GridUid, spawnPos));
        var b = LightSystem.EnsureLight(euid);
        LightSystem.SetColor(euid, Color.White, b);
        LightSystem.SetRadius(euid, 48f, b);
        LightSystem.SetEnergy(euid, 0.5f, b);
        
        PlayerManager.SetAttachedEntity(session, euid);
        ContentStateManager.SetState(session,"Content.Client.Game.GameState");
    }
}

public static class SpiralScanner
{
    public static (int x, int y) GetPosition(int n)
    {
        if (n == 0) return (0, 0);

        var layer = (int)Math.Ceiling((Math.Sqrt(n + 1) - 1) / 2);
        var legLen = layer * 2;
        var maxVal = (2 * layer + 1) * (2 * layer + 1) - 1;
        var diff = maxVal - n;

        int x = 0, y = 0;

        if (diff < legLen)
        {
            x = -layer + diff;
            y = layer;
        }
        else if (diff < legLen * 2)
        {
            x = layer;
            y = layer - (diff - legLen);
        }
        else if (diff < legLen * 3)
        {
            x = layer - (diff - legLen * 2);
            y = -layer;
        }
        else
        {
            x = -layer;
            y = -layer + (diff - legLen * 3);
        }

        return (x, y);
    }
}