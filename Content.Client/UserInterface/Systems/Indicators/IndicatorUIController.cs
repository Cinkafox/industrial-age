using Content.Client.UserInterface.Systems.Indicators.Controls;
using Content.Shared.PlayerIndicator;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Indicators;

public sealed class IndicatorUIController : UIController
{
    public Controls.PlayerIndicator? Indicator;
    
    private EntityUid Player = EntityUid.Invalid;
    
    public override void Initialize()
    {
        base.Initialize();
        
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

        
    }

    public void UpdateData(Dictionary<string, IndicatorBar> values)
    {
        if (Indicator == null) return;
        
        foreach (var (name, indicatorBar) in values)
        {
            Indicator.ChangeValue(name, indicatorBar.Value, indicatorBar.MaxValue);
        }
    }
}
