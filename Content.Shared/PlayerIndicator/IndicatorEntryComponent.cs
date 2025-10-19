using Robust.Shared.GameStates;

namespace Content.Shared.PlayerIndicator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IndicatorEntryComponent : Component
{
    [DataField, AutoNetworkedField] public float Value;
    [DataField, AutoNetworkedField] public float MaxValue;
    [DataField, AutoNetworkedField] public float MinValue;
}