using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InputMoverComponent : Component
{
    //TODO: Отделить говно от основных приколов
    [ViewVariables, AutoNetworkedField] public Angle Direction = Angle.Zero;
    [ViewVariables, AutoNetworkedField] public int PushedButtonCount = 0;
    [ViewVariables, AutoNetworkedField] public float Magnitude = 0f;
    [ViewVariables, AutoNetworkedField] public bool IsRunning = false;
    [ViewVariables, AutoNetworkedField] public float StaminaCost = 10f;
    [ViewVariables, AutoNetworkedField] public float Speed = 0f;
}