using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent]
public sealed partial class InputMoverComponent : Component
{
    [ViewVariables] public Angle Direction = Angle.Zero;
    [ViewVariables] public int PushedButtonCount = 0;
    [ViewVariables] public float Magnitude = 0f;
}