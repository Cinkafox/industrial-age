using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InputMoverComponent : Component
{
    //TODO: Отделить говно от основных приколов
    [ViewVariables, AutoNetworkedField] public Angle Direction = Angle.Zero;
    [ViewVariables, AutoNetworkedField] public DirectionFlag WalkDirection = DirectionFlag.None;
    [ViewVariables, AutoNetworkedField] public float Magnitude = 0f;
    [ViewVariables, AutoNetworkedField] public bool IsRunning = false;
    [ViewVariables, AutoNetworkedField] public float BaseSpeed = 15f;
    [ViewVariables, AutoNetworkedField] public float StaminaCost = 10f;
    [ViewVariables, AutoNetworkedField] public float Speed = 0f;
}

[Flags]
[Serializable, NetSerializable]
public enum WalkDirection: sbyte
{
    None = 0,
    South = 1 << 0,
    East = 1 << 1,
    North = 1 << 2,
    West = 1 << 3,

    SouthEast = South | East,
    NorthEast = North | East,
    NorthWest = North | West,
    SouthWest = South | West,
}

public static class WalkDirectionHelpers
{
    public static Angle ToAngle(this WalkDirection walkDirection)
    {
        return walkDirection switch
        {
            WalkDirection.None => Angle.Zero,
            WalkDirection.South => Angle.FromDegrees(180),
            WalkDirection.East => Angle.FromDegrees(90),
            WalkDirection.North => Angle.FromDegrees(0),
            WalkDirection.West => Angle.FromDegrees(270),
            WalkDirection.SouthEast => Angle.FromDegrees(135),
            WalkDirection.NorthEast => Angle.FromDegrees(45),
            WalkDirection.NorthWest => Angle.FromDegrees(315),
            WalkDirection.SouthWest => Angle.FromDegrees(225),
            _ => Angle.Zero
        };
    }
}