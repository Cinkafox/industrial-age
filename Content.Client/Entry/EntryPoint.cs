using Content.Client.Connection;
using Content.Client.Launcher;
using Content.Shared.Input;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
       ContentContexts.SetupContexts(_inputManager.Contexts);
       _userInterfaceManager.SetDefaultTheme("DefaultTheme");
       _userInterfaceManager.MainViewport.Visible = false;
       
       _baseClient.RunLevelChanged += (_, args) =>
       {
           if (args.NewLevel == ClientRunLevel.Initialize)
           {
               SwitchState(args.OldLevel == ClientRunLevel.Connected ||
                                    args.OldLevel == ClientRunLevel.InGame);
           }
       };
       
       SwitchState();
    }

    private void SwitchState(bool disconnected = false)
    {
        if (!_gameController.LaunchState.FromLauncher)
        {
            _baseClient.ConnectToServer("127.0.0.1",1212);
        }

        _stateManager.RequestStateChange<ConnectingState>();
    }
}