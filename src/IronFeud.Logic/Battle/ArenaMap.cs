using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Battle;

/// <summary>An axis-aligned block standing on the ground; <c>baseY</c> sinks it into uneven terrain.</summary>
public readonly struct Block
{
    public readonly Vector3 Min, Max;

    public Block(float x, float z, float w, float d, float h, float baseY = 0f)
    {
        Min = new Vector3(x - w / 2f, baseY, z - d / 2f);
        Max = new Vector3(x + w / 2f, baseY + h, z + d / 2f);
    }

    public Vector3 Center => (Min + Max) / 2f;
    public Vector3 Size => Max - Min;

    public bool ContainsXZ(float x, float z, float pad) =>
        x > Min.X - pad && x < Max.X + pad && z > Min.Z - pad && z < Max.Z + pad;

    /// <summary>Slab test; distance along a unit ray to the entry point, or +inf. Origins inside the box return 0.</summary>
    public float Raycast(Vector3 o, Vector3 dir)
    {
        float tMin = 0f, tMax = float.PositiveInfinity;
        for (int a = 0; a < 3; a++)
        {
            float oa = a == 0 ? o.X : a == 1 ? o.Y : o.Z;
            float da = a == 0 ? dir.X : a == 1 ? dir.Y : dir.Z;
            float lo = a == 0 ? Min.X : a == 1 ? Min.Y : Min.Z;
            float hi = a == 0 ? Max.X : a == 1 ? Max.Y : Max.Z;
            if (MathF.Abs(da) < 1e-9f)
            {
                if (oa < lo || oa > hi) return float.PositiveInfinity;
                continue;
            }
            float t0 = (lo - oa) / da, t1 = (hi - oa) / da;
            if (t0 > t1) (t0, t1) = (t1, t0);
            tMin = MathF.Max(tMin, t0);
            tMax = MathF.Min(tMax, t1);
            if (tMin > tMax) return float.PositiveInfinity;
        }
        return tMin;
    }
}

/// <summary>A spot next to a block where a mech can hide. <see cref="BlockHeight"/> marks tall cover.</summary>
public readonly struct CoverPoint
{
    public readonly Vector3 Position;
    public readonly float BlockHeight;

    public CoverPoint(Vector3 position, float blockHeight)
    {
        Position = position;
        BlockHeight = blockHeight;
    }
}

/// <summary>Look of a map, for the renderer.</summary>
public enum ArenaTheme { Default, NoMansLand }

/// <summary>
/// Battle geometry as plain data: ground (flat at y = 0, or a <see cref="HeightField"/>) plus axis-aligned blocks.
/// Shots, line of sight, AI steering, foot placement and mech collision all query this, so battles run headless;
/// the Godot side only renders it.
/// </summary>
public sealed class ArenaMap : IGround
{
    public readonly List<Block> Blocks = new();
    public readonly List<CoverPoint> Cover = new();

    /// <summary>Hills; null = flat ground at y = 0.</summary>
    public HeightField? Terrain;

    public float Height(float x, float z) => Terrain?.Height(x, z) ?? 0f;

    /// <summary>Playable radius around the origin (AI destinations stay inside); 0 = unbounded.</summary>
    public float Radius;

    /// <summary>Look of the map, for the renderer.</summary>
    public ArenaTheme Theme;

    /// <summary>Trench lines (x, z polylines), for the renderer's wire pickets.</summary>
    public readonly List<Vector2[]> Trenches = new();

    /// <summary>Spawn distance from the centre for the arena's two teams (±z).</summary>
    public const float ArenaSpawnZ = 190f;

