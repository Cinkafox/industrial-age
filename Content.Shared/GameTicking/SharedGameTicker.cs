using System.Numerics;
using Content.Shared.StateManipulation;
using Content.Shared.World;
using Content.Shared.World.Systems;
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
    [Dependency] protected readonly WorldGenSystem WorldGenSystem = default!;

    private bool _isGameInitialized;
    protected bool IsServer;
    
    public const bool MappingMode = true;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;

    public void InitializeGame()
    {
        if (_isGameInitialized) 
            throw new Exception();

        _isGameInitialized = true;
        
        MapUid = MapSystem.CreateMap(!MappingMode);

        if (MappingMode)
            return;
        
        GridUid = MapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        AddComp<WorldGenComponent>(GridUid);
        
        GridUid.Comp.CanSplit = false;
        RaiseLocalEvent(new GameInitializedEvent(MapUid, GridUid));
    }
}

public record struct GameInitializedEvent(EntityUid MapUid, EntityUid GridUid);