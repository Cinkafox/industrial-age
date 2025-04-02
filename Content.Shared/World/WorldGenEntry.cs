using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[Serializable, NetSerializable]
public record struct WorldGenEntry(float Height, Robust.Shared.Map.Tile Tile, HashSet<EntProtoId> Entities);