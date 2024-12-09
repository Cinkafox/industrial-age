using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.PlayerIndicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlayerIndicatorComponent : Component
{
    [DataField] public Dictionary<string, IndicatorBar> Values = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class IndicatorBar
{
    [DataField] public float Value;
    [DataField] public float MaxValue;
}

[Serializable, NetSerializable]
public sealed class PlayerIndicatorComponentState : IComponentState
{
    public Dictionary<string, IndicatorBar> Values = default!;
}