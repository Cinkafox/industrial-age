using Robust.Shared.Input;

namespace Content.Shared.Input;

[KeyFunctions]
public static class ContentKeyFunctions
{
    public static readonly BoundKeyFunction ActivateItemInWorld = "ActivateItemInWorld";
    public static readonly BoundKeyFunction MouseMiddle = "MouseMiddle";
    public static readonly BoundKeyFunction RotateObjectClockwise = "RotateObjectClockwise";
    public static readonly BoundKeyFunction RotateObjectCounterclockwise = "RotateObjectCounterclockwise";
    public static readonly BoundKeyFunction FlipObject = "FlipObject";
    public static readonly BoundKeyFunction ToggleRoundEndSummaryWindow = "ToggleRoundEndSummaryWindow";
    public static readonly BoundKeyFunction OpenEntitySpawnWindow = "OpenEntitySpawnWindow";
    public static readonly BoundKeyFunction OpenSandboxWindow = "OpenSandboxWindow";
    public static readonly BoundKeyFunction OpenTileSpawnWindow = "OpenTileSpawnWindow";
    public static readonly BoundKeyFunction OpenDecalSpawnWindow = "OpenDecalSpawnWindow";
    public static readonly BoundKeyFunction OpenAdminMenu = "OpenAdminMenu";
    public static readonly BoundKeyFunction TakeScreenshot = "TakeScreenshot";
    public static readonly BoundKeyFunction TakeScreenshotNoUI = "TakeScreenshotNoUI";
    public static readonly BoundKeyFunction ToggleFullscreen = "ToggleFullscreen";
    
    public static readonly BoundKeyFunction EditorCopyObject = "EditorCopyObject";
    public static readonly BoundKeyFunction EditorFlipObject = "EditorFlipObject";
    public static readonly BoundKeyFunction InspectEntity = "InspectEntity";

    public static readonly BoundKeyFunction MappingUnselect = "MappingUnselect";
    public static readonly BoundKeyFunction SaveMap = "SaveMap";
    public static readonly BoundKeyFunction MappingEnablePick = "MappingEnablePick";
    public static readonly BoundKeyFunction MappingEnableDelete = "MappingEnableDelete";
    public static readonly BoundKeyFunction MappingPick = "MappingPick";
    public static readonly BoundKeyFunction MappingRemoveDecal = "MappingRemoveDecal";
    public static readonly BoundKeyFunction MappingCancelEraseDecal = "MappingCancelEraseDecal";
    public static readonly BoundKeyFunction MappingOpenContextMenu = "MappingOpenContextMenu";
}