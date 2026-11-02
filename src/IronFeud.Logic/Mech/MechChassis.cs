using System;
using System.Numerics;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Mech;

public enum ChassisClass { Light, Medium, Heavy, Assault }

/// <summary>Body plan and look. Independent of the weight class, which sets size, speed and armour.</summary>
public enum MechModel
{
    /// <summary>The prototype's upright biped: boxy torso, hanging arms, forward knees.</summary>
    Bascinet,

    /// <summary>Chicken walker: a long, low hull with a nose cockpit, weapon pods instead of arms, digitigrade legs.</summary>
    Sicklefoot,

    /// <summary>Light jumper: an upright drum torso on tall digitigrade legs, thin arms ending in long barrels.</summary>
    Bodkin,

    /// <summary>Medium hull-down shooter: a high, level shoulder bar with the cockpit tucked just under it.</summary>
    Brigandine,

    /// <summary>Squat heavy breacher: short thick legs, wide stance, cockpit sunk between two shoulder humps.</summary>
    Harrow,

    /// <summary>Top-heavy assault fortress: stubby legs on huge feet, a wide chest, a visor slit under a brow plate.</summary>
    Barbican,
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

/// <summary>A cover plate as a box in a shoulder's frame (see <see cref="MechChassis.ShoulderCover"/>).</summary>
public struct ShoulderPlate
{
    public Vector3 Center;
    public Vector3 Size;
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
    public MechModel Model = MechModel.Bascinet;

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

    /// <summary>
    /// Arm aiming: the arms swing up to this far from the torso's aim (yaw ±20°, pitch ±25°) at
    /// <see cref="ArmRate"/>, 2.5x the torso twist rate (the genre's ratio), so arm weapons get onto a target before the
    /// torso does. Torso weapons fire along the torso.
    /// </summary>
    public float ArmYawMax = 0.35f, ArmPitchMax = 0.45f;

    public float ArmRate => 2.5f * TwistRate;

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

    /// <summary>
    /// The weapons, in fire-bit order (bit i fires <c>Loadout[i]</c>), each on a hardpoint of the body. Several weapons
    /// on one hardpoint stack (see <see cref="MountAnchor"/>); the muzzle sits the weapon's length ahead of its anchor.
    /// </summary>
    public MountDef[] Loadout = { new(WeaponSpec.Cannon, Hardpoint.RightArm), new(WeaponSpec.Laser, Hardpoint.LeftArm) };

    // Hardpoint anchors. Arms: the mount point in the elbow frame. Rack: the shoulder rack in the torso frame (its
    // side torso by x, left = -x). Chest: torso frame.
    public Vector3 RightArmMount = new(0f, 0.05f, -2f);
    public Vector3 LeftArmMount = new(0f, 0.1f, -2f);
    public Vector3 TorsoMount = new(1.1f, 1.7f, 0.3f);
    public Vector3 ChestMountLeft = new(-1.3f, 0.55f, -1.4f);
    public Vector3 ChestMountRight = new(1.3f, 0.55f, -1.4f);

    /// <summary>
    /// Point-defense turrets fitted (each shoots down ~1.5 enemy missiles a second within ~110 m, also over nearby
    /// teammates). One thins a volley; stopping one takes several, on this mech or across the lance.
    /// </summary>
    public int PointDefense;

    /// <summary>Where the first point-defense turret sits (torso frame); further turrets stack back. Rig only.</summary>
    public Vector3 PointDefenseMount = new(-1.1f, 1.75f, 0.4f);

    /// <summary>
    /// How far off it can be seen, as a share of a pilot's sight and radar range: small mechs are seen last (and, with
    /// the same sensors, see bigger ones first). Firing or a jet burn shows it at full size for a moment.
    /// </summary>
    public float Signature = 1f;

    /// <summary>
    /// A target designator: a completed lock also marks the target for the lance (see <see cref="Battle.BattleSim.Marked"/>).
    /// Lights carry one.
    /// </summary>
    public bool Designator;

