using Content.Shared.PlayerIndicator;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Stamina;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly PlayerIndicatorSystem _playerIndicatorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<StaminaComponent,ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StaminaComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<StaminaComponent> ent, ref ComponentRemove args)
    {
        _playerIndicatorSystem.Remove(ent.Owner, StaminaIndicator.State);
    }

    private void OnComponentInit(Entity<StaminaComponent> ent, ref ComponentInit args)
    {
        _playerIndicatorSystem.Add(ent.Owner, StaminaIndicator.State, ent.Comp.MaxStamina);
        ChangeStamina(ent.Owner, ent.Comp.Stamina);
    }

    public void ChangeStamina(Entity<StaminaComponent?> entity, float value)
    {
        if(!Resolve(entity, ref entity.Comp)) 
            return;
        
        entity.Comp.Stamina = float.Clamp(entity.Comp.Stamina + value, 0f, entity.Comp.MaxStamina);
        
        _playerIndicatorSystem.Set(entity.Owner, StaminaIndicator.State, entity.Comp.Stamina);
    }

    public bool UseStamina(Entity<StaminaComponent?> entity, float cost)
    {
        if(!Resolve(entity, ref entity.Comp)) 
            return false;

        var staminaAfter = entity.Comp.Stamina - cost;
        entity.Comp.Stamina = float.Max(staminaAfter, 0);
        entity.Comp.NextRegenerate = _gameTiming.CurTime + entity.Comp.RegenerateDelay;
        
        _playerIndicatorSystem.Set(entity.Owner, StaminaIndicator.State, entity.Comp.Stamina);
        return staminaAfter > 0;
    }
    
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StaminaComponent>();
        while (query.MoveNext(out var uid, out var staminaComponent))
        {
            if (staminaComponent.StaminaUse != 0)
            {
                staminaComponent.NextRegenerate = _gameTiming.CurTime + staminaComponent.RegenerateDelay;
                ChangeStamina(uid, - staminaComponent.StaminaUse);
            }
            else if (_gameTiming.CurTime > staminaComponent.NextRegenerate)
            {
                ChangeStamina(uid, staminaComponent.StaminaRegenerate * frameTime);
            }
        }
    }
}

[Serializable, NetSerializable]
public enum StaminaIndicator : byte
{
    State,
}