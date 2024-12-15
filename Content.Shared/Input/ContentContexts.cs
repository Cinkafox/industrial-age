using Robust.Shared.Input;

namespace Content.Shared.Input;

public sealed class ContentContexts
{
    public static void SetupContexts(IInputContextContainer contexts)
    {
        var viewer = contexts.GetContext("human");
        viewer.AddFunction(EngineKeyFunctions.GuiTabNavigateNext);
        viewer.AddFunction(EngineKeyFunctions.GuiTabNavigatePrev);
        viewer.AddFunction(EngineKeyFunctions.UIClick);
        viewer.AddFunction(EngineKeyFunctions.EscapeMenu);
        viewer.AddFunction(EngineKeyFunctions.MoveUp);
        viewer.AddFunction(EngineKeyFunctions.MoveDown);
        viewer.AddFunction(EngineKeyFunctions.MoveLeft);
        viewer.AddFunction(EngineKeyFunctions.MoveRight);
        viewer.AddFunction(EngineKeyFunctions.Walk);
        viewer.AddFunction(ContentKeyFunctions.ActivateItemInWorld);
        
        viewer.AddFunction(ContentKeyFunctions.OpenEntitySpawnWindow);
        viewer.AddFunction(ContentKeyFunctions.OpenSandboxWindow);
        viewer.AddFunction(ContentKeyFunctions.OpenTileSpawnWindow);
        viewer.AddFunction(ContentKeyFunctions.OpenDecalSpawnWindow);
        
        // Not in engine, because engine cannot check for sanbox/admin status before starting placement.
        viewer.AddFunction(ContentKeyFunctions.EditorCopyObject);

        // Not in engine because the engine doesn't understand what a flipped object is
        viewer.AddFunction(ContentKeyFunctions.EditorFlipObject);

        // Not in engine so that the RCD can rotate objects
        viewer.AddFunction(EngineKeyFunctions.EditorRotateObject);
        viewer.AddFunction(EngineKeyFunctions.EditorPlaceObject);
        viewer.AddFunction(EngineKeyFunctions.EditorCancelPlace);
        viewer.AddFunction(EngineKeyFunctions.EditorGridPlace);
        viewer.AddFunction(EngineKeyFunctions.EditorLinePlace);
    }
}