    /// <summary>
    /// Carries an ECM jammer: enemies inside its bubble can't lock or scan, and its lance inside it can't be locked
    /// unless marked (see <see cref="Battle.BattleSim.Jammed"/>). The Sicklefoot light carries one.
    /// </summary>
    public bool Ecm;

    // Hit volumes (spheres). Torso/arms/legs from the prototype; the cockpit is a tiny sphere on the canopy glass.
    // The torso sphere is the envelope the centre and side torso spheres are derived from (MechPoseSolver); the legs'
    // radius covered both legs and now sizes one sphere per leg.
    public Vector3 TorsoHitOffset = new(0f, 1.2f, 0f);         // torso-local
    public float TorsoHitRadius = 2.4f;
    public Vector3 ArmHitOffset = new(0f, 0f, -1.7f);           // elbow-local (forearm)
    public float ArmHitRadius = 1.4f;
    public float LegsHitRadius = 2.0f;
    public Vector3 CockpitHitOffset = new(0f, 0.05f, -0.7f);  // head-local, just in front of the torso sphere
    public float CockpitHitRadius = 0.09f; // was 0.12, which only passed the cockpit test through a ray-sphere precision error

    /// <summary>
    /// Cover plates on the left shoulder (shoulder-local, the right one mirrored), part of that arm: hits on them strike
    /// the arm, not the core behind. The Bascinet's pauldrons, with a raised flange on the inner edge that screens the
    /// head and upper torso from the side; the rig draws exactly these boxes. A mech that twists a shoulder toward the
    /// fire takes it on the arm (and risks that arm's weapon) instead of the torso.
    /// </summary>
    public ShoulderPlate[] ShoulderCover =
    {
        new() { Center = new Vector3(-0.35f, 0.3f, 0f), Size = new Vector3(1.25f, 1.0f, 1.8f) },  // pauldron
        new() { Center = new Vector3(0.17f, 1.0f, -0.75f), Size = new Vector3(0.24f, 1.0f, 2.3f) },  // flange, swept forward to the canopy
    };

    /// <summary>
    /// Stability budget in impact points (a shell drains its damage, see <see cref="WeaponSpec.Impact"/>): grows with
    /// mass and stance. Drained, the aim wavers; emptied, the mech staggers (<see cref="MechSim.Stability"/>).
    /// </summary>
    public float StabilityMax = 70f;

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

    public static MechChassis Light(MechModel model = MechModel.Bascinet) => Create(ChassisClass.Light, model);
    public static MechChassis Medium(MechModel model = MechModel.Bascinet) => Create(ChassisClass.Medium, model);
    public static MechChassis Heavy(MechModel model = MechModel.Bascinet) => Create(ChassisClass.Heavy, model);
    public static MechChassis Assault(MechModel model = MechModel.Bascinet) => Create(ChassisClass.Assault, model);

    /// <summary>
    /// A game mech, in three layers kept apart so a new chassis is mostly data: the model's <see cref="Body"/> (body plan
    /// at reference size: legs, frames, mounts, hit volumes, plates), the class's <see cref="ApplyClass"/> (size, speed,
    /// agility, armour, jets, heat) and the <see cref="ApplyLoadout"/> (weapons, rack, point defense, sensors).
    /// Speed bands: light 100-150 km/h, medium 60-100, heavy 50-70, assault 30-50.
    /// </summary>
    public static MechChassis Create(ChassisClass cls, MechModel model = MechModel.Bascinet)
    {
        var c = Body(model);
        c.Model = model;
        c.Class = cls;
        c.Name = $"{model} {cls}";
        ApplyClass(c, cls);
        ApplyLoadout(c, cls, model);
        ApplyDesign(c, model);
        c.FitGaitToLegs();
        c.ApplyModelArmour(model);
        return c;
    }