    /// <summary>
    /// The prototype's arena generator, scaled 2.5× for genre speeds (prototype: 95 m ring, 36 blocks of 5-16 m).
    /// A 240 m ring with ~70 larger blocks: the mean free sight line grows with the doubled engagement ranges
    /// (~150 m vs the prototype's ~75 m). Spawn zones at z = ±190 kept open (and flat). With <paramref name="hills"/>,
    /// rolling terrain up to ~±10 m.
    /// </summary>
    public static ArenaMap Arena(uint seed, bool hills = true)
    {
        const float r = 240f, gap = 14f;
        var map = new ArenaMap { Radius = r };
        if (hills) map.Terrain = Hills(seed, r);
        var rng = new DeterministicRng(seed);
        for (int tries = 0; map.Blocks.Count < 70 && tries < 8000; tries++)
        {
            float w = 8f + rng.NextFloat() * 18f, d = 8f + rng.NextFloat() * 18f;
            float h = 6f + MathF.Pow(rng.NextFloat(), 1.6f) * 28f;
            float x = rng.Range(-1f, 1f) * (r - 12f), z = rng.Range(-1f, 1f) * (r - 12f);
            if (MathF.Sqrt(x * x + z * z) > r - 10f) continue;
            if (MathF.Abs(z) > 0.65f * r && MathF.Abs(x) < 65f) continue; // spawn zones
            var b = new Block(x, z, w, d, h, map.BaseUnder(x, z, w, d));
            bool ok = true;
            foreach (var o in map.Blocks)
                if (b.Max.X + gap > o.Min.X && b.Min.X - gap < o.Max.X && b.Max.Z + gap > o.Min.Z && b.Min.Z - gap < o.Max.Z) { ok = false; break; }
            if (ok) map.Blocks.Add(b);
        }
        map.BuildCover();
        return map;
    }

    /// <summary>Open ground with no blocks (the gait parade), flat or hilly, ~±340 m.</summary>
    public static ArenaMap Open(uint seed, bool hills)
    {
        var map = new ArenaMap();
        if (hills) map.Terrain = Hills(seed, 300f, spawns: false);
        return map;
    }

    /// <summary>
    /// Mixed hills on a 3 m grid to ±340 m: rolling noise (±7 m at 140 m), sharp-crested ridges (±4 m at 80 m), and a
    /// handful of steep mounds (8-16 m, flanks 30-42°), flat at the spawns, fading out past <paramref name="radius"/>.
    /// </summary>
    private static HeightField Hills(uint seed, float radius, bool spawns = true)
    {
        uint s = seed * 2654435761u + 17u;
        var f = HeightField.Create(340f, 3f).AddNoise(s, 7f, 140f).AddNoise(s ^ 0x5f3759dfu, 4f, 80f, 2, ridged: true);
        var rng = new DeterministicRng(s);
        for (int i = 0; i < 7; i++)
        {
            float x = rng.Range(-0.8f, 0.8f) * radius, z = rng.Range(-0.6f, 0.6f) * radius;
            f.AddMound(x, z, rng.Range(25f, 45f), rng.Range(8f, 16f), rng.Range(0.6f, 0.9f));
        }
        if (spawns) f.Flatten(0f, ArenaSpawnZ, 45f).Flatten(0f, -ArenaSpawnZ, 45f);
        return f.Fade(radius + 20f);
    }

