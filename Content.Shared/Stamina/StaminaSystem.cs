using Content.Shared.PlayerIndicator;
using Robust.Shared.Timing;

namespace Content.Shared.Stamina;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly PlayerIndicatorSystem _playerIndicatorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public static string IndicatorName = "stamina";
    
    public override void Initialize()
    {
        SubscribeLocalEvent<StaminaComponent,ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StaminaComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<StaminaComponent> ent, ref ComponentRemove args)
    {
        _playerIndicatorSystem.RemoveIndicator(ent.Owner, IndicatorName);
    }

    private void OnComponentInit(Entity<StaminaComponent> ent, ref ComponentInit args)
    {
        _playerIndicatorSystem.AddIndicator(ent.Owner, IndicatorName, ent.Comp.MaxStamina);
    }

    public void ChangeStamina(Entity<StaminaComponent?> entity, float value)
    {
        if(!Resolve(entity, ref entity.Comp)) 
            return;
        
        entity.Comp.Stamina = float.Clamp(entity.Comp.Stamina + value, 0f, entity.Comp.MaxStamina);
        _playerIndicatorSystem.ChangeValue(entity.Owner, IndicatorName, entity.Comp.Stamina);
    }

    public bool UseStamina(Entity<StaminaComponent?> entity, float cost)
    {
        if(!Resolve(entity, ref entity.Comp)) 
            return false;

        var staminaAfter = entity.Comp.Stamina - cost;
        entity.Comp.Stamina = float.Max(staminaAfter, 0);
        entity.Comp.NextRegenerate = _gameTiming.CurTime + entity.Comp.RegenerateDelay;
        _playerIndicatorSystem.ChangeValue(entity.Owner, IndicatorName, entity.Comp.Stamina);
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