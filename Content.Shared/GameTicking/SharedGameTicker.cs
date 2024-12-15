using System.Numerics;
using Content.Shared.StateManipulation;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.GameTicking;

public abstract partial class SharedGameTicker : EntitySystem
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedMapSystem MapSystem = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedPointLightSystem LightSystem = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] protected readonly IContentStateManager ContentStateManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly INetManager _netManager = default!;

    private bool _isGameInitialized;
    protected bool _isServer;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    
    public override void Initialize()
    {
        
    }

    public void InitializeGame()
    {
        if (_isGameInitialized) 
            throw new Exception();

        _isGameInitialized = true;

        MapUid = MapSystem.CreateMap(out var mapId);
        GridUid = MapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        
        var width = 55;
        var height = 55;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var sx = x - width / 2;
                var sy= y - height / 2;
                
                MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            }
        }
        
        Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(2,2)));
        Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(3,2)));
        Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(1,2)));
        
        SpawnRotate(Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(2,-2))),Angle.FromDegrees(180));
        SpawnRotate(Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(3,-2))),Angle.FromDegrees(180));
        SpawnRotate(Spawn("WallTest", new EntityCoordinates(GridUid, new Vector2(1,-2))),Angle.FromDegrees(180));
    }

    public void SpawnRotate(EntityUid uid, Angle angle)
    {
        var transform = Transform(uid);
        transform.LocalRotation = angle;
    }
}