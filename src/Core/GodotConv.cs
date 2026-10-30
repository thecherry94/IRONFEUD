using Godot;
using N = System.Numerics;

namespace IronFeud.Core;

/// <summary>Bridges IronFeud.Logic (System.Numerics) and Godot math types. Both use the same axes.</summary>
public static class GodotConv
{
    public static Vector3 ToGodot(this N.Vector3 v) => new(v.X, v.Y, v.Z);
    public static Quaternion ToGodot(this N.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
    public static N.Vector3 ToLogic(this Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>Transform for a unit-height, Y-aligned mesh stretched between two world points.</summary>
    public static Transform3D Segment(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = Mathf.Max(0.01f, d.Length());
        Vector3 dir = d / len;
        Vector3 axis = Vector3.Up.Cross(dir);
        Basis rot = axis.LengthSquared() < 1e-10f
            ? (dir.Y > 0f ? Basis.Identity : new Basis(Vector3.Right, Mathf.Pi))
            : new Basis(axis.Normalized(), Mathf.Acos(Mathf.Clamp(Vector3.Up.Dot(dir), -1f, 1f)));
        return new Transform3D(rot * Basis.FromScale(new Vector3(1f, len, 1f)), a + d * 0.5f);
    }
}
