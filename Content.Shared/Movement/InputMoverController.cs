using System.Numerics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

public sealed class InputMoverController : VirtualController
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    
    public override void Initialize()
    {
        base.Initialize();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, Direction.North))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, Direction.West))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, Direction.East))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, Direction.South))
            .Register<InputMoverController>();
    }

    public void HandleDirChange(EntityUid sessionAttachedEntity, Angle angle, ushort messageSubTick, bool b)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;

        inputMoverComponent.Direction = angle;
        inputMoverComponent.Magnitude = b ? 1f : 0f;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if(!_timing.InSimulation) 
            return;
        var query = EntityQueryEnumerator<InputMoverComponent,TransformComponent>();

        while (query.MoveNext(out var uid, out var inputMoverComponent, out var transformComponent))
        {
            var delta = transformComponent.LocalRotation - inputMoverComponent.Direction;
            transformComponent.LocalRotation = inputMoverComponent.Direction;
            //_physicsSystem.ApplyTorque(uid, (float)(1f * delta));
            //Logger.Debug(inputMoverComponent.Magnitude + "");
            //_physicsSystem.ApplyForce(uid, transformComponent.LocalRotation.RotateVec(new Vector2(inputMoverComponent.Magnitude * 10, 0)));
            transformComponent.LocalPosition +=
                transformComponent.LocalRotation.RotateVec(new Vector2(0, -inputMoverComponent.Magnitude * 0.2f));
        }
    }
}

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
