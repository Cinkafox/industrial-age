using Robust.Shared.Random;

namespace Content.Shared.World.GenAddition;

[ImplicitDataDefinitionForInheritors]
public partial interface IWorldGenAddition
{
    public void Invoke(WorldGenData data, WorldTileEntry entry, Vector2i pos);
}