    /// <summary>The weight class a design is built for (rosters use these; <see cref="Create"/> builds any pairing).</summary>
    public static ChassisClass NativeClass(MechModel model) => model switch
    {
        MechModel.Sicklefoot or MechModel.Bodkin => ChassisClass.Light,
        MechModel.Bascinet or MechModel.Brigandine => ChassisClass.Medium,
        MechModel.Harrow => ChassisClass.Heavy,
        MechModel.Barbican => ChassisClass.Assault,
        _ => throw new ArgumentOutOfRangeException(nameof(model)),
    };

    /// <summary>A design in its own weight class.</summary>
    public static MechChassis Design(MechModel model) => Create(NativeClass(model), model);

    /// <summary>The body plan at its reference (medium) size.</summary>
    public static MechChassis Body(MechModel model) => model switch
    {
        MechModel.Bascinet => new MechChassis(),
        MechModel.Sicklefoot => SicklefootBody(),
        MechModel.Bodkin => BodkinBody(),
        MechModel.Brigandine => BrigandineBody(),
        MechModel.Harrow => HarrowBody(),
        MechModel.Barbican => BarbicanBody(),
        _ => throw new ArgumentOutOfRangeException(nameof(model)),
    };

    /// <summary>
    /// Per-design quirks on top of the class: agility within the class band, armour split, loadout. Quirks waiting for
    /// systems that don't exist yet (stability, bracing, shoving, melee, logistics) are noted on each design.
    /// </summary>
    private static void ApplyDesign(MechChassis c, MechModel model)
    {
        switch (model)
        {
            case MechModel.Bodkin:
                // The roster's best jumper and widest twist; fragile legs, a small reactor that runs hot.
                // Later: mid-air heading change, landing strikes (melee).
                c.TwistMax = 2.0f;
                c.JumpFuel = 1.7f;
                c.JumpThrust = 26f;
                c.LegsHp *= 0.85f;
                c.HeatDissipation -= 0.006f;
                c.StabilityMax *= 0.9f; // tall and light on its feet
                break;
            case MechModel.Brigandine:
                // Arms ride high under the shoulder bar: when its eyes clear a crest, so do its guns, with little torso
                // showing. Particle cannon and autocannon (the hull-down shooter's long, fast guns).
                c.SetWeapons(WeaponSpec.ParticleCannon, WeaponSpec.Autocannon);
                // Its particle cannon runs hot: better sinks keep it out of the running-hot band (without them a
                // brawling Brigandine lost the shipped 1v1 28-12 once running hot cost speed and aim).
                c.HeatDissipation += 0.0025f;
                break;
            case MechModel.Harrow:
                // Slow, narrow twist (lights inside its arc are safe), quick off the mark, a heavy front, hard to stagger.
                // Later: cover breaking, shoves, thin rear plate.
                c.TwistMax = 1.25f;
                c.TwistRate = 1.3f;
                c.Accel *= 1.3f;
                c.TorsoHp *= 1.1f;
                c.StabilityMax *= 1.25f; // low and wide: hard to rock
                break;
            case MechModel.Barbican:
                // The slowest, narrowest twist in the roster, the most armour, the steadiest; a cowled visor. Later:
                // bracing, logistics cost, short sensors.
                c.TwistMax = 1.05f;
                c.TwistRate = 1.1f;
                c.TorsoHp *= 1.1f;
                c.ArmHp *= 1.1f;
                c.StabilityMax *= 1.25f; // huge feet, low centre of mass
                break;
        }
    }

