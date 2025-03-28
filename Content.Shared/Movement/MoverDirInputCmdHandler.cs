using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

sealed class MoverDirInputCmdHandler : InputCmdHandler
{
    private readonly InputMoverController _controller;
    private readonly Angle _angle;

    public MoverDirInputCmdHandler(InputMoverController controller, Direction direction)
    {
        _controller = controller;
        _angle = direction.ToAngle();
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null) return false;
        
        _controller.HandleDirChange(session.AttachedEntity.Value, _angle, message.SubTick, message.State == BoundKeyState.Down);
        return false;
    }
}

sealed class RunInputCmdHandler : InputCmdHandler
{
    private readonly InputMoverController _controller;

    public RunInputCmdHandler(InputMoverController controller)
    {
        _controller = controller;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null) return false;
        
        _controller.HandleRunChange(session.AttachedEntity.Value, message.SubTick, message.State == BoundKeyState.Down);
        return false;
    }
}

sealed class RotateCameraInputCmdHandler : InputCmdHandler
{
    private readonly InputMoverController _controller;
    private readonly Angle _angle;

    public RotateCameraInputCmdHandler(InputMoverController controller, Angle angle)
    {
        _controller = controller;
        _angle = angle;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null || !entManager.TryGetComponent<EyeComponent>(session.AttachedEntity, out var eyeComponent)) return false;

        entManager.System<SharedEyeSystem>().SetRotation(session.AttachedEntity.Value, eyeComponent.Rotation + _angle);
        return false;
    }
}