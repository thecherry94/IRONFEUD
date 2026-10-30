using System;
using System.Numerics;

namespace IronFeud.Logic.Core;

/// <summary>
/// Scalar and planar helpers. World convention matches Godot: +Y up, forward = -Z,
/// heading psi rotates about +Y (positive = turn left).
/// </summary>
public static class MathUtil
{
    public const float Tau = MathF.PI * 2f;

    public static float Clamp(float x, float a, float b) => MathF.Min(b, MathF.Max(a, x));
    public static float Clamp01(float x) => Clamp(x, 0f, 1f);
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public static float Smoothstep(float a, float b, float x)
    {
        float t = Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    /// <summary>Ken Perlin's smootherstep on [0,1].</summary>
    public static float Smootherstep(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    public static float WrapAngle(float a) => MathF.Atan2(MathF.Sin(a), MathF.Cos(a));
    public static float LerpAngle(float a, float b, float t) => a + WrapAngle(b - a) * t;

    /// <summary>Wraps a phase into [0,1).</summary>
    public static float Frac(float x) => x - MathF.Floor(x);

    /// <summary>Rotates a vector about +Y by <paramref name="psi"/>.</summary>
    public static Vector3 RotateY(Vector3 v, float psi)
    {
        float c = MathF.Cos(psi), s = MathF.Sin(psi);
        return new Vector3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
    }

    /// <summary>World forward for heading psi: (-sin psi, 0, -cos psi).</summary>
    public static Vector3 Forward(float psi) => new(-MathF.Sin(psi), 0f, -MathF.Cos(psi));

    /// <summary>World right for heading psi: (cos psi, 0, -sin psi).</summary>
    public static Vector3 Right(float psi) => new(MathF.Cos(psi), 0f, -MathF.Sin(psi));

    public static float PlanarDistance(Vector3 a, Vector3 b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
}
