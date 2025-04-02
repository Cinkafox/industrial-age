using Robust.Shared.Noise;
using Robust.Shared.Serialization;

namespace Content.Shared.World;

[Serializable, NetSerializable]
public record struct NoiseAmplitude(FastNoiseLite Noise, float Amplitude);