using Content.Shared.Tile;
using Robust.Shared.Prototypes;

namespace Content.Shared.World.GenAddition.Conditions;

public sealed partial class TileCondition : IAdditionCondition
{
    [DataField] public HashSet<ProtoId<ContentTileDefinition>> TileWhitelist = new();
    
    public bool CheckCondition(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        return TileWhitelist.Count != 0 && TileWhitelist.Contains(entry.TileDefinition);
    }
}

public sealed partial class SpawnProbabilityCondition : IAdditionCondition
{
    [DataField] public float SpawnProbability;
    
    public bool CheckCondition(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        return SpawnProbability >= data.GetRandom().NextDouble();
    }
}

public sealed partial class EmptyTileRequiredCondition : IAdditionCondition
{
    public bool CheckCondition(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        return entry.Entities.Count != 0;
    }
}

public sealed partial class AdjacentTileRequiredCondition : IAdditionCondition
{
    [DataField] public HashSet<ProtoId<ContentTileDefinition>> Tiles = new();
    
    public bool CheckCondition(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        return CheckTile(data, pos + Vector2i.Up) ||
               CheckTile(data, pos + Vector2i.Left) || 
               CheckTile(data, pos + Vector2i.Down) || 
               CheckTile(data, pos + Vector2i.Right);
    }

    private bool CheckTile(WorldGenData data, Vector2i pos)
    {
        return Tiles.Contains(data.GetTile(pos));
    }
}