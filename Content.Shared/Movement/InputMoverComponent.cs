using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent]
public sealed partial class InputMoverComponent : Component
{
    
    [ViewVariables] public GameTick LastInputTick;
    [ViewVariables] public ushort LastInputSubTick;
    
    [ViewVariables] public Angle Direction = Angle.Zero;
    [ViewVariables] public float Magnitude = 0f;
}