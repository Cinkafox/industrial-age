using Content.Shared.Tile;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WorldGenData
{
    [DataField] public HashSet<NoiseAmplitude> NoiseWorld = new();
    [DataField] public float Redistribution = 1f;
    [DataField] public Dictionary<float, ProtoId<ContentTileDefinition>> TileElevation = new();
    [DataField] public ProtoId<ContentTileDefinition> DefaultTile;
}