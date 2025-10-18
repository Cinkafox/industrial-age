using System.Numerics;
using Content.Shared.Stamina;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Serialization;
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
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, MoveButtons.Up))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, MoveButtons.Left))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, MoveButtons.Right))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, MoveButtons.Down))
            .Bind(EngineKeyFunctions.Walk, new RunInputCmdHandler(this))
            .Register<InputMoverController>();
    }

    public void HandleDirChange(EntityUid sessionAttachedEntity, MoveButtons buttons, ushort messageSubTick, bool isDown)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;
        
        if (isDown)
            inputMoverComponent.PushedButtons |= buttons;
        else
            inputMoverComponent.PushedButtons &= ~buttons;
    }

    public void HandleRunChange(EntityUid sessionAttachedEntity, ushort messageSubTick, bool isRunning)
    {
        if(!_inputMoverQuery.TryComp(sessionAttachedEntity, out var inputMoverComponent))
            return;
        
        inputMoverComponent.IsRunning = isRunning;
        Dirty(sessionAttachedEntity, inputMoverComponent);
    }
}

[Flags]
[Serializable, NetSerializable]
public enum MoveButtons : byte
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Walk = 16,
    AnyDirection = Up | Down | Left | Right,
}