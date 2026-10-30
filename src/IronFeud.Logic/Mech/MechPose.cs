using System.Numerics;

namespace IronFeud.Logic.Mech;

/// <summary>
/// Render-ready pose in Godot convention. Pelvis is in world space; everything else is local to its rig parent
/// (see MechRig for the hierarchy).
/// </summary>
public sealed class MechPose
{
    public Vector3 PelvisPosition;
    public Quaternion PelvisRotation = Quaternion.Identity;

    /// <summary>Torso base (twist ring) relative to the pelvis.</summary>
    public Quaternion TorsoRotation = Quaternion.Identity;
    public Quaternion HeadRotation = Quaternion.Identity;

    /// <summary>Index 0 = left, 1 = right.</summary>
    public readonly Quaternion[] Shoulders = { Quaternion.Identity, Quaternion.Identity };
    public readonly Quaternion[] Elbows = { Quaternion.Identity, Quaternion.Identity };

    public readonly LegPose[] Legs = { new(), new() };

    // World-space upper body, for weapons and aiming.
    public readonly Vector3[] ElbowWorldPositions = new Vector3[2];
    public readonly Quaternion[] ElbowWorldRotations = { Quaternion.Identity, Quaternion.Identity };

    /// <summary>Torso aim ray: yaw from the torso (including sway), pitch from the aim actuator. Weapons converge on it.</summary>
    public Vector3 AimOrigin;
    public Vector3 AimDirection = -Vector3.UnitZ;

    /// <summary>Signed shake this tick, for cameras.</summary>
    public float Jank;
}

public sealed class LegPose
{
    public Quaternion Hip = Quaternion.Identity;
    public Quaternion Knee = Quaternion.Identity;
    public Quaternion Ankle = Quaternion.Identity;

    // World-space values for debug drawing and tests.
    public FootTarget Target;
    public Vector3 HipWorld;
    public Vector3 KneeWorld;
    public Vector3 AnkleWorld;
    public Quaternion FootWorld = Quaternion.Identity;
}
