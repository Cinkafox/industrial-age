using Robust.Shared.Timing;

namespace Content.Shared.PlayerIndicator;

public sealed class PlayerIndicatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public void Remove(Entity<PlayerIndicatorComponent?> entity, Enum indicator)
    {
        _appearanceSystem.RemoveData(entity, indicator);
        
        if(!Resolve(entity, ref entity.Comp)) return;
        
        entity.Comp.Indicators.Remove(indicator);
    }

    public void Add(Entity<PlayerIndicatorComponent?> entity, Enum indicator, float max)
    {
        Set(entity, indicator, max);
    }
    
    public void Set(Entity<PlayerIndicatorComponent?> entity, Enum indicator, float value)
    {
        if(!_gameTiming.IsFirstTimePredicted) return;
        
        _appearanceSystem.SetData(entity, indicator, value);
        
        if(!Resolve(entity, ref entity.Comp)) return;
        
        if (!entity.Comp.Indicators.Contains(indicator))
        {
            entity.Comp.Indicators.Add(indicator);
        }
    }
}