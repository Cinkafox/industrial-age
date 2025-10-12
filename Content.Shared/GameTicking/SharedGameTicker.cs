using System.Numerics;
using Content.Shared.StateManipulation;
using Content.Shared.World;
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
    [Dependency] protected readonly SharedPointLightSystem LightSystem = default!;
    [Dependency] protected readonly IContentStateManager ContentStateManager = default!;
    [Dependency] protected readonly INetManager NetManager = default!;

    private bool _isGameInitialized;
    protected bool IsServer;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;

    public void InitializeGame()
    {
        if (_isGameInitialized) 
            throw new Exception();

        _isGameInitialized = true;

        MapUid = MapSystem.CreateMap();
        GridUid = MapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        AddComp<WorldGenComponent>(GridUid);
        RaiseLocalEvent(new GameInitializedEvent(MapUid, GridUid));
    }
}

public record struct GameInitializedEvent(EntityUid MapUid, EntityUid GridUid);