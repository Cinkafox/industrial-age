using Robust.Shared.Prototypes;

namespace Content.Shared.World;

[Prototype]
public sealed partial class WorldGenPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
    [DataField(required:true)] public WorldGenData Data = default!;
}