    /// <summary>
    /// A mech-scale WW1 no man's land in the 240 m ring: a zig-zag trench line with a parapet in front of each spawn
    /// (z = ±145), and between them a cratered waste: dozens of shell craters, a few mine craters deep enough to hide
    /// a mech, steep spoil heaps and ridges that break the long sight lines, and a handful of ruined bunkers.
    /// Spawns at z = ±190 behind the trenches. 2 m grid so craters keep their shape.
    /// </summary>
    public static ArenaMap NoMansLand(uint seed)
    {
        const float r = 240f, trenchZ = 145f;
        var map = new ArenaMap { Radius = r, Theme = ArenaTheme.NoMansLand };
        uint s = seed * 2246822519u + 3u;
        var rng = new DeterministicRng(s);
        var f = HeightField.Create(300f, 2f).AddNoise(s, 1.5f, 50f);

        foreach (float side in new[] { -1f, 1f })
        {
            // Fire bays: the line zig-zags ±8 m every 30 m.
            var line = new Vector2[15];
            for (int i = 0; i < line.Length; i++)
                line[i] = new Vector2(-210f + i * 30f, side * (trenchZ + (i % 2 == 0 ? 8f : -8f)));
            f.AddTrench(line, 6f, 4.5f, 5.5f, new Vector2(0f, -side));
            map.Trenches.Add(line);
        }

        for (int i = 0; i < 3; i++)
            f.AddCrater(rng.Range(-150f, 150f), rng.Range(-90f, 90f), rng.Range(24f, 34f), rng.Range(9f, 12f), rng.Range(3f, 4f));
        for (int i = 0; i < 5; i++)
            f.AddMound(rng.Range(-180f, 180f), rng.Range(-90f, 90f), rng.Range(25f, 40f), rng.Range(9f, 15f), rng.Range(0.7f, 1f));
        for (int i = 0; i < 2; i++)
        {
            float x = rng.Range(-170f, 70f), z = rng.Range(-70f, 70f), a = rng.Range(-0.5f, 0.5f), len = rng.Range(70f, 110f);
            var from = new Vector2(x, z);
            f.AddRidge(from, from + new Vector2(MathF.Cos(a), MathF.Sin(a)) * len, rng.Range(18f, 26f), rng.Range(8f, 12f), 0.8f);
        }
        for (int i = 0; i < 80; i++)
            f.AddCrater(rng.Range(-210f, 210f), rng.Range(-trenchZ + 10f, trenchZ - 10f), rng.Range(7f, 16f), rng.Range(1f, 3f), rng.Range(0.6f, 1.5f));

        f.Flatten(0f, ArenaSpawnZ, 20f).Flatten(0f, -ArenaSpawnZ, 20f); // soft edge stops short of the trenches
        map.Terrain = f.Fade(r + 20f);

        // Ruined bunkers: low concrete stubs scattered in no man's land.
        for (int tries = 0; map.Blocks.Count < 10 && tries < 400; tries++)
        {
            float x = rng.Range(-190f, 190f), z = rng.Range(-115f, 115f), w = rng.Range(6f, 12f), d = rng.Range(6f, 12f);
            if (map.InsideAnyBlock(x, z, 20f)) continue;
            map.Blocks.Add(new Block(x, z, w, d, rng.Range(4f, 7f), map.BaseUnder(x, z, w, d)));
        }
        map.BuildCover();
        return map;
    }

    /// <summary>Base height for a block: the lowest ground under its corners, sunk half a metre.</summary>
    private float BaseUnder(float x, float z, float w, float d)
    {
        float lo = Height(x, z);
        for (int i = 0; i < 4; i++) lo = MathF.Min(lo, Height(x + (i % 2 == 0 ? -w : w) / 2f, z + (i < 2 ? -d : d) / 2f));
        return lo - 0.5f;
    }

    /// <summary>Open test ground: scattered blocks of mixed height, keeping a rectangle (x0, z0, x1, z1) clear.</summary>
    public static ArenaMap Scatter(uint seed, float extent, int count, (float X0, float Z0, float X1, float Z1)? clear = null)
    {
        var map = new ArenaMap();
        var rng = new DeterministicRng(seed);
        for (int i = 0; i < count; i++)
        {
            float h = 3f + rng.NextFloat() * rng.NextFloat() * 26f, w = 3f + rng.NextFloat() * 9f, d = 3f + rng.NextFloat() * 9f;
            float pad = MathF.Max(w, d) * 0.75f, x, z;
            do
            {
                x = rng.Range(-extent, extent);
                z = rng.Range(-extent, extent);
            } while (x * x + z * z < 16f * 16f ||
                     clear is { } c && x > c.X0 - pad && x < c.X1 + pad && z > c.Z0 - pad && z < c.Z1 + pad);
            map.Blocks.Add(new Block(x, z, w, d, h));
        }
        map.BuildCover();
        return map;
    }

