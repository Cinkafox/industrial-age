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
    [Dependency] private readonly SharedEyeSystem _sharedEyeSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, DirectionFlag.North))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, DirectionFlag.West))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, DirectionFlag.East))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, DirectionFlag.South))
            .Bind(EngineKeyFunctions.Walk, new RunInputCmdHandler(this))
            .Bind(EngineKeyFunctions.CameraRotateLeft, new RotateCameraInputCmdHandler(this, Angle.FromDegrees(1)))
            .Bind(EngineKeyFunctions.CameraRotateRight, new RotateCameraInputCmdHandler(this, Angle.FromDegrees(-1)))
            .Register<InputMoverController>();
    }

    public void HandleDirChange(EntityUid sessionAttachedEntity, DirectionFlag direction, ushort messageSubTick, bool isDown)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;
        
        var doWalk = true;

        if (isDown)
        {
            inputMoverComponent.WalkDirection |= direction;
            if (inputMoverComponent.WalkDirection.TryAsDir(out var dir))
            {
                inputMoverComponent.Direction = dir.ToAngle();
            }
            else
            {
                doWalk = false;
            }
        }
        else
        {
            inputMoverComponent.WalkDirection &= ~direction;
        }
        
        var curr = (inputMoverComponent.WalkDirection == DirectionFlag.None && doWalk) ? 0f : 1f;
        var old = inputMoverComponent.Magnitude;
        
        inputMoverComponent.Magnitude = curr;
        
        Dirty(sessionAttachedEntity, inputMoverComponent);
        
        RaiseLocalEvent(sessionAttachedEntity, new MoveInputEvent()
        {
            OldMagnitude = old, CurrentMagnitude = curr
        });
        
    }

    public void HandleRunChange(EntityUid sessionAttachedEntity, ushort messageSubTick, bool isRunning)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;
        
        inputMoverComponent.IsRunning = isRunning;
        Dirty(sessionAttachedEntity, inputMoverComponent);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        
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
                eyeAngle = eyeComponent.Rotation * 6.9; // Prediction shit, and client has rotation 6.9 more than server. Shit fuck.

            var delta = (transformComponent.LocalRotation - (-eyeAngle + inputMoverComponent.Direction)).Normalise();

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
    
    public static bool TryAsDir(this DirectionFlag directionFlag, out Direction dir)
    {
        switch (directionFlag)
        {
            case DirectionFlag.South:
                dir = Direction.South;
                return true;
            case DirectionFlag.SouthEast:
                dir = Direction.SouthEast;
                return true;
            case DirectionFlag.East:
                dir = Direction.East;
                return true;
            case DirectionFlag.NorthEast:
                dir = Direction.NorthEast;
                return true;
            case DirectionFlag.North:
                dir = Direction.North;
                return true;
            case DirectionFlag.NorthWest:
                dir = Direction.NorthWest;
                return true;
            case DirectionFlag.West:
                dir = Direction.West;
                return true;
            case DirectionFlag.SouthWest:
                dir = Direction.SouthWest;
                return true;
            default:
                dir = Direction.Invalid;
                return false;
        }
    }
}

public sealed class MoveInputEvent : EntityEventArgs
{
    public float OldMagnitude;
    public float CurrentMagnitude;
}