    /// <summary>Weight-class stats: size, speed and agility, armour, jets, heat sinks.</summary>
    private static void ApplyClass(MechChassis c, ChassisClass cls)
    {
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
                c.SetArmour(60f, 30f, 45f, 4f);
                c.RoughGroundCaution = 0.5f;
                c.HeatDissipation = 0.06f;
                c.CollapseTime = 1.1f;
                break;
            case ChassisClass.Medium:
                // Quick to reposition, and one hard-hitting gun.
                c.SetSpeed(22f, rev: 6f, accel: 6f, decel: 10f, omega: 1.1f, omegaAccel: 3f, twistRate: 2f, aimRate: 1.2f);
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
                c.HeatDissipation = 0.065f; // big guns need big heat sinks
                c.CollapseTime = 1.7f;
                break;
            case ChassisClass.Assault:
                // The wall: never quite breaks into a run, heaviest armour.
                c.Scaled(1.3f);
                c.SetSpeed(11f, rev: 3.5f, accel: 2.8f, decel: 5f, omega: 0.6f, omegaAccel: 1.5f, twistRate: 1.2f, aimRate: 0.9f);
                c.TurnSpeedPenalty = 0.4f;
                c.SetArmour(180f, 75f, 120f, 7f);
                c.HeatDissipation = 0.075f;
                c.CollapseTime = 2f;
                break;
        }
        // 4-6 weapons a mech (the loadouts below) cut the mean lance battle from 91 to 74 s: armour up 20 % to match.
        c.TorsoHp *= LoadoutArmour;
        c.ArmHp *= LoadoutArmour;
        c.LegsHp *= LoadoutArmour;
        c.Signature = cls switch { ChassisClass.Light => 0.6f, ChassisClass.Medium => 0.8f, ChassisClass.Assault => 1.15f, _ => 1f };
        c.StabilityMax = cls switch { ChassisClass.Light => 40f, ChassisClass.Medium => 70f, ChassisClass.Heavy => 100f, _ => 135f };
        // Torso twist range narrows with weight (the genre's numbers: assaults +-62-90 deg, lights far wider), so a light
        // inside an assault's arc is safe and a big mech must turn its legs to follow one. The rate already falls with
        // tonnage^-0.75 (light 3, medium 2, heavy 1.5, assault 1.2 rad/s).
        c.TwistMax = cls switch { ChassisClass.Light => 1.9f, ChassisClass.Medium => 1.75f, ChassisClass.Heavy => 1.45f, _ => 1.15f };
    }

