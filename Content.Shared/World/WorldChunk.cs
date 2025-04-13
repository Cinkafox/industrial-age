using Robust.Shared.Serialization;

namespace Content.Shared.World;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WorldChunk
{
    public static readonly int ChunkSize = 8;
    
    [DataField] public WorldTileEntry[] Entries = new WorldTileEntry[ChunkSize * ChunkSize];

    public WorldTileEntry GetEntry(Vector2i localPosition)
    {
        return Entries[localPosition.X * ChunkSize + localPosition.Y];
    }

    public IEnumerable<(Vector2i, WorldTileEntry)> GetEntries()
    {
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                yield return (new Vector2i(x, y), Entries[x * ChunkSize + y]);
            }
        }
    }
}