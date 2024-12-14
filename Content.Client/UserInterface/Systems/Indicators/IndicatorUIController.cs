using Content.Client.UserInterface.Systems.Indicators.Controls;
using Content.Shared.PlayerIndicator;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Indicators;

public sealed class IndicatorUIController : UIController
{
    private AppearanceSystem _appearanceSystem = default!;
    
    public PlayerIndicator? Indicator;
    
    private EntityUid Player = EntityUid.Invalid;
    
    public override void Initialize()
    {
        IoCManager.InjectDependencies(this);
        
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDeatach);
    }

    private void OnDeatach(LocalPlayerDetachedEvent ev)
    {
        if (Indicator == null) return;
        
        Player = EntityUid.Invalid;
        Indicator.ClearIndicators();
        Indicator.Visible = false;
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        if (Indicator == null) return;
        if (!EntityManager.TryGetComponent<PlayerIndicatorComponent>(ev.Entity, out var _)) return;
        _appearanceSystem = EntityManager.System<AppearanceSystem>();
        
        Indicator.Visible = true;
        Player = ev.Entity;
        Indicator?.SetEntity(ev.Entity);
    }

    public void Register(Controls.PlayerIndicator indicator)
    {
        Indicator = indicator;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (Indicator == null) return;
        
        if (!EntityManager.TryGetComponent<PlayerIndicatorComponent>(Player, out var playerIndicatorComponent)) return;

        foreach (var indicator in playerIndicatorComponent.Indicators)
        {
            if (!_appearanceSystem.TryGetData<float>(Player, indicator, out var value))
            {
                Logger.Error("NO DATA " + indicator);
                continue;
            }
            
            Indicator.ChangeValue(Loc.GetString($"indicator-{indicator.ToString()}"), value);
        }
    }
}
