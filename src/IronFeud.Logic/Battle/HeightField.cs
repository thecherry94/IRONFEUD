using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Battle;

/// <summary>
/// Terrain as a regular height grid (bilinear between nodes) centred on the origin. Outside the grid the height is 0.
/// Plain data: battles stay headless and deterministic.
/// </summary>
public sealed class HeightField : IGround
{
    public readonly int N;
    public readonly float Cell;
    public readonly float[] Heights;
    public float MaxHeight { get; private set; }

    /// <summary>World x/z of node (0, 0).</summary>
    public float Origin => -0.5f * (N - 1) * Cell;
    public float HalfExtent => 0.5f * (N - 1) * Cell;

    public HeightField(int n, float cell)
    {
        N = n;
        Cell = cell;
        Heights = new float[n * n];
    }

    public float this[int ix, int iz]
    {
        get => Heights[iz * N + ix];
        set => Heights[iz * N + ix] = value;
    }

    public float Height(float x, float z)
    {
        float fx = (x - Origin) / Cell, fz = (z - Origin) / Cell;
        if (fx < 0f || fz < 0f || fx > N - 1 || fz > N - 1) return 0f;
        int ix = Math.Min((int)fx, N - 2), iz = Math.Min((int)fz, N - 2);
        float tx = fx - ix, tz = fz - iz;
        float h0 = MathUtil.Lerp(this[ix, iz], this[ix + 1, iz], tx);
        float h1 = MathUtil.Lerp(this[ix, iz + 1], this[ix + 1, iz + 1], tx);
        return MathUtil.Lerp(h0, h1, tz);
    }

    /// <summary>
    /// Distance along a unit ray to the terrain surface, or +inf. Marches in half-cell steps and refines the
    /// crossing by bisection. Rays that stay above <see cref="MaxHeight"/> are rejected at once.
    /// </summary>
    public float Raycast(Vector3 origin, Vector3 dir, float maxDistance)
    {
        if (origin.Y > MaxHeight && dir.Y >= 0f) return float.PositiveInfinity;
        float step = 0.5f * Cell, t0 = 0f;
        float above0 = origin.Y - Height(origin.X, origin.Z);
        if (above0 <= 0f) return 0f;
        for (float t = step; ; t += step)
        {
            bool last = t >= maxDistance;
            if (last) t = maxDistance;
            Vector3 p = origin + dir * t;
            float above = p.Y - Height(p.X, p.Z);
            if (above <= 0f)
            {
                float a = t0, b = t;
                for (int i = 0; i < 8; i++)
                {
                    float m = 0.5f * (a + b);
                    Vector3 q = origin + dir * m;
                    if (q.Y - Height(q.X, q.Z) > 0f) a = m; else b = m;
                }
                return b;
            }
            if (last) return float.PositiveInfinity;
            if (p.Y > MaxHeight && dir.Y >= 0f) return float.PositiveInfinity;
            t0 = t;
        }
    }

    /// <summary>
    /// Rolling hills from value noise (3 octaves, base wavelength <paramref name="wavelength"/>), faded to 0 beyond
    /// <paramref name="fadeStart"/> from the centre and flattened around <paramref name="flats"/> (x, z, radius).
    /// </summary>
    public static HeightField Hills(uint seed, float halfExtent, float cell, float amplitude, float wavelength, float fadeStart,
        params (float X, float Z, float Radius)[] flats)
    {
        int n = (int)MathF.Ceiling(2f * halfExtent / cell) + 1;
        var f = new HeightField(n, cell);
        var noise = new ValueNoise(seed);
        float fadeEnd = halfExtent - cell;
        for (int iz = 0; iz < n; iz++)
        for (int ix = 0; ix < n; ix++)
        {
            float x = f.Origin + ix * cell, z = f.Origin + iz * cell;
            float h = 0f, amp = amplitude, freq = 1f / wavelength;
            for (int o = 0; o < 3; o++)
            {
                h += amp * noise.Sample(x * freq, z * freq, o);
                amp *= 0.45f;
                freq *= 2.1f;
            }
            float r = MathF.Sqrt(x * x + z * z);
            h *= 1f - MathUtil.Smoothstep(fadeStart, fadeEnd, r);
            foreach (var (fx, fz, fr) in flats)
            {
                float d = MathF.Sqrt((x - fx) * (x - fx) + (z - fz) * (z - fz));
                h *= MathUtil.Smoothstep(fr, fr * 1.8f, d);
            }
            f[ix, iz] = h;
        }
        f.UpdateBounds();
        return f;
    }

    public void UpdateBounds()
    {
        MaxHeight = 0f;
        foreach (float h in Heights) MaxHeight = MathF.Max(MaxHeight, h);
    }

    /// <summary>Smooth value noise in [-1, 1] on an integer lattice, hashed from the seed.</summary>
    private readonly struct ValueNoise
    {
        private readonly uint _seed;

        public ValueNoise(uint seed) => _seed = seed;

        public float Sample(float x, float z, int octave)
        {
            int ix = (int)MathF.Floor(x), iz = (int)MathF.Floor(z);
            float tx = MathUtil.Smootherstep(x - ix), tz = MathUtil.Smootherstep(z - iz);
            float a = Lattice(ix, iz, octave), b = Lattice(ix + 1, iz, octave);
            float c = Lattice(ix, iz + 1, octave), d = Lattice(ix + 1, iz + 1, octave);
            return MathUtil.Lerp(MathUtil.Lerp(a, b, tx), MathUtil.Lerp(c, d, tx), tz);
        }

        private float Lattice(int x, int z, int octave)
        {
            uint h = _seed ^ (uint)x * 0x27d4eb2du ^ (uint)z * 0x165667b1u ^ (uint)octave * 0x9e3779b9u;
            h ^= h >> 15;
            h *= 0x85ebca6bu;
            h ^= h >> 13;
            h *= 0xc2b2ae35u;
            h ^= h >> 16;
            return (h >> 8) * (2f / 16777216f) - 1f;
        }
    }
}
