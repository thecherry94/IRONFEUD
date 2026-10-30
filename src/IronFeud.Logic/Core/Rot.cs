using System;
using System.Numerics;

namespace IronFeud.Logic.Core;

/// <summary>Quaternion builders that match Godot's axis and Euler conventions.</summary>
public static class Rot
{
    public static Quaternion X(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, a);
    public static Quaternion Y(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, a);
    public static Quaternion Z(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, a);

    /// <summary>Godot's default Node3D Euler order (YXZ): R = Ry · Rx · Rz.</summary>
    public static Quaternion Yxz(float x, float y, float z) => Y(y) * X(x) * Z(z);

    /// <summary>
    /// Heading <paramref name="yaw"/> tilted so local +Y lines up with <paramref name="up"/> (a sole on a slope):
    /// local -Z stays as close to the heading as the tilt allows.
    /// </summary>
    public static Quaternion YawOnSlope(float yaw, Vector3 up)
    {
        Vector3 back = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        Vector3 x = Vector3.Normalize(Vector3.Cross(up, back));
        Vector3 z = Vector3.Cross(x, up);
        var m = new Matrix4x4(x.X, x.Y, x.Z, 0f, up.X, up.Y, up.Z, 0f, z.X, z.Y, z.Z, 0f, 0f, 0f, 0f, 1f);
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(m));
    }
}