    /// <summary>8 candidates per block (sides and corners, 4.5 m out), dropped if outside the arena or inside a block.</summary>
    private void BuildCover()
    {
        const float o = 4.5f;
        Span<Vector2> cand = stackalloc Vector2[8];
        foreach (var b in Blocks)
        {
            Vector3 c = b.Center;
            cand[0] = new(b.Min.X - o, c.Z);
            cand[1] = new(b.Max.X + o, c.Z);
            cand[2] = new(c.X, b.Min.Z - o);
            cand[3] = new(c.X, b.Max.Z + o);
            cand[4] = new(b.Min.X - o, b.Min.Z - o);
            cand[5] = new(b.Max.X + o, b.Min.Z - o);
            cand[6] = new(b.Min.X - o, b.Max.Z + o);
            cand[7] = new(b.Max.X + o, b.Max.Z + o);
            foreach (var p in cand)
            {
                if (Radius > 0f && p.Length() > Radius - 3f) continue;
                if (InsideAnyBlock(p.X, p.Y, 2.5f)) continue;
                Cover.Add(new CoverPoint(new Vector3(p.X, Height(p.X, p.Y), p.Y), b.Size.Y));
            }
        }
        if (Terrain != null) BuildTerrainCover();
    }

    /// <summary>
    /// Terrain cover on a 12 m grid: level-enough spots where the ground rises at least 5 m within 22 m in some
    /// direction (behind a berm or mound, in a crater or trench). Whether a spot hides from a given enemy is decided
    /// at runtime by line of sight; <see cref="CoverPoint.BlockHeight"/> holds the rise.
    /// </summary>
    private void BuildTerrainCover()
    {
        const float spacing = 12f, minRise = 5f;
        float extent = Radius > 0f ? Radius - 6f : Terrain!.HalfExtent - 20f;
        for (float z = -extent; z <= extent; z += spacing)
        for (float x = -extent; x <= extent; x += spacing)
        {
            if (Radius > 0f && x * x + z * z > extent * extent) continue;
            if (InsideAnyBlock(x, z, 3f) || this.NormalAt(x, z, 2f).Y < 0.9f) continue; // not on a wall
            float h0 = Height(x, z), rise = 0f;
            for (int dir = 0; dir < 8; dir++)
            {
                float a = dir * MathF.PI / 4f, c = MathF.Cos(a), sn = MathF.Sin(a);
                foreach (float d in new[] { 8f, 14f, 22f })
                    rise = MathF.Max(rise, Height(x + c * d, z + sn * d) - h0);
            }
            if (rise >= minRise) Cover.Add(new CoverPoint(new Vector3(x, h0, z), rise));
        }
    }

    public bool InsideAnyBlock(float x, float z, float pad)
    {
        foreach (var b in Blocks)
            if (b.ContainsXZ(x, z, pad)) return true;
        return false;
    }

    /// <summary>Distance along a unit ray to the first block or the ground, capped at <paramref name="maxDistance"/>.</summary>
    public float RayDistance(Vector3 origin, Vector3 dir, float maxDistance, bool ground = true)
    {
        float best = maxDistance;
        if (ground && Terrain != null) best = MathF.Min(best, Terrain.Raycast(origin, dir, best));
        else if (ground && dir.Y < -1e-6f)
        {
            float t = -origin.Y / dir.Y;
            if (t >= 0f && t < best) best = t;
        }
        foreach (var b in Blocks)
        {
            float t = b.Raycast(origin, dir);
            if (t < best) best = t;
        }
        return best;
    }

    /// <summary>Line of sight between two points, blocked by blocks and hills (the prototype's 0.3 m slack at the far end).</summary>
    public bool LineClear(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = d.Length();
        if (len < 1e-4f) return true;
        return RayDistance(a, d / len, len, ground: Terrain != null) >= len - 0.3f;
    }

