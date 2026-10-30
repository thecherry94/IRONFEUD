using System;
using System.Numerics;

namespace IronFeud.Logic.Mech.Damage;

public struct HitVolume
{
    public MechSection Section;
    public Vector3 Center;
    public float Radius;
}

public struct RayHit
{
    public MechSection Section;
    public float Distance;
    public Vector3 Point;
}

public static class HitTest
{
    /// <summary>Entry distance of a ray (unit <paramref name="dir"/>) into a sphere; 0 if the origin is inside.</summary>
    public static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius, out float t)
    {
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, dir);
        float c = oc.LengthSquared() - radius * radius;
        t = 0f;
        if (c <= 0f) return true;
        if (b > 0f) return false;
        float disc = b * b - c;
        if (disc < 0f) return false;
        t = -b - MathF.Sqrt(disc);
        return true;
    }

    /// <summary>Nearest hit volume along the ray within <paramref name="maxDistance"/>.</summary>
    public static bool Raycast(ReadOnlySpan<HitVolume> volumes, Vector3 origin, Vector3 dir, float maxDistance, out RayHit hit)
    {
        hit = default;
        float best = maxDistance;
        bool found = false;
        foreach (var v in volumes)
        {
            if (RaySphere(origin, dir, v.Center, v.Radius, out float t) && t <= best)
            {
                best = t;
                found = true;
                hit = new RayHit { Section = v.Section, Distance = t, Point = origin + dir * t };
            }
        }
        return found;
    }
}
