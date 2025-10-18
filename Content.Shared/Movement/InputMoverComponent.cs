using Robust.Shared.GameStates;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InputMoverComponent : Component
{
    [ViewVariables, AutoNetworkedField] public MoveButtons PushedButtons;
    [ViewVariables, AutoNetworkedField] public bool IsRunning;
}