    /// <summary>Steepest climb (rise over run, averaged over 8 m) the AI will steer up; mechs can climb steeper, slowly.</summary>
    public const float MaxClimbGrade = 0.8f;

    /// <summary>
    /// Three parallel horizontal probes (centre, ±halfWidth) at knee height above the ground against blocks, and the
    /// ground along the centre line against climbs steeper than <see cref="MaxClimbGrade"/>.
    /// </summary>
    public bool PathClear(Vector3 position, float heading, float halfWidth, float length, float height = 3f)
    {
        Vector3 fwd = MathUtil.Forward(heading), side = MathUtil.Right(heading) * halfWidth;
        float y = Height(position.X, position.Z) + height;
        if (Terrain != null)
        {
            const float step = 8f;
            float prev = Height(position.X, position.Z);
            for (float d = step; d <= length; d += step)
            {
                Vector3 q = position + fwd * d;
                float h = Height(q.X, q.Z);
                if ((h - prev) / step > MaxClimbGrade) return false;
                prev = h;
            }
        }
        for (int o = -1; o <= 1; o++)
        {
            Vector3 from = new Vector3(position.X, y, position.Z) + side * o;
            if (RayDistance(from, fwd, length, ground: false) < length) return false;
        }
        return true;
    }

    /// <summary>
    /// Keeps an AI destination inside the arena and out of blocks (4 m clearance). The prototype left a block
    /// through the face along its dominant axis; this takes the nearest face that stays inside the ring, so blocks
    /// near the edge can't push a point outside.
    /// </summary>
    public Vector3 ClampDestination(Vector3 p)
    {
        const float pad = 4f;
        float max = Radius - 6f;
        Vector2 q = new(p.X, p.Z);
        if (Radius > 0f && q.Length() > max) q *= max / q.Length();
        Span<Vector2> exits = stackalloc Vector2[4];
        for (int i = 0; i < 3; i++)
            foreach (var b in Blocks)
            {
                if (!b.ContainsXZ(q.X, q.Y, pad)) continue;
                exits[0] = new(b.Min.X - pad, q.Y);
                exits[1] = new(b.Max.X + pad, q.Y);
                exits[2] = new(q.X, b.Min.Z - pad);
                exits[3] = new(q.X, b.Max.Z + pad);
                Vector2 best = exits[0];
                float bestCost = float.PositiveInfinity;
                foreach (var e in exits)
                {
                    float cost = Vector2.Distance(e, q) + (Radius > 0f && e.Length() > max ? 1000f : 0f);
                    if (cost < bestCost) { bestCost = cost; best = e; }
                }
                q = best;
            }
        return new Vector3(q.X, Height(q.X, q.Y), q.Y);
    }

    /// <summary>Pushes a circle (mech footprint) out of every block it overlaps. Returns the corrected ground position.</summary>
    public Vector3 PushOut(Vector3 p, float radius)
    {
        foreach (var b in Blocks)
        {
            float cx = MathUtil.Clamp(p.X, b.Min.X, b.Max.X), cz = MathUtil.Clamp(p.Z, b.Min.Z, b.Max.Z);
            float dx = p.X - cx, dz = p.Z - cz, d2 = dx * dx + dz * dz;
            if (d2 >= radius * radius) continue;
            if (d2 > 1e-8f)
            {
                float d = MathF.Sqrt(d2);
                p.X = cx + dx / d * radius;
                p.Z = cz + dz / d * radius;
                continue;
            }
            // Centre inside the block: leave through the nearest face.
            float l = p.X - b.Min.X, r = b.Max.X - p.X, n = p.Z - b.Min.Z, f = b.Max.Z - p.Z;
            float m = MathF.Min(MathF.Min(l, r), MathF.Min(n, f));
            if (m == l) p.X = b.Min.X - radius;
            else if (m == r) p.X = b.Max.X + radius;
            else if (m == n) p.Z = b.Min.Z - radius;
            else p.Z = b.Max.Z + radius;
        }
        return p;
    }
}
