using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.World.GenAddition;

public sealed partial class EntWorldGenAddition : IWorldGenAddition
{
    [DataField] public EntProtoId Entity;
    [DataField] public Angle Rotation;
    [DataField] public Angle RandomAngleMax;
    [DataField] public Vector2 Shift;
    [DataField] public float SpawnСhance = 0.25f;
    [DataField] public HashSet<string> TileWhitelist = new();
    [DataField] public bool NoEntityRequired = true;
    public void Invoke(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        if(NoEntityRequired && entry.Entities.Count != 0) 
            return;
        
        if(TileWhitelist.Count != 0 && !TileWhitelist.Contains(entry.TileDefinition)) 
            return;
        
        if(SpawnСhance < data.GetRandom().NextDouble()) 
            return;

        if (RandomAngleMax != 0)
        {
            Rotation += new Angle(data.GetRandom().NextDouble() * RandomAngleMax);
        }
        
        entry.AddEntity(Entity, Rotation, Shift);
    }
}