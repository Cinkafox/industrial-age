using Content.Shared.Tile;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WorldGenData
{
    [DataField] public HashSet<NoiseAmplitude> NoiseWorld = new();
    [DataField] public float Redistribution = 1f;
    [DataField] public Dictionary<float, ProtoId<ContentTileDefinition>> TileElevation = new();
    [DataField] public ProtoId<ContentTileDefinition> DefaultTile;
    [DataField] public HashSet<IWorldGenAddition> Additions = new();
}

[ImplicitDataDefinitionForInheritors]
public partial interface IWorldGenAddition
{
    public void Invoke(WorldGenData data, WorldTileEntry entry, Vector2i pos, IRobustRandom random);
}


public sealed partial class EntWorldGenAddition : IWorldGenAddition
{
    [DataField] public EntProtoId Entity;
    [DataField] public float SpawnСhance = 0.25f;
    [DataField] public HashSet<string> TileWhitelist = new();
    [DataField] public bool NoEntityRequired = true;
    public void Invoke(WorldGenData data, WorldTileEntry entry, Vector2i pos, IRobustRandom random)
    {
        if(NoEntityRequired && entry.Entities.Count != 0) 
            return;
        if(SpawnСhance < random.Next(1.0)) 
            return;
        if(TileWhitelist.Count != 0 && !TileWhitelist.Contains(entry.TileDefinition)) 
            return;
        entry.AddEntity(Entity);
    }
}