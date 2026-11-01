using System;
using System.Numerics;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Mech;

public enum ChassisClass { Light, Medium, Heavy, Assault }

/// <summary>Body plan and look. Independent of the weight class, which sets size, speed and armour.</summary>
public enum MechModel
{
    /// <summary>The prototype's upright biped: boxy torso, hanging arms, forward knees.</summary>
    Hauberk,

    /// <summary>Chicken walker: a long, low hull with a nose cockpit, weapon pods instead of arms, digitigrade legs.</summary>
    Sicklefoot,
}

public enum LegLayout
{
    /// <summary>Thigh, shin, flat foot; the knee points forward.</summary>
    Plantigrade,

    /// <summary>
    /// Thigh, shin, and a long raised foot bone (L3) held parallel to the thigh, walking on the toes: the knee points
    /// forward and the hock (ankle) backward, the "Z" leg of birds and chicken walkers.
    /// </summary>
    Digitigrade,
}

/// <summary>
/// Everything that differs between mechs: kinematics, gait, rig dimensions, hit volumes, armour, hardpoints,
/// heat, plus the feel multipliers from the prototype's sliders. Each mech owns its own instance (the Godot side
/// writes the feel multipliers for live tuning). See docs/locomotion.md for what the gait constants mean.
/// Field defaults are the web prototype's walker (4.2 m/s); <see cref="Create"/> builds the game's mechs from a
/// model (body plan) and a weight class.
/// </summary>
public sealed class MechChassis
{
    public string Name = "Prototype";
    public ChassisClass Class = ChassisClass.Medium;
    public MechModel Model = MechModel.Hauberk;

    /// <summary>
    /// Rig size relative to the model's medium. All lengths below are already scaled; the gait's built-in lengths
    /// (lift, sway) and the rig's meshes are multiplied by it.
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

    // Legs. L3 is the raised foot bone of a digitigrade leg (0 for plantigrade). AnkleHeight is the height of the
    // IK end point above the ground when the foot is flat: the ankle, or the toe joint of a digitigrade leg.
    public LegLayout Legs = LegLayout.Plantigrade;
    public float L1 = 3.2f;
    public float L2 = 3.3f;
    public float L3;
    public float AnkleHeight = 0.62f;
    public float HipWidth = 1.6f;
    public float HipHeight = 5.75f;
    public float HipRootY = -0.15f;

    /// <summary>Foot pivots for heel-strike / toe-off: toe ahead of and heel behind the IK end point.</summary>
    public float ToeLength = 1.7f, HeelLength = 1.05f;

    /// <summary>Leg extension (hip-to-foot distance / leg length) with the foot under the hip; sets the hip height.</summary>
    public float StanceExtension = 0.766f;

    // Upper-body attachment points (MechRig builds the same hierarchy from these)
    public float TorsoBaseY = 0.55f;
    public float TorsoY = 0.6f;
    public Vector3 ShoulderOffset = new(2.4f, 1.9f, 0f);     // left arm mirrors X
    public Vector3 ElbowOffset = new(0.45f, -2.1f, 0f);      // left arm mirrors X
    public float AimOriginY = 1.2f;                            // torso-local height of the aim ray origin
    public Vector3 HeadOffset = new(0f, 2.75f, -1.0f);         // torso-local

    /// <summary>Gait arm swing multiplier (weapon pods on a hull barely swing).</summary>
    public float ArmSwing = 1f;

    /// <summary>How much rough ground ahead slows the mech (1 = fully; lights are nimble and keep their speed).</summary>
    public float RoughGroundCaution = 1f;

    // Loadout: one weapon per arm, bolted on at the mount point (elbow frame); the muzzle is the weapon's length ahead.
    public WeaponSpec RightArmWeapon = WeaponSpec.Cannon;
    public WeaponSpec LeftArmWeapon = WeaponSpec.Laser;
    public Vector3 RightArmMount = new(0f, 0.05f, -2f);
    public Vector3 LeftArmMount = new(0f, 0.1f, -2f);

