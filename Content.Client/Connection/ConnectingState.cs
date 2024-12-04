using Robust.Client;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Connection;

public sealed class ConnectingState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    
    public const string LOCAL_ADDRESS = "ss14://localhost:1212/";
    public string? Address => _gameController.LaunchState.Ss14Address 
                              ?? _gameController.LaunchState.ConnectAddress ?? LOCAL_ADDRESS;
    
    public BoxContainer MessageContainer = new BoxContainer()
    {
        Margin = new Thickness(12),
        Orientation = BoxContainer.LayoutOrientation.Vertical
    };
    
    protected override void Startup()
    {
        _userInterfaceManager.StateRoot.AddChild(MessageContainer);
        
        _clientNetManager.ConnectFailed += OnConnectFailed;
        _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;
        
        //Message($"Connecting to: {Address}");
    }

    protected override void Shutdown()
    {
        _clientNetManager.ConnectFailed -= OnConnectFailed;
        _clientNetManager.ClientConnectStateChanged -= OnConnectStateChanged;
        
        _userInterfaceManager.StateRoot.RemoveChild(MessageContainer);
    }
    
    private void OnConnectStateChanged(ClientConnectionState state)
    {
        Message(state.ToString());
    }

    private void OnConnectFailed(object? sender, NetConnectFailArgs args)
    {
        if (args.RedialFlag)
        {
            Redial();
        }
        Message($"Error while connecting to the server. Reason: {args.Reason}" );
    }
    
    public void Message(string message)
    {
        MessageContainer.AddChild(new Label()
        {
            Text = message
        });
    }
    
    public bool Redial()
    {
        Message("Trying to redial...");
        try
        {
            if (_gameController.LaunchState.Ss14Address != null)
            {
                _gameController.Redial(_gameController.LaunchState.Ss14Address);
                return true;
            }
            else
            {
                Message($"Redial not possible, no Ss14Address");
            }
        }
        catch (Exception ex)
        {
            Message($"Redial exception: {ex}");
        }
        return false;
    }
}