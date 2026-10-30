using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Battle;

/// <summary>An axis-aligned block standing on the ground (y = 0).</summary>
public readonly struct Block
{
    public readonly Vector3 Min, Max;

    public Block(float x, float z, float w, float d, float h)
    {
        Min = new Vector3(x - w / 2f, 0f, z - d / 2f);
        Max = new Vector3(x + w / 2f, h, z + d / 2f);
    }

    public Vector3 Center => (Min + Max) / 2f;
    public Vector3 Size => Max - Min;
    public float Height => Max.Y;

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

/// <summary>
/// Battle geometry as plain data: flat ground at y = 0 plus axis-aligned blocks. Shots, line of sight, AI steering
/// and mech collision all query this, so battles run headless; the Godot side only renders it.
/// </summary>
public sealed class ArenaMap
{
    public readonly List<Block> Blocks = new();
    public readonly List<CoverPoint> Cover = new();

    /// <summary>Playable radius around the origin (AI destinations stay inside); 0 = unbounded.</summary>
    public float Radius;

    /// <summary>The prototype's arena: a 95 m ring with 36 blocks, spawn zones at z = ±75 kept open.</summary>
    public static ArenaMap Arena(uint seed)
    {
        const float r = 95f;
        var map = new ArenaMap { Radius = r };
        var rng = new DeterministicRng(seed);
        for (int tries = 0; map.Blocks.Count < 36 && tries < 4000; tries++)
        {
            float w = 5f + rng.NextFloat() * 11f, d = 5f + rng.NextFloat() * 11f;
            float h = 5f + MathF.Pow(rng.NextFloat(), 1.6f) * 20f;
            float x = rng.Range(-1f, 1f) * (r - 8f), z = rng.Range(-1f, 1f) * (r - 8f);
            if (MathF.Sqrt(x * x + z * z) > r - 6f) continue;
            if (MathF.Abs(z) > 62f && MathF.Abs(x) < 26f) continue; // spawn zones
            var b = new Block(x, z, w, d, h);
            bool ok = true;
            foreach (var o in map.Blocks)
                if (b.Max.X + 9f > o.Min.X && b.Min.X - 9f < o.Max.X && b.Max.Z + 9f > o.Min.Z && b.Min.Z - 9f < o.Max.Z) { ok = false; break; }
            if (ok) map.Blocks.Add(b);
        }
        map.BuildCover();
        return map;
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
                Cover.Add(new CoverPoint(new Vector3(p.X, 0f, p.Y), b.Height));
            }
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
        if (ground && dir.Y < -1e-6f)
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

    /// <summary>Line of sight between two points (the prototype's 0.3 m slack at the far end).</summary>
    public bool LineClear(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = d.Length();
        if (len < 1e-4f) return true;
        return RayDistance(a, d / len, len, ground: false) >= len - 0.3f;
    }

    /// <summary>Three parallel horizontal probes (centre, ±halfWidth) at knee height along a heading.</summary>
    public bool PathClear(Vector3 position, float heading, float halfWidth, float length, float height = 3f)
    {
        Vector3 fwd = MathUtil.Forward(heading), side = MathUtil.Right(heading) * halfWidth;
        for (int o = -1; o <= 1; o++)
        {
            Vector3 from = new Vector3(position.X, height, position.Z) + side * o;
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
        return new Vector3(q.X, 0f, q.Y);
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
