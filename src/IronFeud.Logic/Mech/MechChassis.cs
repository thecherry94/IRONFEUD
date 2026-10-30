using System;
using System.Numerics;

namespace IronFeud.Logic.Mech;

public enum ChassisClass { Light, Medium, Heavy, Assault }

/// <summary>
/// Everything that differs between mech classes: kinematics, gait, rig dimensions, hit volumes, armour, hardpoints,
/// heat, plus the feel multipliers from the prototype's sliders. Each mech owns its own instance (the Godot side
/// writes the feel multipliers for live tuning). See docs/locomotion.md for what the gait constants mean.
/// Field defaults are the web prototype's walker (4.2 m/s); the class presets below are the game's mechs.
/// </summary>
public sealed class MechChassis
{
    public string Name = "Prototype";
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

    // Gait. Duty blends from walking (Duty) to running (DutyRun) between RunStartSpeed and RunFullSpeed.
    public float Duty = 0.62f;
    public float DutyRun = 0.32f;
    public float RunStartSpeed = 5.2f;
    public float RunFullSpeed = 11.7f;
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

    /// <summary>Stance fraction at a given speed.</summary>
    public float DutyAt(float speed) => Core.MathUtil.Lerp(Duty, DutyRun, Core.MathUtil.Smoothstep(RunStartSpeed, RunFullSpeed, speed));

    public static MechChassis Create(ChassisClass c) => c switch
    {
        ChassisClass.Light => Light(),
        ChassisClass.Medium => Medium(),
        ChassisClass.Heavy => Heavy(),
        ChassisClass.Assault => Assault(),
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    /// <summary>The web prototype's walker, unchanged (4.2 m/s). Tests pin conventions against it.</summary>
    public static MechChassis Prototype() => new();

    // Class presets. Speed bands: light 100-150 km/h, medium 60-100, heavy 50-70, assault 30-50.
    // Everything except the prototype's geometry is a first guess for feel tuning.

    /// <summary>The prototype's body at 80 km/h.</summary>
    public static MechChassis Medium()
    {
        var c = new MechChassis { Name = "Medium" };
        c.SetSpeed(22f, rev: 6f, accel: 6f, decel: 10f, omega: 1.1f, omegaAccel: 3f, twistRate: 2f, aimRate: 1.2f);
        c.TurnSpeedPenalty = 0.5f;
        c.AMax = 2.6f; // ~70 % of the leg's horizontal reach, for running strides
        return c;
    }

    /// <summary>Small, leggy and very fast: 130 km/h, quick turns and twist, thin armour.</summary>
    public static MechChassis Light()
    {
        var c = new MechChassis().Scaled(0.72f);
        c.Name = "Light";
        c.Class = ChassisClass.Light;

        // Legs 10 % longer relative to the body; hips at the same ~75 % leg extension as the medium.
        c.L1 *= 1.1f;
        c.L2 *= 1.1f;
        c.HipHeight = c.AnkleHeight + 0.75f * (c.L1 + c.L2) - c.HipRootY;
        c.AMax = 2.3f;
        c.FMin = 0.6f;
        c.SetRunSpeedsFromLegs();

        c.SetSpeed(36f, rev: 8f, accel: 10f, decel: 16f, omega: 1.8f, omegaAccel: 5f, twistRate: 3f, aimRate: 1.6f);
        c.TurnSpeedPenalty = 0.55f;
        c.TwistMax = 1.5f;

        c.TorsoHp = 60f;
        c.ArmHp = 30f;
        c.LegsHp = 45f;
        c.CockpitHp = 4f;
        c.HeatDissipation = 0.06f;
        c.CollapseTime = 1.1f;
        return c;
    }

    /// <summary>Bigger and tougher: 60 km/h, a ponderous run.</summary>
    public static MechChassis Heavy()
    {
        var c = new MechChassis().Scaled(1.15f);
        c.Name = "Heavy";
        c.Class = ChassisClass.Heavy;
        c.AMax = 3f;
        c.SetRunSpeedsFromLegs();
        c.SetSpeed(16.5f, rev: 4.5f, accel: 4f, decel: 7f, omega: 0.8f, omegaAccel: 2f, twistRate: 1.5f, aimRate: 1f);
        c.TurnSpeedPenalty = 0.45f;
        c.TorsoHp = 140f;
        c.ArmHp = 60f;
        c.LegsHp = 95f;
        c.CockpitHp = 6f;
        c.HeatDissipation = 0.05f;
        c.CollapseTime = 1.7f;
        return c;
    }

    /// <summary>The wall: 40 km/h, never quite breaks into a run, heaviest armour.</summary>
    public static MechChassis Assault()
    {
        var c = new MechChassis().Scaled(1.3f);
        c.Name = "Assault";
        c.Class = ChassisClass.Assault;
        c.AMax = 3.3f;
        c.SetRunSpeedsFromLegs();
        c.SetSpeed(11f, rev: 3.5f, accel: 2.8f, decel: 5f, omega: 0.6f, omegaAccel: 1.5f, twistRate: 1.2f, aimRate: 0.9f);
        c.TurnSpeedPenalty = 0.4f;
        c.TorsoHp = 180f;
        c.ArmHp = 75f;
        c.LegsHp = 120f;
        c.CockpitHp = 7f;
        c.HeatDissipation = 0.045f;
        c.CollapseTime = 2f;
        return c;
    }

    private void SetSpeed(float vMax, float rev, float accel, float decel, float omega, float omegaAccel, float twistRate, float aimRate)
    {
        VMax = vMax;
        VRev = rev;
        Accel = accel;
        Decel = decel;
        OmegaMax = omega;
        OmegaAccel = omegaAccel;
        TwistRate = twistRate;
        AimRate = aimRate;
    }

    /// <summary>
    /// Walk-to-run transition from leg length via the Froude number v²/(g·h): animals start to run around 0.5 and
    /// run fully by ~2.5. h is the hip joint height.
    /// </summary>
    private void SetRunSpeedsFromLegs()
    {
        float h = HipHeight + HipRootY;
        RunStartSpeed = MathF.Sqrt(0.5f * 9.81f * h);
        RunFullSpeed = MathF.Sqrt(2.5f * 9.81f * h);
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