    /// <summary>A rack on the right shoulder (fire bit 2), in the torso frame; null = none. Lost only with the mech.</summary>
    public WeaponSpec? TorsoWeapon;
    public Vector3 TorsoMount = new(1.1f, 1.7f, 0.3f);

    /// <summary>
    /// Point-defense turrets fitted (each shoots down ~1.5 enemy missiles a second within ~110 m, also over nearby
    /// teammates). One thins a volley; stopping one takes several, on this mech or across the lance.
    /// </summary>
    public int PointDefense;

    // Hit volumes (spheres). Torso/arms/legs from the prototype; the cockpit is a tiny sphere on the canopy glass.
    public Vector3 TorsoHitOffset = new(0f, 1.2f, 0f);         // torso-local
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

    /// <summary>
    /// Gyro-stabilised weapon mounts: the share of the body's motion (gait sway, torso springs, footfall and recoil
    /// kicks) the aim ray cancels. The torso still visibly moves; the guns stay on the pilot's line.
    /// </summary>
    public float AimStabilization = 0.85f;

    // Jump jets (JumpThrust 0 = none fitted)
    /// <summary>Upward acceleration while the jets burn, m/s²; must beat <see cref="MechLocomotion.Gravity"/>. 0 = no jets.</summary>
    public float JumpThrust;

    /// <summary>Seconds of burn on a full charge; it refills over <see cref="JumpRecharge"/> seconds on the ground.</summary>
    public float JumpFuel = 1.3f;
    public float JumpRecharge = 6f;

    /// <summary>Heat per second of burn.</summary>
    public float JumpHeat = 0.12f;

    /// <summary>Forward push while burning, m/s² along the legs' heading (the nozzles angle back): a jump carries you.</summary>
    public float JumpPush = 4f;

    public bool HasJumpJets => JumpThrust > 0f;


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

    public float LegLength => L1 + L2 + L3;

    /// <summary>0 = walking, 1 = fully running.</summary>
    public float RunBlend(float speed) => Core.MathUtil.Smoothstep(RunStartSpeed, RunFullSpeed, speed);

    /// <summary>Stance fraction at a given speed.</summary>
    public float DutyAt(float speed) => Core.MathUtil.Lerp(Duty, DutyRun, RunBlend(speed));

    /// <summary>The web prototype's walker, unchanged (4.2 m/s). Tests pin conventions against it.</summary>
    public static MechChassis Prototype() => new();

    public static MechChassis Light(MechModel model = MechModel.Hauberk) => Create(ChassisClass.Light, model);
    public static MechChassis Medium(MechModel model = MechModel.Hauberk) => Create(ChassisClass.Medium, model);
    public static MechChassis Heavy(MechModel model = MechModel.Hauberk) => Create(ChassisClass.Heavy, model);
    public static MechChassis Assault(MechModel model = MechModel.Hauberk) => Create(ChassisClass.Assault, model);

