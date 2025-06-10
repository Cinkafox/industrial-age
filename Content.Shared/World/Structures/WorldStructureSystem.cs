using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Shared.World.Structures;

public sealed class WorldStructureSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoaderSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<WorldStructureComponent,ComponentInit>(OnInit);
    }

    private void OnInit(Entity<WorldStructureComponent> ent, ref ComponentInit args)
    {
        var parent = Transform(ent).ParentUid;
        var position = (Vector2i)Transform(ent).LocalPosition;
        if(!TryComp<MapGridComponent>(parent, out var mapGrid))
            return;
        
        _mapSystem.CreateMap(out var mapId);

        if (!TryLoadGrid(mapId, ent.Comp.StructurePath, out var grid, out var entities)) 
            return;

        foreach (var tile in _mapSystem.GetAllTiles(grid.Value,grid.Value.Comp))
        {
            var realPos = position + new Vector2i(tile.X, tile.Y);
            _mapSystem.SetTile(parent, mapGrid, realPos, tile.Tile);
        }

        foreach (var entity in entities)
        {
            var entPosition = Transform(entity).LocalPosition + position;
            _transformSystem.SetCoordinates(entity, new EntityCoordinates(parent, entPosition));
        }
        
        _mapSystem.DeleteMap(mapId);
    }
    
    public bool TryLoadGrid(
        MapId map,
        ResPath path,
        [NotNullWhen(true)] out Entity<MapGridComponent>? grid,[NotNullWhen(true)] out HashSet<EntityUid>? entities)
    {
        entities = null;
        var opts = new MapLoadOptions
        {
            MergeMap = map,
            ExpectedCategory = FileCategory.Grid
        };

        grid = null;
        if (!_mapLoaderSystem.TryLoadGeneric(path, out var result, opts))
            return false;
        
        if (result.Grids.Count == 1)
        {
            grid = result.Grids.Single();
            entities = result.Entities;
            return true;
        }

        _mapLoaderSystem.Delete(result);
        return false;
    }
}