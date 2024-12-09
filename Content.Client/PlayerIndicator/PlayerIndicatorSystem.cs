using Content.Client.UserInterface.Systems.Indicators;
using Content.Shared.PlayerIndicator;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;

namespace Content.Client.PlayerIndicator;

public sealed class ClientPlayerIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerIndicatorComponent, ComponentHandleState>(OnHandleState);
    }
    
    private void OnHandleState(EntityUid uid, PlayerIndicatorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PlayerIndicatorComponentState state || args.Next is not PlayerIndicatorComponentState next)
            return;
        
        foreach (var (name, indicatorBar) in state.Values)
        {
            component.Values[name] = indicatorBar;
        }
        
        
        _userInterfaceManager.GetUIController<IndicatorUIController>().UpdateData(state.Values);
    }
}