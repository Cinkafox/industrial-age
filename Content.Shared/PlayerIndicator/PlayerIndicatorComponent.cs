using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.PlayerIndicator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PlayerIndicatorComponent : Component
{
    [DataField, AutoNetworkedField] public List<Enum> Indicators = new();
}
