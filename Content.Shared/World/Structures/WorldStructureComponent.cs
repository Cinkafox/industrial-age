using Robust.Shared.Utility;

namespace Content.Shared.World.Structures;

[RegisterComponent]
public sealed partial class WorldStructureComponent : Component
{
    [DataField] public ResPath StructurePath { get; set; }
}