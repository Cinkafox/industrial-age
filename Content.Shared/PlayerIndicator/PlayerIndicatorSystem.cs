using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.PlayerIndicator;

public sealed class PlayerIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public void Remove(Entity<PlayerIndicatorComponent?> entity, EntityUid indicatorUid)
    {
        if(!Resolve(entity, ref entity.Comp) || 
           !TryComp<IndicatorEntryComponent>(indicatorUid, out var _)) return;
        
        entity.Comp.IndicatorEntities.Remove(GetNetEntity(indicatorUid));
        EntityManager.DeleteEntity(indicatorUid);
    }

    public EntityUid AddIndicator(Entity<PlayerIndicatorComponent?> entity, EntProtoId<IndicatorEntryComponent> protoId)
    {
        if(!Resolve(entity, ref entity.Comp)) 
            entity.Comp = AddComp<PlayerIndicatorComponent>(entity);
        
        var ent = Spawn(protoId, new EntityCoordinates(entity, Vector2.Zero));
        entity.Comp.IndicatorEntities.Add(GetNetEntity(ent));
        Dirty(entity);
        return ent;
    }
    
    public void Set(EntityUid indicatorUid, float value)
    {
        if(!_gameTiming.IsFirstTimePredicted || 
           !TryComp<IndicatorEntryComponent>(indicatorUid, out var indicatorEntryComponent)) return;

        indicatorEntryComponent.Value = value;
        Dirty(indicatorUid, indicatorEntryComponent);
    }
    
    public bool TryAdd(EntityUid indicatorUid, float value)
    {
        if(!_gameTiming.IsFirstTimePredicted || 
           !TryComp<IndicatorEntryComponent>(indicatorUid, out var indicatorEntryComponent)) return false;

        if (indicatorEntryComponent.Value + value > indicatorEntryComponent.MaxValue)
        {
            indicatorEntryComponent.Value = indicatorEntryComponent.MaxValue;
            Dirty(indicatorUid, indicatorEntryComponent);
            return false;
        }
        
        indicatorEntryComponent.Value += value;
        Dirty(indicatorUid, indicatorEntryComponent);
        return true;
    }

    public bool TrySubtract(EntityUid indicatorUid, float value)
    {
        if(!_gameTiming.IsFirstTimePredicted || 
           !TryComp<IndicatorEntryComponent>(indicatorUid, out var indicatorEntryComponent)) return false;
        
        if(indicatorEntryComponent.Value - value < indicatorEntryComponent.MinValue)
        {
            indicatorEntryComponent.Value = indicatorEntryComponent.MinValue;
            Dirty(indicatorUid, indicatorEntryComponent);
            return false;
        }
        
        indicatorEntryComponent.Value -= value;
        Dirty(indicatorUid, indicatorEntryComponent);
        return true;
    }
}