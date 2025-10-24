namespace Content.Shared.World.GenAddition.Conditions;

[ImplicitDataDefinitionForInheritors]
public partial interface IAdditionCondition
{
    public bool CheckCondition(WorldGenData data, WorldTileEntry entry, Vector2i pos);
}