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

    /// <summary>A flat field ±<paramref name="halfExtent"/> around the origin; shape it with the Add/Flatten/Fade operations.</summary>
    public static HeightField Create(float halfExtent, float cell) =>
        new((int)MathF.Ceiling(2f * halfExtent / cell) + 1, cell);

    /// <summary>
    /// Value noise, <paramref name="octaves"/> octaves from <paramref name="wavelength"/> down. Ridged noise folds it into
    /// sharp crests and rounded troughs (range about [-1, 1]·amplitude either way).
    /// </summary>
    public HeightField AddNoise(uint seed, float amplitude, float wavelength, int octaves = 3, bool ridged = false)
    {
        var noise = new ValueNoise(seed);
        Each((x, z) =>
        {
            float h = 0f, amp = amplitude, freq = 1f / wavelength;
            for (int o = 0; o < octaves; o++)
            {
                float v = noise.Sample(x * freq, z * freq, o);
                if (ridged) v = 2f * (1f - MathF.Abs(v)) * (1f - MathF.Abs(v)) - 1f;
                h += amp * v;
                amp *= 0.45f;
                freq *= 2.1f;
            }
            return h;
        });
        return this;
    }

    /// <summary>
    /// A round hill whose steepest flank has slope <paramref name="maxGrade"/> (rise over run); steep hills get a flat
    /// top, gentle ones are domes (never gentler than the plain smoothstep dome, 1.5·height/radius).
    /// </summary>
    public HeightField AddMound(float x, float z, float radius, float height, float maxGrade)
    {
        float steepness = MathF.Max(1f, maxGrade * radius / (1.5f * height));
        Around(x, z, radius, (px, pz) =>
        {
            float t = 1f - MathF.Sqrt((px - x) * (px - x) + (pz - z) * (pz - z)) / radius;
            return t <= 0f ? 0f : height * MathUtil.Smoothstep(0f, 1f, MathF.Min(1f, t * steepness));
        });
        return this;
    }

    /// <summary>A long berm along the segment a → b, same profile as <see cref="AddMound"/> across its width.</summary>
    public HeightField AddRidge(Vector2 a, Vector2 b, float halfWidth, float height, float maxGrade)
    {
        float steepness = MathF.Max(1f, maxGrade * halfWidth / (1.5f * height));
        Along(a, b, halfWidth, d =>
        {
            float t = 1f - d / halfWidth;
            return t <= 0f ? 0f : height * MathUtil.Smoothstep(0f, 1f, MathF.Min(1f, t * steepness));
        });
        return this;
    }

    /// <summary>A shell or mine crater: a bowl of <paramref name="depth"/> inside <paramref name="radius"/> with a thrown-up rim.</summary>
    public HeightField AddCrater(float x, float z, float radius, float depth, float rim)
    {
        Around(x, z, radius * 1.9f, (px, pz) =>
        {
            float u = MathF.Sqrt((px - x) * (px - x) + (pz - z) * (pz - z)) / radius;
            float bowl = u < 1f ? -depth * (1f - u * u) : 0f;
            float lip = rim * MathF.Exp(-((u - 1f) * (u - 1f)) / 0.09f);
            return bowl + lip;
        });
        return this;
    }

    /// <summary>
    /// A trench along a polyline: a flat-bottomed ditch, and a parapet berm on the side facing
    /// <paramref name="front"/> (a direction, e.g. toward the enemy). Each node uses its nearest segment, so zig-zag
    /// corners don't dig twice.
    /// </summary>
    public HeightField AddTrench(Vector2[] line, float halfWidth, float depth, float parapetHeight, Vector2 front)
    {
        float reach = 3.5f * halfWidth;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var v in line)
        {
            minX = MathF.Min(minX, v.X); maxX = MathF.Max(maxX, v.X);
            minZ = MathF.Min(minZ, v.Y); maxZ = MathF.Max(maxZ, v.Y);
        }
        int x0 = Math.Max(0, (int)((minX - reach - Origin) / Cell)), x1 = Math.Min(N - 1, (int)((maxX + reach - Origin) / Cell) + 1);
        int z0 = Math.Max(0, (int)((minZ - reach - Origin) / Cell)), z1 = Math.Min(N - 1, (int)((maxZ + reach - Origin) / Cell) + 1);
        for (int iz = z0; iz <= z1; iz++)
        for (int ix = x0; ix <= x1; ix++)
        {
            var p = new Vector2(Origin + ix * Cell, Origin + iz * Cell);
            float best = float.MaxValue;
            bool frontSide = false;
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 a = line[i], ab = line[i + 1] - a;
                float t = MathUtil.Clamp01(Vector2.Dot(p - a, ab) / MathF.Max(1e-6f, ab.LengthSquared()));
                Vector2 off = p - (a + ab * t);
                float d = off.Length();
                if (d >= best) continue;
                best = d;
                frontSide = Vector2.Dot(off, front) > 0f;
            }
            if (best > reach) continue;
            // Walls over 80 % of the half-width (~50° for a 4.5 m deep, 12 m wide trench); parapet ~40° flanks.
            float ditch = -depth * (1f - MathUtil.Smoothstep(halfWidth * 0.2f, halfWidth, best));
            float u = MathF.Abs(best - 1.8f * halfWidth) / (1.6f * halfWidth);
            float berm = frontSide ? parapetHeight * (1f - MathUtil.Smoothstep(0f, 1f, u)) : 0f;
            this[ix, iz] += ditch + berm;
        }
        return this;
    }

    /// <summary>Blends the ground to 0 inside <paramref name="radius"/> (soft edge out to 1.8·radius).</summary>
    public HeightField Flatten(float x, float z, float radius)
    {
        for (int iz = 0; iz < N; iz++)
        for (int ix = 0; ix < N; ix++)
        {
            float px = Origin + ix * Cell - x, pz = Origin + iz * Cell - z;
            this[ix, iz] *= MathUtil.Smoothstep(radius, radius * 1.8f, MathF.Sqrt(px * px + pz * pz));
        }
        return this;
    }

    /// <summary>Fades everything to 0 between <paramref name="start"/> and the edge of the grid.</summary>
    public HeightField Fade(float start)
    {
        float end = HalfExtent - Cell;
        for (int iz = 0; iz < N; iz++)
        for (int ix = 0; ix < N; ix++)
        {
            float px = Origin + ix * Cell, pz = Origin + iz * Cell;
            this[ix, iz] *= 1f - MathUtil.Smoothstep(start, end, MathF.Sqrt(px * px + pz * pz));
        }
        UpdateBounds();
        return this;
    }

    private void Each(Func<float, float, float> add)
    {
        for (int iz = 0; iz < N; iz++)
        for (int ix = 0; ix < N; ix++)
            this[ix, iz] += add(Origin + ix * Cell, Origin + iz * Cell);
    }

    private void Around(float x, float z, float radius, Func<float, float, float> add)
    {
        int x0 = Math.Max(0, (int)((x - radius - Origin) / Cell)), x1 = Math.Min(N - 1, (int)((x + radius - Origin) / Cell) + 1);
        int z0 = Math.Max(0, (int)((z - radius - Origin) / Cell)), z1 = Math.Min(N - 1, (int)((z + radius - Origin) / Cell) + 1);
        for (int iz = z0; iz <= z1; iz++)
        for (int ix = x0; ix <= x1; ix++)
            this[ix, iz] += add(Origin + ix * Cell, Origin + iz * Cell);
    }

    /// <summary>Applies a profile of the distance to segment a → b within <paramref name="reach"/>.</summary>
    private void Along(Vector2 a, Vector2 b, float reach, Func<float, float> profile)
    {
        Vector2 ab = b - a;
        float len2 = MathF.Max(1e-6f, ab.LengthSquared());
        int x0 = Math.Max(0, (int)((MathF.Min(a.X, b.X) - reach - Origin) / Cell)), x1 = Math.Min(N - 1, (int)((MathF.Max(a.X, b.X) + reach - Origin) / Cell) + 1);
        int z0 = Math.Max(0, (int)((MathF.Min(a.Y, b.Y) - reach - Origin) / Cell)), z1 = Math.Min(N - 1, (int)((MathF.Max(a.Y, b.Y) + reach - Origin) / Cell) + 1);
        for (int iz = z0; iz <= z1; iz++)
        for (int ix = x0; ix <= x1; ix++)
        {
            var p = new Vector2(Origin + ix * Cell, Origin + iz * Cell);
            float t = MathUtil.Clamp01(Vector2.Dot(p - a, ab) / len2);
            float d = Vector2.Distance(p, a + ab * t);
            if (d <= reach) this[ix, iz] += profile(d);
        }
    }

    /// <summary>
    /// Makes the field point-symmetric through the centre, h(x, z) = h(-x, -z), keeping the north half: the south
    /// half becomes it rotated 180°, crossfaded over a band of ±<paramref name="blend"/> m around z = 0 so there is no
    /// seam (a plain copy left 8:1 cliffs along the centre line). Symmetric everywhere because the crossfade weights
    /// of a point and its mirror image add up to one. Battle spawns mirror the same way.
    /// </summary>
    public HeightField MirrorThroughCentre(float blend = 30f)
    {
        var original = (float[])Heights.Clone();
        for (int iz = 0; iz < N; iz++)
        {
            float z = Origin + iz * Cell, w = MathUtil.Smoothstep(-blend, blend, z);
            for (int ix = 0; ix < N; ix++)
                Heights[iz * N + ix] = w * original[iz * N + ix] + (1f - w) * original[(N - 1 - iz) * N + (N - 1 - ix)];
        }
        UpdateBounds();
        return this;
    }

    public void UpdateBounds()
    {
        MaxHeight = 0f;
        foreach (float h in Heights) MaxHeight = MathF.Max(MaxHeight, h);
    }

    /// <summary>Steepest slope (rise over run) between neighbouring nodes, for tests and tuning.</summary>
    public float MaxGrade()
    {
        float g = 0f;
        for (int iz = 0; iz < N - 1; iz++)
        for (int ix = 0; ix < N - 1; ix++)
            g = MathF.Max(g, MathF.Max(MathF.Abs(this[ix + 1, iz] - this[ix, iz]), MathF.Abs(this[ix, iz + 1] - this[ix, iz])) / Cell);
        return g;
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
