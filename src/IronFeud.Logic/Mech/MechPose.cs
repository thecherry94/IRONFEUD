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
    public Vector3 TorsoWorldPosition;
    public Quaternion TorsoWorldRotation = Quaternion.Identity;
    public readonly Vector3[] ElbowWorldPositions = new Vector3[2];
    public readonly Quaternion[] ElbowWorldRotations = { Quaternion.Identity, Quaternion.Identity };

    /// <summary>Torso aim ray: yaw from the torso (including sway), pitch from the aim actuator. Weapons converge on it.</summary>
    public Vector3 AimOrigin;
    public Vector3 AimDirection = -Vector3.UnitZ;

    /// <summary>World-space hit spheres, indexed by <see cref="Damage.MechSection"/>.</summary>
    public readonly Damage.HitVolume[] HitVolumes = new Damage.HitVolume[Damage.MechDamage.SectionCount];

    /// <summary>World-space cover plates (see <see cref="MechChassis.ShoulderCover"/>): left shoulder's, then right's.</summary>
    public Damage.HitBox[] HitBoxes = System.Array.Empty<Damage.HitBox>();

    /// <summary>Signed shake this tick, for cameras.</summary>
    public float Jank;

    /// <summary>How far the hips are lowered so the legs reach their footholds (smoothed, metres).</summary>
    public float Crouch;
}

/// <summary>
/// Joint rotations, each local to its parent: hip → knee → (hock, digitigrade only) → ankle. The ankle is the foot
/// joint at the IK end point: the ankle of a plantigrade leg, the toe joint of a digitigrade one.
/// </summary>
public sealed class LegPose
{
    public Quaternion Hip = Quaternion.Identity;
    public Quaternion Knee = Quaternion.Identity;

    /// <summary>The solved joint angles behind the rotations (for debugging and the pose-quality tests).</summary>
    public LegAngles Angles;

    /// <summary>Digitigrade only: the backward-pointing joint between shin and raised foot bone.</summary>
    public Quaternion Hock = Quaternion.Identity;
    public Quaternion Ankle = Quaternion.Identity;

    // World-space values for debug drawing and tests.
    public FootTarget Target;
    public Vector3 HipWorld;
    public Vector3 KneeWorld;

    /// <summary>Digitigrade: the hock joint; plantigrade: equal to <see cref="AnkleWorld"/>.</summary>
    public Vector3 HockWorld;
    public Vector3 AnkleWorld;
    public Quaternion FootWorld = Quaternion.Identity;
}
