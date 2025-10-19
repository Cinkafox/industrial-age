using Content.Client.UserInterface.Systems.Indicators.Controls;
using Content.Shared.PlayerIndicator;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Indicators;

public sealed class IndicatorUIController : UIController
{
    public PlayerIndicator? Indicator;
    private EntityUid _player = EntityUid.Invalid;
    
    public override void Initialize()
    {
        IoCManager.InjectDependencies(this);
                
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDeatach);
    }

    private void OnDeatach(LocalPlayerDetachedEvent ev)
    {
        if (Indicator == null) return;
        
        _player = EntityUid.Invalid;
        Indicator.ClearIndicators();
        Indicator.Visible = false;
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        if (Indicator == null) return;
        if (!EntityManager.TryGetComponent<PlayerIndicatorComponent>(ev.Entity, out var _)) return;
        
        Indicator.Visible = true;
        _player = ev.Entity;
    }

    public void Register(PlayerIndicator indicator)
    {
        Indicator = indicator;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (Indicator == null) return;
        
        if (!EntityManager.TryGetComponent<PlayerIndicatorComponent>(_player, out var playerIndicatorComponent)) return;

        foreach (var indicator in playerIndicatorComponent.IndicatorEntities)
        {
            Indicator.UpdateValue(EntityManager.GetEntity(indicator));
        }
    }
}
