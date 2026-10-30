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
}
