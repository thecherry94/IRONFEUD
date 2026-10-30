using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

public struct LegAngles
{
    /// <summary>Hip roll about local Z, applied outside the pitch.</summary>
    public float Roll;

    /// <summary>Hip pitch about local X; positive swings the thigh forward (-Z).</summary>
    public float HipPitch;

    /// <summary>Knee angle about local X; negative swings the shin back (+Z), i.e. a forward-pointing knee.</summary>
    public float Knee;
}

/// <summary>
/// Analytic 2-bone IK in the hip-root frame (unrotated relative to the pelvis, leg hanging along -Y).
/// Hip basis = Rz(roll) · Rx(hipPitch); knee basis = Rx(knee). Godot convention: forward = -Z.
/// </summary>
public static class LegIK
{
    public static LegAngles Solve(Vector3 d, float l1, float l2)
    {
        float roll = MathF.Atan2(d.X, -d.Y);
        float r = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        float l = MathUtil.Clamp(MathF.Sqrt(r * r + d.Z * d.Z), 1e-4f, l1 + l2 - 1e-3f);
        float kneeInterior = MathF.Acos(MathUtil.Clamp((l1 * l1 + l2 * l2 - l * l) / (2f * l1 * l2), -1f, 1f));
        float alpha = MathF.Atan2(-d.Z, r);
        float beta = MathF.Acos(MathUtil.Clamp((l1 * l1 + l * l - l2 * l2) / (2f * l1 * l), -1f, 1f));
        return new LegAngles { Roll = roll, HipPitch = alpha + beta, Knee = -(MathF.PI - kneeInterior) };
    }

    public static Quaternion HipRotation(in LegAngles a) => Rot.Z(a.Roll) * Rot.X(a.HipPitch);
    public static Quaternion KneeRotation(in LegAngles a) => Rot.X(a.Knee);

    /// <summary>Forward kinematics in the hip-root frame; returns knee and ankle positions.</summary>
    public static (Vector3 Knee, Vector3 Ankle) Forward(in LegAngles a, float l1, float l2)
    {
        Quaternion hip = HipRotation(a);
        Vector3 knee = Vector3.Transform(new Vector3(0f, -l1, 0f), hip);
        Vector3 ankle = knee + Vector3.Transform(new Vector3(0f, -l2, 0f), hip * KneeRotation(a));
        return (knee, ankle);
    }
}
