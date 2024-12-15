using Content.Client.Game;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed class SandboxUIController : UIController, IOnStateChanged<GameState>
{
    [Dependency] private readonly IInputManager _input = default!;
    

    // TODO hud refactor cache
    private EntitySpawningUIController EntitySpawningController => UIManager.GetUIController<EntitySpawningUIController>();
    private TileSpawningUIController TileSpawningController => UIManager.GetUIController<TileSpawningUIController>();
    

    public void OnStateEntered(GameState state)
    {
        _input.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                //TODO: ADMIN CHECK
                EntitySpawningController.ToggleWindow();
            }));
        _input.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                TileSpawningController.ToggleWindow();
            }));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorCopyObject, new PointerInputCmdHandler(Copy))
            .Register<SandboxUIController>();
    }

    public void OnStateExited(GameState state)
    {
        CommandBinds.Unregister<SandboxUIController>();
    }
    

    private bool Copy(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        return false;
    }
    
}