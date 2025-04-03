using System.Numerics;
using Content.Shared.Tile;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[Serializable, NetSerializable]
public sealed class WorldTileEntry(float height, Robust.Shared.Map.Tile tile, ProtoId<ContentTileDefinition> tileDefinition)
{
    public float Height { get; } = height;
    public ProtoId<ContentTileDefinition> TileDefinition { get; } = tileDefinition;
    public Robust.Shared.Map.Tile Tile { get; set; } = tile;
    public HashSet<EntitySpawnContext> Entities { get; } = [];

    public void AddEntity(EntProtoId entId, Angle rotation = new Angle(), Vector2 shift = new Vector2())
    {
        Entities.Add(new EntitySpawnContext(entId, rotation, shift));
    }
}

[Serializable, NetSerializable]
public record struct EntitySpawnContext(EntProtoId Entity, Angle Rotation, Vector2 Offset);