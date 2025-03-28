using System.Numerics;
using Content.Shared.Stamina;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

public sealed class InputMoverController : VirtualController
{
    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, Direction.North))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, Direction.West))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, Direction.East))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, Direction.South))
            .Bind(EngineKeyFunctions.Walk, new RunInputCmdHandler(this))
            .Bind(EngineKeyFunctions.CameraRotateLeft, new RotateCameraInputCmdHandler(this, Angle.FromDegrees(1)))
            .Bind(EngineKeyFunctions.CameraRotateRight, new RotateCameraInputCmdHandler(this, Angle.FromDegrees(-1)))
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

        var curr = inputMoverComponent.PushedButtonCount > 0 ? 1f : 0f;
        
        RaiseLocalEvent(sessionAttachedEntity, new MoveInputEvent()
        {
            OldMagnitude = inputMoverComponent.Magnitude, CurrentMagnitude = curr
        });
        

        inputMoverComponent.Magnitude = curr;
    }

    public void HandleRunChange(EntityUid sessionAttachedEntity, ushort messageSubTick, bool isRunning)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;
        
        inputMoverComponent.IsRunning = isRunning;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        
        if (!IoCManager.Resolve<IGameTiming>().IsFirstTimePredicted) return;
        
        var query = EntityQueryEnumerator<InputMoverComponent, TransformComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var inputMoverComponent, out var transformComponent, out var physicsComponent))
        {
            var speedImpl = 1f;
            if (inputMoverComponent.Magnitude != 0)
            {
                if (inputMoverComponent.IsRunning &&
                    _staminaSystem.UseStamina(uid, inputMoverComponent.StaminaCost * frameTime))
                    speedImpl = 1.5f;
                if (!_staminaSystem.UseStamina(uid, 0))
                    speedImpl = 0.25f;
            }
            
            var eyeAngle = Angle.Zero;
        
            if(TryComp<EyeComponent>(uid, out var eyeComponent)) 
                eyeAngle = eyeComponent.Rotation;

            var delta = (transformComponent.LocalRotation - (inputMoverComponent.Direction)).Normalise();

            var currSpeed = inputMoverComponent.Magnitude * 4f * speedImpl;

            if (currSpeed > inputMoverComponent.Speed) inputMoverComponent.Speed += 10 * frameTime;
            else if (currSpeed < inputMoverComponent.Speed) inputMoverComponent.Speed -= 10 * frameTime;
            
            PhysicsSystem.SetAngularVelocity(uid, -(float)delta * 8f / speedImpl);
            PhysicsSystem.SetLinearVelocity(uid,
                transformComponent.LocalRotation.RotateVec(new Vector2(0, -inputMoverComponent.Speed)));
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

public sealed class MoveInputEvent : EntityEventArgs
{
    public float OldMagnitude;
    public float CurrentMagnitude;
}