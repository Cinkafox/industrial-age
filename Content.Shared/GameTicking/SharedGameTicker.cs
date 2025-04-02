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
        
        RaiseLocalEvent(new GameInitializedEvent(MapUid, GridUid));
    }

    public void SpawnRotate(EntityUid uid, Angle angle)
    {
        var transform = Transform(uid);
        transform.LocalRotation = angle;
    }
}

public record struct GameInitializedEvent(EntityUid MapUid, EntityUid GridUid);