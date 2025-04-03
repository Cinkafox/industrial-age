using Robust.Shared.Noise;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[DataDefinition, NetSerializable, Serializable]
public sealed partial class NoiseAmplitude
{
    [DataField(required:true)] public FastNoiseLite Noise = default!;
    [DataField] public float Amplitude = 1f;
}