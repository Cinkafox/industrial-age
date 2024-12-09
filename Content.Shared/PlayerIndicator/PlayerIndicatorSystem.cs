using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.PlayerIndicator;

public sealed class PlayerIndicatorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerIndicatorComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, PlayerIndicatorComponent component, ref ComponentGetState args)
    {
        args.State = new PlayerIndicatorComponentState
        {
            Values = component.Values,
        };
    }
    
    public void AddIndicator(Entity<PlayerIndicatorComponent?> entity, string name, float maxValue)
    {
        if (!Resolve(entity, ref entity.Comp) || 
            !entity.Comp.Values.TryAdd(name, new IndicatorBar())) return;

        entity.Comp.Values[name].MaxValue = maxValue;
        
        Dirty(entity.Owner, entity.Comp);
    }

    public void RemoveIndicator(Entity<PlayerIndicatorComponent?> entity, string name)
    {
        if (!Resolve(entity, ref entity.Comp)) return;
        
        entity.Comp.Values.Remove(name);
        
        Dirty(entity.Owner, entity.Comp);
    }

    public void ChangeValue(Entity<PlayerIndicatorComponent?> entity, string name, float value)
    {
        if (!Resolve(entity, ref entity.Comp)) return;
        
        if(!entity.Comp.Values.TryGetValue(name, out _)) 
            AddIndicator(entity, name, value);

        if (entity.Comp.Values[name].Value == value) 
            return;
        
        if(entity.Comp.Values[name].MaxValue < value ) 
            entity.Comp.Values[name].MaxValue = value;

        entity.Comp.Values[name].Value = value;
        Dirty(entity.Owner, entity.Comp);
    }
}