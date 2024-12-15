using Robust.Shared.Utility;

namespace Content.Client.SpriteStacking;

[RegisterComponent]
public sealed partial class SpriteStackingComponent: Component
{
    [DataField] public ResPath Path = default;
    [DataField] public string State = "";
    [DataField] public bool UpdateStateOnSpriteChange = false;
    [ViewVariables] public SpriteStackingData Data = default!;
}