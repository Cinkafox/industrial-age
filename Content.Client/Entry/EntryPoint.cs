using Content.Client.Connection;
using Content.Shared.Input;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
        
        //AUTOSCALING default Setup!
        _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffX", 1080);
        _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffY", 720);
        _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffX", 520);
        _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffY", 240);
        _configManager.SetCVar("interface.resolutionAutoScaleMinimum", 0.5f);
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
            //_baseClient.ConnectToServer("127.0.0.1",1212);
            _baseClient.StartSinglePlayer();
            return;
        }

        _stateManager.RequestStateChange<ConnectingState>();
    }
}