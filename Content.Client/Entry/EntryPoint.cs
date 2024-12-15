using Content.Client.Connection;
using Content.Shared.Input;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IStyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public bool IsSingleplayer = false;
    
    public override void PreInit()
    {
        StyleSheetify.Client.DependencyRegistration.Register(IoCManager.Instance!);
        IoCManager.BuildGraph();
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
        _userInterfaceManager.SetDefaultTheme("DefaultTheme"); 
        _styleSheetManager.ApplyStyleSheet("default");
       ContentContexts.SetupContexts(_inputManager.Contexts);
       _userInterfaceManager.MainViewport.Visible = false;
       
       _baseClient.RunLevelChanged += (_, args) =>
       {
           if (args.NewLevel == ClientRunLevel.Initialize)
           {
               SwitchState(args.OldLevel is ClientRunLevel.Connected or ClientRunLevel.InGame);
           }
       };
       
       _prototypeManager.PrototypesReloaded += PrototypeReload;
       
       SwitchState();
    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        _styleSheetManager.Reload();
    }

    private void SwitchState(bool disconnected = false)
    {
        _stateManager.RequestStateChange<ConnectingState>();
        var state = (ConnectingState)_stateManager.CurrentState;
        
        if(disconnected)
        {
            state.Message("Disconnected..");
            return;
        }

        if (_gameController.LaunchState.FromLauncher) 
            return;

        if (IsSingleplayer)
        {
            state.Message("Start singleplayer..");
            _baseClient.StartSinglePlayer();
        }
        else
        {
            state.Message("Connect to local server..");
            _baseClient.ConnectToServer("127.0.0.1",1212);
        }
    }
}