    /// <summary>Weapons, shoulder rack, point defense and sensor kit for a model in a class.</summary>
    private static void ApplyLoadout(MechChassis c, ChassisClass cls, MechModel model)
    {
        bool sf = model == MechModel.Sicklefoot;
        switch (cls)
        {
            // Sicklefoot light: machine guns and a flamethrower (heat it, then shred what the plates don't cover);
            // Bascinet light: incendiary cannon and pulse laser.
            case ChassisClass.Light:
                if (sf) c.SetWeapons(WeaponSpec.MachineGun, WeaponSpec.Flamethrower);
                else c.SetWeapons(WeaponSpec.IncendiaryCannon, WeaponSpec.PulseLaser);
                break;
            case ChassisClass.Medium:
                if (sf) c.SetWeapons(WeaponSpec.ParticleCannon, WeaponSpec.Autocannon);
                else c.SetWeapons(WeaponSpec.HeavyCannon, WeaponSpec.Laser);
                break;
            case ChassisClass.Heavy:
                c.SetWeapons(sf ? WeaponSpec.ParticleCannon : WeaponSpec.HeavyCannon, WeaponSpec.HeavyLaser);
                break;
            case ChassisClass.Assault:
                c.SetWeapons(WeaponSpec.SiegeCannon, sf ? WeaponSpec.ParticleCannon : WeaponSpec.HeavyLaser);
                break;
        }
        // Shoulder racks and point defense: lights a rocket pod for in-close bursts, mediums a strike rack, heavies
        // the guided arc missiles (their support role), assaults one point-defense turret (and a strike rack, below). A Bascinet medium with a
        // second turret made every default lance a two-turret screen that shot down ~3/4 of arc volleys.
        WeaponSpec? rack = cls switch
        {
            ChassisClass.Light => WeaponSpec.RocketPod,
            ChassisClass.Medium => WeaponSpec.StrikeRack,
            ChassisClass.Heavy => WeaponSpec.ArcMissiles,
            _ => null,
        };
        if (rack != null) c.Loadout = [.. c.Loadout, new MountDef(rack, Hardpoint.Rack)];
        // Chest guns and second arm weapons: lights, mediums and heavies 5 weapons, assaults 6, every mech mixing
        // weapon families (energy pays in heat, guns and racks in rounds).
        MountDef[] extra = cls switch
        {
            // Lights: a pulse laser and a small laser (one small laser left them out-gunned: kills 0.69 -> 0.45 a game
            // when everyone else armed up; two small lasers, once armour rose 20 %, 0.36).
            ChassisClass.Light => [new(WeaponSpec.PulseLaser, Hardpoint.ChestLeft), new(WeaponSpec.SmallLaser, Hardpoint.ChestRight)],
            ChassisClass.Medium => [new(WeaponSpec.SmallLaser, Hardpoint.ChestLeft), new(WeaponSpec.MachineGun, Hardpoint.ChestRight)],
            ChassisClass.Heavy => [new(WeaponSpec.Laser, Hardpoint.ChestLeft), new(WeaponSpec.MachineGun, Hardpoint.ChestRight)],
            _ => [new(WeaponSpec.Laser, Hardpoint.ChestLeft), new(WeaponSpec.Laser, Hardpoint.ChestRight),
                  new(WeaponSpec.StrikeRack, Hardpoint.Rack), new(WeaponSpec.SmallLaser, Hardpoint.LeftArm)],
        };
        c.Loadout = [.. c.Loadout, .. extra];
        c.PointDefense = cls == ChassisClass.Assault ? 1 : 0;
        c.Designator = cls == ChassisClass.Light;
        c.Ecm = cls == ChassisClass.Light && sf;
    }

    /// <summary>
    /// Sicklefoot at medium size: a long, low hull carried forward of the hips, cockpit in the nose, weapon pods on
    /// short side pylons, digitigrade legs. About a metre lower than the Bascinet but longer.
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

