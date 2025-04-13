using System.Linq;
using Content.Shared.Tile;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WorldGenData
{
    [DataField] public HashSet<NoiseAmplitude> NoiseWorld = new();
    [DataField] public float Redistribution = 1f;
    [DataField] public Dictionary<float, ProtoId<ContentTileDefinition>> TileElevation = new();
    [DataField] public ProtoId<ContentTileDefinition> DefaultTile;
    [DataField] public HashSet<GenAddition.IWorldGenAddition> Additions = new();
    
    public ProtoId<ContentTileDefinition> GetTile(float height)
    {
        var tileElevation = TileElevation;
        var heights = tileElevation.Keys.ToList();

        var first = 0f;
        var second = 0f;

        for (int i = 0; i < heights.Count-1; i++)
        {
            first = heights[i];
            second = heights[i + 1];
            if (height > first && height <= second) return tileElevation[first];
        }
        
        if(height > second) 
            return tileElevation[second];

        return DefaultTile;
    }
    
    public float GetNoise(Vector2i pos)
    {
        var sum = 0f;
        var amplitudeSum = 0f;
        
        foreach (var amplitude in NoiseWorld)
        {
            sum += amplitude.Noise.GetNoise(pos.X, pos.Y) * amplitude.Amplitude;
            amplitudeSum += amplitude.Amplitude;
        }
        
        sum = sum / amplitudeSum;

        return float.Pow(sum, Redistribution);
    }
}