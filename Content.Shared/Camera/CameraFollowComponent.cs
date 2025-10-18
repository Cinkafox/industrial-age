namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraFollowComponent : Component
{
    [DataField] public Angle CameraAngleChangeSpeed { get; set; } = Angle.FromDegrees(65);
    [ViewVariables] public Angle CameraAngle { get; set; }
}