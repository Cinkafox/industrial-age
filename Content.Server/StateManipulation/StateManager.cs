using Content.Shared.DependencyRegistration;
using Content.Shared.StateManipulation;
using Robust.Shared.Player;

namespace Content.Server.StateManipulation;

[DependencyRegister(typeof(IContentStateManager))]
public sealed class ContentStateManager : SharedContentStateManager
{
    public override void Initialize()
    {
        base.Initialize();
        NetManager.RegisterNetMessage<SessionStateChangeMessage>();
    }
    
    public override void SetState(ICommonSession session,string stateType)
    {
        var message = new SessionStateChangeMessage()
        {
           type = stateType
        };
        
        NetManager.ServerSendMessage(message,session.Channel);
    }
}
