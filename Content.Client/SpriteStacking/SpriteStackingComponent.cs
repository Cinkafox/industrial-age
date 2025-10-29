using Robust.Shared.Utility;

namespace Content.Client.SpriteStacking;

[RegisterComponent]
public sealed partial class SpriteStackingComponent : Component
{
    [DataField] public ResPath Path = ResPath.Empty;
    [DataField] public string State = "";
    [DataField] public bool UpdateStateOnSpriteChange;
}