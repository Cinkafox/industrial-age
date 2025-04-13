using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.World.GenAddition;

public sealed partial class EntWorldGenAddition : GenAddition.IWorldGenAddition
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