using System;

namespace IronFeud.Logic.Core;

/// <summary>Small seeded PRNG (xorshift32) so simulation runs are reproducible.</summary>
public sealed class DeterministicRng
{
    private uint _state;

    public DeterministicRng(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed;

    public uint NextUInt()
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        return _state = x;
    }

    /// <summary>Uniform in [0,1).</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    public float Range(float a, float b) => a + (b - a) * NextFloat();

    /// <summary>Standard normal sample (Box-Muller).</summary>
    public float NextGaussian()
    {
        float u = 1f - NextFloat(), v = NextFloat();
        return MathF.Sqrt(-2f * MathF.Log(u)) * MathF.Cos(2f * MathF.PI * v);
    }
}
