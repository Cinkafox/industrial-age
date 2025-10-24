using System.Numerics;
using Content.Shared.World.GenAddition.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.World.GenAddition;

public sealed partial class EntWorldGenAddition : IWorldGenAddition
{
    [DataField] public EntProtoId Entity;
    [DataField] public Angle Rotation;
    [DataField] public Angle RandomAngleMax;
    [DataField] public Vector2 Shift;
    [DataField] public List<IAdditionCondition> Conditions;
    
    [DataField] public bool NoEntityRequired = true;
    public void Invoke(WorldGenData data, WorldTileEntry entry, Vector2i pos)
    {
        foreach (var condition in Conditions)
        {
            if(!condition.CheckCondition(data, entry, pos))
                return;
        }

        if (RandomAngleMax != 0)
        {
            Rotation += new Angle(data.GetRandom().NextDouble() * RandomAngleMax);
        }
        
        entry.AddEntity(Entity, Rotation, Shift);
    }
}