    /// <summary>
    /// A game mech: the model's body at medium size, then the class's scale, speed and armour.
    /// Speed bands: light 100-150 km/h, medium 60-100, heavy 50-70, assault 30-50.
    /// Everything except the prototype's geometry is a first guess for feel tuning.
    /// </summary>
    public static MechChassis Create(ChassisClass cls, MechModel model = MechModel.Hauberk)
    {
        var c = model switch
        {
            MechModel.Hauberk => new MechChassis(),
            MechModel.Sicklefoot => SicklefootBody(),
            _ => throw new ArgumentOutOfRangeException(nameof(model)),
        };
        c.Model = model;
        c.Class = cls;
        c.Name = $"{model} {cls}";
        switch (cls)
        {
            case ChassisClass.Light:
                // Small, leggy and very fast: quick turns and twist, thin armour. Legs 10 % longer relative to the body.
                c.Scaled(0.72f);
                c.L1 *= 1.1f;
                c.L2 *= 1.1f;
                c.L3 *= 1.1f;
                c.FMin = 0.6f;
                c.SetSpeed(36f, rev: 8f, accel: 10f, decel: 16f, omega: 1.8f, omegaAccel: 5f, twistRate: 3f, aimRate: 1.6f);
                // Agile at speed (1.26 rad/s flat out, a 29 m circle): enough to out-turn a heavy's or an assault's
                // legs up close and get at their rear armour.
                c.TurnSpeedPenalty = 0.3f;
                // Jets: ~23 m on a full burn (enough to vault an assault), a hop over a crest on a tap.
                c.JumpThrust = 24f;
                c.JumpFuel = 1.4f;
                c.JumpRecharge = 5f;
                c.JumpHeat = 0.1f;
                c.TwistMax = MathF.Min(c.TwistMax, 1.5f);
                c.SetArmour(60f, 30f, 45f, 4f);
                // Sicklefoot: machine guns and a flamethrower (heat it, then shred what the plates don't cover);
                // Hauberk: incendiary cannon and pulse laser.
                if (model == MechModel.Sicklefoot) c.SetWeapons(WeaponSpec.MachineGun, WeaponSpec.Flamethrower);
                else c.SetWeapons(WeaponSpec.IncendiaryCannon, WeaponSpec.PulseLaser);
                c.RoughGroundCaution = 0.5f;
                c.HeatDissipation = 0.06f;
                c.CollapseTime = 1.1f;
                break;
            case ChassisClass.Medium:
                // Quick to reposition, and one hard-hitting gun.
                c.SetSpeed(22f, rev: 6f, accel: 6f, decel: 10f, omega: 1.1f, omegaAccel: 3f, twistRate: 2f, aimRate: 1.2f);
                if (model == MechModel.Sicklefoot) c.SetWeapons(WeaponSpec.ParticleCannon, WeaponSpec.MachineGun);
                else c.SetWeapons(WeaponSpec.HeavyCannon, WeaponSpec.Laser);
                c.RoughGroundCaution = 0.8f;
                c.TurnSpeedPenalty = 0.5f;
                // Jets: ~13 m on a full burn, enough to pop up over a crest, fire and drop back behind it.
                c.JumpThrust = 21f;
                c.JumpFuel = 1.3f;
                c.JumpRecharge = 6f;
                c.JumpHeat = 0.12f;
                break;
            case ChassisClass.Heavy:
                // Bigger and tougher, a ponderous run.
                c.Scaled(1.15f);
                c.SetSpeed(16.5f, rev: 4.5f, accel: 4f, decel: 7f, omega: 0.8f, omegaAccel: 2f, twistRate: 1.5f, aimRate: 1f);
                c.TurnSpeedPenalty = 0.45f;
                c.SetArmour(140f, 60f, 95f, 6f);
                c.SetWeapons(model == MechModel.Sicklefoot ? WeaponSpec.ParticleCannon : WeaponSpec.HeavyCannon, WeaponSpec.HeavyLaser);
                c.HeatDissipation = 0.065f; // big guns need big heat sinks
                c.CollapseTime = 1.7f;
                break;
            case ChassisClass.Assault:
                // The wall: never quite breaks into a run, heaviest armour.
                c.Scaled(1.3f);
                c.SetSpeed(11f, rev: 3.5f, accel: 2.8f, decel: 5f, omega: 0.6f, omegaAccel: 1.5f, twistRate: 1.2f, aimRate: 0.9f);
                c.TurnSpeedPenalty = 0.4f;
                c.SetArmour(180f, 75f, 120f, 7f);
                c.SetWeapons(WeaponSpec.SiegeCannon, model == MechModel.Sicklefoot ? WeaponSpec.ParticleCannon : WeaponSpec.HeavyLaser);
                c.HeatDissipation = 0.075f;
                c.CollapseTime = 2f;
                break;
        }
        // Shoulder racks and point defense: lights a rocket pod for in-close bursts, mediums a strike rack, heavies
        // the guided arc missiles (their support role), assaults one point-defense turret. A Hauberk medium with a
        // second turret made every default lance a two-turret screen that shot down ~3/4 of arc volleys.
        c.TorsoWeapon = cls switch
        {
            ChassisClass.Light => WeaponSpec.RocketPod,
            ChassisClass.Medium => WeaponSpec.StrikeRack,
            ChassisClass.Heavy => WeaponSpec.ArcMissiles,
            _ => null,
        };
        c.PointDefense = cls == ChassisClass.Assault ? 1 : 0;
        c.FitGaitToLegs();
        c.ApplyModelArmour(model);
        return c;
    }

