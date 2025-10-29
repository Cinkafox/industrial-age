using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

public sealed class InputMoverController : VirtualController
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    
    public override void Initialize()
    {
        base.Initialize();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, MoveButtons.Up))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, MoveButtons.Left))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, MoveButtons.Right))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, MoveButtons.Down))
            .Bind(EngineKeyFunctions.Walk, new MoverDirInputCmdHandler(this, MoveButtons.Nitro))
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        if(!_gameTiming.IsFirstTimePredicted) 
            return;

        var inputQueryEnumerator = AllEntityQuery<InputMoverComponent, TransformComponent>();
        while (inputQueryEnumerator.MoveNext(out var uid, out var inputMoverComponent, out var transformComponent))
        {
            var dir = inputMoverComponent.PushedButtons.ToDirection();
           
            if(dir == DirectionFlag.None || !inputMoverComponent.MovementEnabled) continue;
            transformComponent.LocalPosition -= dir.AsDir().ToVec()*frameTime*5f;
        }
    }
}

[Flags]
[Serializable, NetSerializable]
public enum MoveButtons : byte
{
    None = 0,
    
    Up = 1,
    Left = 2,
    Down = 4,
    Right = 8,
    
    Break = 16,
    Nitro = 32,
}

public static class MoveButtonsExtensions
{
    public static DirectionFlag ToDirection(this MoveButtons moveButtons)
    {
        if(moveButtons.HasFlag(MoveButtons.Break))
            moveButtons &= ~MoveButtons.Break;
        if(moveButtons.HasFlag(MoveButtons.Nitro))
            moveButtons &= ~MoveButtons.Nitro;
        
        return (DirectionFlag)moveButtons;
    }
}