using System;
using System.Numerics;

namespace IronFeud.Logic.Mech.Damage;

public struct HitVolume
{
    public MechSection Section;
    public Vector3 Center;
    public float Radius;
}

/// <summary>
/// An armour plate as an oriented box (cover geometry such as shoulder pauldrons), belonging to a section: a hit on it
/// is a hit on that section.
/// </summary>
public struct HitBox
{
    public MechSection Section;
    public Vector3 Center;
    public Quaternion Rotation;
    public Vector3 HalfExtents;
}

public struct RayHit
{
    public MechSection Section;
    public float Distance;
    public Vector3 Point;
}

public static class HitTest
{
    /// <summary>
    /// Entry distance of a ray (unit <paramref name="dir"/>) into a sphere; 0 if the origin is inside. The miss test
    /// uses the ray's perpendicular distance from the centre: the textbook b² - c cancels catastrophically in floats
    /// at range (at 300 m its error is ~0.01 m², so every 0.1 m cockpit was a ~0.1 m target whatever its radius).
    /// </summary>
    public static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius, out float t)
    {
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, dir);
        float r2 = radius * radius;
        t = 0f;
        if (oc.LengthSquared() <= r2) return true;
        if (b > 0f) return false;
        Vector3 perp = oc - b * dir;
        float disc = r2 - perp.LengthSquared();
        if (disc < 0f) return false;
        t = -b - MathF.Sqrt(disc);
        return true;
    }

    /// <summary>Entry distance of a ray (unit <paramref name="dir"/>) into an oriented box; 0 if the origin is inside.</summary>
    public static bool RayBox(Vector3 origin, Vector3 dir, in HitBox box, out float t)
    {
        var inv = Quaternion.Conjugate(box.Rotation);
        Vector3 o = Vector3.Transform(origin - box.Center, inv), d = Vector3.Transform(dir, inv), e = box.HalfExtents;
        float near = 0f, far = float.MaxValue;
        t = 0f;
        return Slab(o.X, d.X, e.X, ref near, ref far) && Slab(o.Y, d.Y, e.Y, ref near, ref far) && Slab(o.Z, d.Z, e.Z, ref near, ref far)
               && (t = near) >= 0f;
    }

    private static bool Slab(float o, float d, float e, ref float near, ref float far)
    {
        if (MathF.Abs(d) < 1e-9f) return MathF.Abs(o) <= e;
        float t0 = (-e - o) / d, t1 = (e - o) / d;
        if (t0 > t1) (t0, t1) = (t1, t0);
        near = MathF.Max(near, t0);
        far = MathF.Min(far, t1);
        return near <= far;
    }

    /// <summary>Nearest hit volume along the ray within <paramref name="maxDistance"/>.</summary>
    public static bool Raycast(ReadOnlySpan<HitVolume> volumes, Vector3 origin, Vector3 dir, float maxDistance, out RayHit hit) =>
        Raycast(volumes, ReadOnlySpan<HitBox>.Empty, 0u, origin, dir, maxDistance, out hit);

    /// <summary>
    /// Nearest hit along the ray among the section spheres and the cover plates (plates of sections in the
    /// <paramref name="skipSections"/> bit mask are gone and ignored). The spheres are coarse stand-ins for the body
    /// (the torso sphere reaches ~1 m past the visible chest); the plates are exact. So a plate takes the hit if the
    /// ray reaches it first, or if it meets the plate inside the sphere it struck: that plate is the body's surface there.
    /// </summary>
    public static bool Raycast(ReadOnlySpan<HitVolume> volumes, ReadOnlySpan<HitBox> boxes, uint skipSections, Vector3 origin, Vector3 dir,
                               float maxDistance, out RayHit hit)
    {
        hit = default;
        float best = maxDistance;
        bool found = false;
        int struck = -1;
        for (int i = 0; i < volumes.Length; i++)
        {
            var v = volumes[i];
            if (RaySphere(origin, dir, v.Center, v.Radius, out float t) && t <= best)
            {
                best = t;
                found = true;
                struck = i;
                hit = new RayHit { Section = v.Section, Distance = t, Point = origin + dir * t };
            }
        }
        float plateBest = float.MaxValue;
        foreach (var b in boxes)
        {
            if ((skipSections & (1u << (int)b.Section)) != 0 || !RayBox(origin, dir, b, out float t) || t > maxDistance || t >= plateBest) continue;
            Vector3 p = origin + dir * t;
            bool first = !found || t <= best;
            bool insideStruck = struck >= 0 && Vector3.DistanceSquared(p, volumes[struck].Center) <= volumes[struck].Radius * volumes[struck].Radius;
            if (!first && !insideStruck) continue;
            plateBest = t;
            hit = new RayHit { Section = b.Section, Distance = t, Point = p };
            found = true;
        }
        return found;
    }
}
