using Robust.Shared.Prototypes;

namespace Content.Shared.World;

[RegisterComponent]
public sealed partial class WorldGenComponent : Component
{
    
    [DataField] public WorldGenData WorldGenData;
    [DataField] public ProtoId<WorldGenPrototype> WorldGenPrototype = "default";
    [DataField] public Dictionary<Vector2i,WorldChunk> LoadedChunks = new(); 
}