        ChestMountLeft = new Vector3(-0.9f, 0.45f, -2.55f),
        ChestMountRight = new Vector3(0.9f, 0.45f, -2.55f),
        TorsoHitOffset = new Vector3(0f, 0.9f, -0.4f),
        TorsoHitRadius = 2.2f,
        ArmHitOffset = new Vector3(0f, 0f, -1.3f),
        ArmHitRadius = 1.15f,
        CockpitHitOffset = new Vector3(0f, 0.1f, -0.8f),
        CockpitHitRadius = 0.08f, // its head juts out of the torso sphere: no shield round the canopy, so a smaller window
        ShoulderCover = System.Array.Empty<ShoulderPlate>(), // weapon pods on bare pylons: nothing screens the hull
        CollapseDrop = 2.8f,
    };

    /// <summary>
    /// Bodkin at reference size: an upright drum torso on tall digitigrade legs, the cockpit on its front face, thin arms
    /// hanging from high shoulders with long-barrelled forearms reaching forward. Taller and narrower than a Sicklefoot.
    /// </summary>
    private static MechChassis BodkinBody() => new()
    {
        Legs = LegLayout.Digitigrade,
        L1 = 2.5f,
        L2 = 3.3f,
        L3 = 2.1f,
        AnkleHeight = 0.35f,
        HipWidth = 1.25f,
        HipRootY = -0.1f,
        StanceExtension = 0.74f,
        ToeLength = 1.05f,
        HeelLength = 0.35f,

        TorsoBaseY = 0.45f,
        TorsoY = 0.5f,
        ShoulderOffset = new Vector3(1.75f, 1.55f, -0.1f),
        ElbowOffset = new Vector3(0.3f, -1.5f, 0f),
        AimOriginY = 1.1f,
        HeadOffset = new Vector3(0f, 1.45f, -1.05f),
        ArmSwing = 0.6f,

        RightArmMount = new Vector3(0f, 0f, -1.9f),
        LeftArmMount = new Vector3(0f, 0f, -1.9f),
        TorsoMount = new Vector3(0.95f, 2.25f, 0.4f),

        ChestMountLeft = new Vector3(-0.75f, 0.45f, -1.4f),
        ChestMountRight = new Vector3(0.75f, 0.45f, -1.4f),
        TorsoHitOffset = new Vector3(0f, 1.0f, 0f),
        TorsoHitRadius = 1.95f,
        ArmHitOffset = new Vector3(0f, 0f, -1.4f),
        ArmHitRadius = 1.0f,
        LegsHitRadius = 1.8f,
        CockpitHitOffset = new Vector3(0f, 0.05f, -0.6f),
        CockpitHitRadius = 0.09f,
        ShoulderCover = System.Array.Empty<ShoulderPlate>(),
        CollapseDrop = 2.7f,
    };

    /// <summary>
    /// Brigandine at reference size: Bascinet-like legs under a torso whose shoulder joints sit on a high, level bar
    /// above the cockpit, which is tucked in just under it; the arms hang from the bar and their forearm guns ride high.
    /// </summary>
    private static MechChassis BrigandineBody() => new()
    {
        L1 = 3.3f,
        L2 = 3.4f,
        HipWidth = 1.5f,
        TorsoBaseY = 0.5f,
        TorsoY = 0.55f,
        ShoulderOffset = new Vector3(2.5f, 2.75f, 0.2f),
        ElbowOffset = new Vector3(0.35f, -1.6f, 0f),
        AimOriginY = 2.1f,
        HeadOffset = new Vector3(0f, 2.15f, -1.15f),
        ArmSwing = 0.8f,

        RightArmMount = new Vector3(0f, 0.15f, -2f),
        LeftArmMount = new Vector3(0f, 0.15f, -2f),
        TorsoMount = new Vector3(-1.4f, 3.3f, 0.5f),
        PointDefenseMount = new Vector3(1.4f, 3.2f, 0.6f),

        ChestMountLeft = new Vector3(-1.0f, 0.6f, -1.3f),
        ChestMountRight = new Vector3(1.0f, 0.6f, -1.3f),
        TorsoHitOffset = new Vector3(0f, 1.25f, 0f),
        TorsoHitRadius = 2.3f,
        ArmHitOffset = new Vector3(0f, 0f, -1.6f),
        ArmHitRadius = 1.3f,
        CockpitHitOffset = new Vector3(0f, 0f, -0.55f),
        CockpitHitRadius = 0.11f,
        ShoulderCover = System.Array.Empty<ShoulderPlate>(),
        CollapseDrop = 2.5f,
    };

    /// <summary>
    /// Harrow at reference size: short thick legs on a wide stance, a broad torso with two raised shoulder humps and the
    /// cockpit sunk between them; heavy forearms with the guns above the fists. The humps are cover plates (part of
    /// the arms), screening the cockpit from the side.
    /// </summary>
    private static MechChassis HarrowBody() => new()
    {
        L1 = 2.95f,
        L2 = 3.05f,
        AnkleHeight = 0.7f,
        HipWidth = 1.7f, // a wider stance put each foot on very different ground in craters (8 % of planted feet off target)
        HipRootY = -0.2f,
        StanceExtension = 0.75f, // crouched deep in its legs: shorter, straighter legs left too little bend for trenches
        ToeLength = 1.9f,
        HeelLength = 1.25f,

        TorsoBaseY = 0.5f,
        TorsoY = 0.5f,
        ShoulderOffset = new Vector3(2.75f, 1.55f, 0f),
        ElbowOffset = new Vector3(0.5f, -1.75f, 0f),
        AimOriginY = 1.3f,
        HeadOffset = new Vector3(0f, 1.75f, -1.05f),
        ArmSwing = 0.7f,

        RightArmMount = new Vector3(0f, 0.6f, -1.7f),
        LeftArmMount = new Vector3(0f, 0.6f, -1.7f),
        TorsoMount = new Vector3(0f, 2.3f, 1.3f),
        PointDefenseMount = new Vector3(0f, 2.2f, 0.9f),

        ChestMountLeft = new Vector3(-1.4f, 0.5f, -1.55f),
        ChestMountRight = new Vector3(1.4f, 0.5f, -1.55f),
        TorsoHitOffset = new Vector3(0f, 1.1f, 0f),
        TorsoHitRadius = 2.6f,
        ArmHitOffset = new Vector3(0f, 0.1f, -1.5f),
        ArmHitRadius = 1.55f,
        LegsHitRadius = 2.1f,
        CockpitHitOffset = new Vector3(0f, 0f, -0.55f),
        CockpitHitRadius = 0.1f,
        ShoulderCover = new[]
        {
            new ShoulderPlate { Center = new Vector3(0.1f, 0.85f, -0.1f), Size = new Vector3(1.3f, 1.5f, 2.0f) }, // hump
        },
        CollapseDrop = 2.2f,
    };

    /// <summary>
    /// Barbican at reference size: stubby legs on very large feet, a very wide, deep chest with the shoulder line well
    /// above a narrow visor slit under a flat brow plate (a cowled cockpit), arms hanging wide.
    /// </summary>
    private static MechChassis BarbicanBody() => new()
    {
        L1 = 3.0f,
        L2 = 3.1f,
        AnkleHeight = 0.8f,
        HipWidth = 1.8f,
        HipRootY = -0.2f,
        StanceExtension = 0.75f, // crouched: stubby legs need bend in reserve for trenches and mounds
        ToeLength = 2.2f,
        HeelLength = 1.4f,

        TorsoBaseY = 0.55f,
        TorsoY = 0.6f,
        ShoulderOffset = new Vector3(3.15f, 2.55f, 0.1f),
        ElbowOffset = new Vector3(0.5f, -2.0f, 0f),
        AimOriginY = 1.6f,
        HeadOffset = new Vector3(0f, 1.7f, -1.45f),
        ArmSwing = 0.6f,

        RightArmMount = new Vector3(0f, 0.05f, -2.1f),
        LeftArmMount = new Vector3(0f, 0.05f, -2.1f),
        TorsoMount = new Vector3(1.4f, 3.4f, 0.3f),
        PointDefenseMount = new Vector3(-1.5f, 3.35f, 0.4f),

        ChestMountLeft = new Vector3(-1.7f, 0.9f, -1.75f),
        ChestMountRight = new Vector3(1.7f, 0.9f, -1.75f),
        TorsoHitOffset = new Vector3(0f, 1.5f, 0f),
        TorsoHitRadius = 2.75f,
        ArmHitOffset = new Vector3(0f, 0f, -1.7f),
        ArmHitRadius = 1.5f,
        LegsHitRadius = 2.2f,
        CockpitHitOffset = new Vector3(0f, 0f, -0.45f),
        CockpitHitRadius = 0.09f,
        ShoulderCover = new[]
        {
            new ShoulderPlate { Center = new Vector3(-0.3f, 0.45f, 0f), Size = new Vector3(1.6f, 1.1f, 2.1f) }, // pauldron
        },
        CollapseDrop = 2.4f,
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
    /// duels: the Bascinet is the bigger, taller target (more sphere area, a torso that shows over crests), so it gets
    /// the heavier plate. 1.0 = the class's base values.
    /// </summary>
    public static float ArmourFactor(MechModel model) => model switch
    {
        MechModel.Bascinet => BascinetArmour,
        // The same humanoid target size, but no shoulder cover to soak hits into its arms: at 1.2 it lost every duel
        // series (23-17 .. 26-14); at 1.3, once side torsos shrank the centre sphere and the Bascinet's plates caught
        // more of the fire, 25-15 (1.4: 24-16, 1.5: 21-19).
        MechModel.Brigandine => 1.5f,
        _ => 1f,
    };

    private const float BascinetArmour = 1.3f;

    /// <summary>Armour on every class since the loadouts grew to 4-6 weapons (time to kill kept near the old).</summary>
    private const float LoadoutArmour = 1.2f;

    private void ApplyModelArmour(MechModel model)
    {
        float k = ArmourFactor(model);
        TorsoHp *= k;
        ArmHp *= k;
        LegsHp *= k;
    }

    /// <summary>Puts one weapon in each arm (fire bits 0 and 1), keeping whatever else is mounted.</summary>
    private void SetWeapons(WeaponSpec right, WeaponSpec left)
    {
        var rest = Array.FindAll(Loadout, m => m.At is not (Hardpoint.RightArm or Hardpoint.LeftArm));
        Loadout = [new MountDef(right, Hardpoint.RightArm), new MountDef(left, Hardpoint.LeftArm), .. rest];
    }

    /// <summary>The weapon on the right arm (the first one), if any.</summary>
    public WeaponSpec? RightArmWeapon => Array.Find(Loadout, m => m.At == Hardpoint.RightArm).Weapon;

    /// <summary>The weapon on the left arm (the first one), if any.</summary>
    public WeaponSpec? LeftArmWeapon => Array.Find(Loadout, m => m.At == Hardpoint.LeftArm).Weapon;

    /// <summary>
    /// Where weapon <paramref name="index"/> is bolted on, in its hardpoint's frame (elbow for arms, torso otherwise),
    /// at this chassis' size. The k-th weapon on a hardpoint sits k x 0.55 m (medium size) below the first on an arm,
    /// and as far further out on the chest and rack.
    /// </summary>
    public Vector3 MountAnchor(int index)
    {
        var at = Loadout[index].At;
        int k = 0;
        for (int i = 0; i < index; i++) if (Loadout[i].At == at) k++;
        float step = 0.55f * Scale * k;
        return at switch
        {
            Hardpoint.RightArm => RightArmMount - new Vector3(0f, step, 0f),
            Hardpoint.LeftArm => LeftArmMount - new Vector3(0f, step, 0f),
            Hardpoint.Rack => TorsoMount + new Vector3(MathF.Sign(TorsoMount.X) * step, 0f, 0f),
            Hardpoint.ChestLeft => ChestMountLeft - new Vector3(step, 0f, 0f),
            _ => ChestMountRight + new Vector3(step, 0f, 0f),
        };
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
        ChestMountLeft *= s;
        ChestMountRight *= s;
        PointDefenseMount *= s;
        TorsoHitOffset *= s;
        TorsoHitRadius *= s;
        ArmHitOffset *= s;
        ArmHitRadius *= s;
        LegsHitRadius *= s;
        CockpitHitOffset *= s;
        CockpitHitRadius *= s;
        var cover = new ShoulderPlate[ShoulderCover.Length];
        for (int i = 0; i < cover.Length; i++) cover[i] = new ShoulderPlate { Center = ShoulderCover[i].Center * s, Size = ShoulderCover[i].Size * s };
        ShoulderCover = cover;
        CollapseDrop *= s;
        return this;
    }
}

/// <summary>Where on the body a weapon is bolted: an arm, the shoulder rack, or a chest anchor.</summary>
public enum Hardpoint { RightArm, LeftArm, Rack, ChestLeft, ChestRight }

/// <summary>One weapon of a loadout and the hardpoint it sits on.</summary>
public readonly record struct MountDef(WeaponSpec Weapon, Hardpoint At);