    /// <summary>
    /// Sicklefoot at medium size: a long, low hull carried forward of the hips, cockpit in the nose, weapon pods on
    /// short side pylons, digitigrade legs. About a metre lower than the Hauberk but longer.
    /// </summary>
    private static MechChassis SicklefootBody() => new()
    {
        Legs = LegLayout.Digitigrade,
        L1 = 2.3f,
        L2 = 3.0f,
        L3 = 1.9f,
        AnkleHeight = 0.35f,
        HipWidth = 1.45f,
        HipRootY = -0.1f,
        StanceExtension = 0.72f,
        ToeLength = 1.0f,
        HeelLength = 0.35f,

        TorsoBaseY = 0.45f,
        TorsoY = 0.55f,
        ShoulderOffset = new Vector3(2.05f, 0.75f, -0.7f),
        ElbowOffset = new Vector3(0.5f, -0.15f, 0f),
        AimOriginY = 0.9f,
        HeadOffset = new Vector3(0f, 0.55f, -2.75f),
        ArmSwing = 0.25f,
        TwistMax = 1.9f,

        RightArmMount = new Vector3(0f, 0f, -2.4f),
        LeftArmMount = new Vector3(0f, 0.05f, -1.9f),

        TorsoHitOffset = new Vector3(0f, 0.9f, -0.4f),
        TorsoHitRadius = 2.2f,
        ArmHitOffset = new Vector3(0f, 0f, -1.3f),
        ArmHitRadius = 1.15f,
        LegsHitDrop = 2.6f,
        CockpitHitOffset = new Vector3(0f, 0.1f, -0.8f),
        CollapseDrop = 2.8f,
    };

    /// <summary>
    /// Hip height from the stance extension, stride limit from the leg's horizontal reach (70 % of it at 95 %
    /// extension), walk-to-run speeds from the Froude number.
    /// </summary>
    private void FitGaitToLegs()
    {
        float l = LegLength, h = StanceExtension * l;
        HipHeight = AnkleHeight + h - HipRootY;
        AMax = 0.7f * MathF.Sqrt((0.95f * l) * (0.95f * l) - h * h);
        SetRunSpeedsFromLegs();
    }

    /// <summary>
    /// Armour multiplier per body plan, on torso, arms and legs (the cockpit stays fragile). Balanced against headless
    /// duels: the Hauberk is the bigger, taller target (more sphere area, a torso that shows over crests), so it gets
    /// the heavier plate. 1.0 = the class's base values.
    /// </summary>
    public static float ArmourFactor(MechModel model) => model switch
    {
        MechModel.Hauberk => HauberkArmour,
        _ => 1f,
    };

    private const float HauberkArmour = 1.3f;

    private void ApplyModelArmour(MechModel model)
    {
        float k = ArmourFactor(model);
        TorsoHp *= k;
        ArmHp *= k;
        LegsHp *= k;
    }

    private void SetWeapons(WeaponSpec right, WeaponSpec left)
    {
        RightArmWeapon = right;
        LeftArmWeapon = left;
    }

    private void SetArmour(float torso, float arm, float legs, float cockpit)
    {
        TorsoHp = torso;
        ArmHp = arm;
        LegsHp = legs;
        CockpitHp = cockpit;
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
        L3 *= s;
        AnkleHeight *= s;
        ToeLength *= s;
        HeelLength *= s;
        HipWidth *= s;
        HipHeight *= s;
        HipRootY *= s;
        TorsoBaseY *= s;
        TorsoY *= s;
        ShoulderOffset *= s;
        ElbowOffset *= s;
        AimOriginY *= s;
        HeadOffset *= s;
        RightArmMount *= s;
        LeftArmMount *= s;
        TorsoMount *= s;
        TorsoHitOffset *= s;
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
