using System.Numerics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Content.Shared.Movement;

public sealed class InputMoverController : VirtualController
{
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

    public void HandleDirChange(EntityUid sessionAttachedEntity, Angle angle, ushort messageSubTick, bool isDown)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;

        if (isDown)
        {
            inputMoverComponent.Direction = angle;
            inputMoverComponent.PushedButtonCount += 1;
        }
        else
        {
            inputMoverComponent.PushedButtonCount -= 1;
        }

        inputMoverComponent.Magnitude = inputMoverComponent.PushedButtonCount > 0 ? 1f : 0f;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        
        var query = EntityQueryEnumerator<InputMoverComponent,TransformComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var inputMoverComponent, out var transformComponent, out var physicsComponent))
        {
            var delta = (transformComponent.LocalRotation - inputMoverComponent.Direction).Normalise();
            
            PhysicsSystem.SetAngularVelocity(uid, -(float)delta * 8f);
            PhysicsSystem.SetLinearVelocity(uid,
                transformComponent.LocalRotation.RotateVec(new Vector2(0, -inputMoverComponent.Magnitude*4f)));
        }
    }
}

public static class AngleExt
{
    public static Angle Normalise(this Angle angle)
    {
        while (angle > MathF.PI)
            angle -= 2 * MathF.PI;
        while (angle < -MathF.PI)
            angle += 2 * MathF.PI;
        return angle;
    }
}