using Content.Shared.DependencyRegistration;
using Content.Shared.StateManipulation;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Content.Client.StateManipulation;

[DependencyRegister(typeof(IContentStateManager))]
public sealed class ContentStateManager : SharedContentStateManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        NetManager.RegisterNetMessage<SessionStateChangeMessage>(OnSessionChange);
    }

    private void OnSessionChange(SessionStateChangeMessage message) => SetState(message.type);

    public void SetState(string state)
    {
        SetState(_playerManager.LocalSession!, state);
    }

    public override void SetState(ICommonSession session, string state)
    {
        if (session != _playerManager.LocalSession!)
            throw new Exception();

        var stateType = _reflectionManager.GetType(state);
        
        _stateManager.RequestStateChange(stateType!);
    }
}
