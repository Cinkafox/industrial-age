using Robust.Shared.GameStates;

namespace Content.Shared.PlayerIndicator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlayerIndicatorComponent : Component
{
    [DataField, AutoNetworkedField] public List<NetEntity> IndicatorEntities = [];
}
