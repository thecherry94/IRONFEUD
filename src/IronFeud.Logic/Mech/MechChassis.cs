using System;
using System.Numerics;

namespace IronFeud.Logic.Mech;

public enum ChassisClass { Light, Medium }

/// <summary>
/// Everything that differs between mech classes: kinematics, gait, rig dimensions, hit volumes, armour, hardpoints,
/// heat, plus the feel multipliers from the prototype's sliders. Each mech owns its own instance (the Godot side
/// writes the feel multipliers for live tuning). See docs/locomotion.md for what the gait constants mean.
/// </summary>
public sealed class MechChassis
{
    public string Name = "Medium";
    public ChassisClass Class = ChassisClass.Medium;

    /// <summary>
    /// Rig size relative to the prototype mech. All lengths below are already scaled; the gait's built-in lengths
    /// (lift, sway, foot size) and the rig's meshes are multiplied by it.
    /// </summary>
    public float Scale = 1f;

    // Body kinematics
    public float VMax = 4.2f;
    public float VRev = 1.8f;
    public float Accel = 1.5f;
    public float Decel = 2.8f;
    public float OmegaMax = 0.6f;
    public float OmegaAccel = 1.2f;
    public float TurnSpeedPenalty = 0.35f;
    public float TwistMax = 1.75f;
    public float TwistRate = 1.3f;
    public float AimMin = -0.35f;
    public float AimMax = 0.45f;
    public float AimRate = 0.8f;

    // Gait
    public float Duty = 0.62f;
    public float DMax = 6.2f;
    public float FMin = 0.36f;
    public float AMax = 1.95f;

    // Rig dimensions
    public float L1 = 3.2f;
    public float L2 = 3.3f;
    public float AnkleHeight = 0.62f;
    public float HipWidth = 1.6f;
    public float HipHeight = 5.75f;
    public float HipRootY = -0.15f;

    // Upper-body attachment points (MechRig builds the same hierarchy from these)
    public float TorsoBaseY = 0.55f;
    public float TorsoY = 0.6f;
    public Vector3 ShoulderOffset = new(2.4f, 1.9f, 0f);     // left arm mirrors X
    public Vector3 ElbowOffset = new(0.45f, -2.1f, 0f);      // left arm mirrors X
    public float AimOriginY = 1.2f;                            // torso-local height of the aim ray origin
    public Vector3 HeadOffset = new(0f, 2.75f, -1.0f);         // torso-local

    // Hardpoints: muzzle positions in the elbow frame. The rig carries a cannon on the right arm, a laser on the left.
    public Vector3 RightArmMuzzle = new(0f, 0.05f, -5.4f);
    public Vector3 LeftArmMuzzle = new(0f, 0.1f, -3.5f);

    // Hit volumes (spheres). Torso/arms/legs from the prototype; the cockpit is a tiny sphere on the canopy glass.
    public float TorsoHitY = 1.2f;
    public float TorsoHitRadius = 2.4f;
    public Vector3 ArmHitOffset = new(0f, 0f, -1.7f);           // elbow-local (forearm)
    public float ArmHitRadius = 1.4f;
    public float LegsHitDrop = 2.4f;                           // below the pelvis
    public float LegsHitRadius = 2.0f;
    public Vector3 CockpitHitOffset = new(0f, 0.05f, -0.85f);  // head-local, just in front of the torso sphere
    public float CockpitHitRadius = 0.3f;

    // Armour (section hit points)
    public float TorsoHp = 100f;
    public float ArmHp = 45f;
    public float LegsHp = 70f;
    public float CockpitHp = 5f;

    // Damage effects
    public float CrippledSpeedFactor = 0.45f;
    public float CollapseDrop = 2.5f;
    public float CollapseTime = 1.4f;

    // Heat (unitless, weapons lock at >= 1)
    public float HeatStart = 0.1f;
    public float HeatFloor = 0.08f;
    public float HeatDissipation = 0.055f;
    public float HeatDissipationMovePenalty = 0.015f;

    // Feel multipliers (prototype sliders)
    public float Sway = 1f;
    public float Bob = 1f;
    public float Lift = 1f;
    public float Impact = 1f;
    public float Jank = 1f;

    public static MechChassis Create(ChassisClass c) => c switch
    {
        ChassisClass.Light => Light(),
        ChassisClass.Medium => Medium(),
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    /// <summary>The prototype mech.</summary>
    public static MechChassis Medium() => new();

    /// <summary>
    /// Small, leggy and fast: ~32 km/h, quick turns and twist, a jogging gait (duty 0.5), thin armour.
    /// Not in the prototype; numbers are first guesses for feel tuning.
    /// </summary>
    public static MechChassis Light()
    {
        var c = new MechChassis().Scaled(0.72f);
        c.Name = "Light";
        c.Class = ChassisClass.Light;

        // Legs 10 % longer relative to the body; hips at the same ~75 % leg extension as the medium.
        c.L1 *= 1.1f;
        c.L2 *= 1.1f;
        c.HipHeight = c.AnkleHeight + 0.75f * (c.L1 + c.L2) - c.HipRootY;
        c.AMax *= 1.1f;

        c.VMax = 9f;
        c.VRev = 3f;
        c.Accel = 4f;
        c.Decel = 6f;
        c.OmegaMax = 1.3f;
        c.OmegaAccel = 3.5f;
        c.TurnSpeedPenalty = 0.4f;
        c.TwistMax = 1.5f;
        c.TwistRate = 2.6f;
        c.AimRate = 1.4f;

        c.Duty = 0.5f;
        c.DMax = 5f;
        c.FMin = 0.6f;

        c.TorsoHp = 60f;
        c.ArmHp = 30f;
        c.LegsHp = 45f;
        c.CockpitHp = 4f;
        c.HeatDissipation = 0.06f;
        c.CollapseTime = 1.1f;
        return c;
    }

    /// <summary>Uniformly scales every length (rig, hit volumes, hardpoints, stride) by <paramref name="s"/>.</summary>
    private MechChassis Scaled(float s)
    {
        Scale *= s;
        DMax *= s;
        AMax *= s;
        L1 *= s;
        L2 *= s;
        AnkleHeight *= s;
        HipWidth *= s;
        HipHeight *= s;
        HipRootY *= s;
        TorsoBaseY *= s;
        TorsoY *= s;
        ShoulderOffset *= s;
        ElbowOffset *= s;
        AimOriginY *= s;
        HeadOffset *= s;
        RightArmMuzzle *= s;
        LeftArmMuzzle *= s;
        TorsoHitY *= s;
        TorsoHitRadius *= s;
        ArmHitOffset *= s;
        ArmHitRadius *= s;
        LegsHitDrop *= s;
        LegsHitRadius *= s;
        CockpitHitOffset *= s;
        CockpitHitRadius *= s;
        CollapseDrop *= s;
        return this;
    }
}
