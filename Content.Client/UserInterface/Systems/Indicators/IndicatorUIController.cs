using Content.Client.UserInterface.Systems.Indicators.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Indicators;

public sealed class IndicatorUIController : UIController
{
    public HashSet<PlayerIndicator> Indicators = new HashSet<PlayerIndicator>();
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
        
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        foreach (var indicator in Indicators)
        {
            indicator.SetEntity(ev.Entity);
        }
    }

    public void Register(PlayerIndicator indicator)
    {
        Indicators.Add(indicator);
    }
}
