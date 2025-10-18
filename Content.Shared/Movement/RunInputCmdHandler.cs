using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

public sealed class RunInputCmdHandler : InputCmdHandler
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