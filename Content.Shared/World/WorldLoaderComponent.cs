namespace Content.Shared.World;

[RegisterComponent]
public sealed partial class WorldLoaderComponent : Component
{
    [DataField] public int Radius = 3;
    [DataField] public Vector2i EntChunkPos = new Vector2i(0, 0);
}