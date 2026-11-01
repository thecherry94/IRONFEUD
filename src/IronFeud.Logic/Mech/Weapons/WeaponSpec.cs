using System;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>Projectile: a shell (ballistic). Beam: hitscan. Missile: a self-propelled round, fired in salvos.</summary>
public enum WeaponKind { Projectile, Beam, Missile }

/// <summary>Static weapon data. The prototype's damage/heat/cooldown; ranges and shell speed rescaled for genre mech speeds (docs/combat-ai.md).</summary>
public sealed record WeaponSpec
{
    public required string Name { get; init; }
    public required WeaponKind Kind { get; init; }
    public required float Damage { get; init; }
    public required float Heat { get; init; }
    public required float Cooldown { get; init; }

    /// <summary>
    /// Ranges, the same rule for every weapon: full damage out to <see cref="OptimalRange"/>, then a straight line
    /// down to nothing at <see cref="Range"/> (beams stop there, shells expire there).
    /// </summary>
    public required float Range { get; init; }
    public required float OptimalRange { get; init; }

    // Projectile
    public float MuzzleVelocity { get; init; }
    public float Gravity { get; init; }
    public float MaxLifetime { get; init; } = 3f;

    public float BeamVisualTime { get; init; } = 0.22f;

    /// <summary>Effectiveness against armour plates and against exposed internal structure (1 = full damage).</summary>
    public float VsArmor { get; init; } = 1f;
    public float VsStructure { get; init; } = 1f;

    /// <summary>
    /// Burn lasers: the beam stays on this long after the trigger and deals <see cref="Damage"/> spread evenly over
    /// it, every tick along wherever the arm points then. Holding it on target is the skill; a dodging target takes
    /// a fraction, smeared across sections. 0 = the whole damage in one instant ray. Cooldown counts from the trigger.
    /// </summary>
    public float BurnTime { get; init; }

    // Missiles: a salvo of Pulses rounds, PulseInterval apart, each with an equal share of Damage.
    /// <summary>Cruise speed, m/s (they leave the tube at half of it and reach it in 0.5 s).</summary>
    public float MissileSpeed { get; init; }

    /// <summary>Half-angle of the cone each unguided round leaves in, radians.</summary>
    public float Spread { get; init; }

    /// <summary>Guided rounds home on the launcher's lock (see <see cref="LockTime"/>) at <see cref="TurnRate"/> rad/s.</summary>
    public bool Guided { get; init; }
    public float TurnRate { get; init; }

    /// <summary>Launch elevation, radians: guided rounds climb first and dive on the target, over cover.</summary>
    public float Loft { get; init; }

    /// <summary>Seconds a target must stay spotted and in the aim cone to lock.</summary>
    public float LockTime { get; init; }

    /// <summary>Rounds arm only past this distance from the launcher: closer impacts do no damage.</summary>
    public float MinRange { get; init; }

    /// <summary>Pulse lasers: the trigger fires this many instant pulses <see cref="PulseInterval"/> apart, each carrying an equal share.</summary>
    public int Pulses { get; init; } = 1;
    public float PulseInterval { get; init; }

    // Looks, in the rig's medium-size units: how far the muzzle sits ahead of the arm's weapon mount, the barrel
    // radius (or emitter size for beams) and the number of barrels (or lenses). Also sizes muzzle flash and impacts.
    public float Length { get; init; } = 3f;
    public float Caliber { get; init; } = 0.26f;
    public int Barrels { get; init; } = 1;

    // Recoil impulses (spring velocities). The torso yaw kick is applied as side · TorsoYawKick,
    // i.e. the firing arm's shoulder is pushed back.
    public float ArmKick { get; init; }
    public float ElbowKick { get; init; }
    public float TorsoYawKick { get; init; }
    public float TorsoPitchKick { get; init; }
    public float Trauma { get; init; }

    public float DamageAt(float distance)
    {
        if (distance <= OptimalRange) return Damage;
        return Damage * MathF.Max(0f, 1f - (distance - OptimalRange) / MathF.Max(1e-3f, Range - OptimalRange));
    }

    /// <summary>Distance out to which the weapon still does 40 % damage: the AI doesn't bother firing beyond it.</summary>
    public float EffectiveRange => OptimalRange + 0.6f * (Range - OptimalRange);

    // The roster, light to heavy.
    // Heavier guns hit far harder but cycle slowly, run hot and fire slower shells: a running light can dodge them.

    /// <summary>Light beam: a quick burst of three pulses, the damage up front instead of held on target.</summary>
    public static readonly WeaponSpec PulseLaser = new()
    {
        Name = "Pulse laser", Kind = WeaponKind.Beam, Damage = 9f, Heat = 0.09f, Cooldown = 0.9f,
        Pulses = 3, PulseInterval = 0.09f,
        Range = 280f, OptimalRange = 180f, BeamVisualTime = 0.06f,
        Length = 1.1f, Caliber = 0.6f, Barrels = 1,
        ArmKick = 0.3f, TorsoYawKick = 0.05f, Trauma = 0.03f,
    };

    /// <summary>Light gun: twin barrels, fast shells, small rounds four times a second.</summary>
    public static readonly WeaponSpec Autocannon = new()
    {
        Name = "Autocannon", Kind = WeaponKind.Projectile, Damage = 4f, Heat = 0.03f, Cooldown = 0.25f,
        Range = 520f, OptimalRange = 300f, MuzzleVelocity = 560f, Gravity = 4f, MaxLifetime = 1.5f,
        Length = 2.6f, Caliber = 0.12f, Barrels = 2,
        ArmKick = 0.8f, ElbowKick = 1f, TorsoYawKick = 0.06f, TorsoPitchKick = 0.05f, Trauma = 0.05f,
    };

    /// <summary>A 1.25 s burn; same damage per second as the old instant 9-point laser if it is held on target.</summary>
    public static readonly WeaponSpec Laser = new()
    {
        Name = "Laser", Kind = WeaponKind.Beam, Damage = 20f, Heat = 0.24f, Cooldown = 2.25f, BurnTime = 1.25f,
        Range = 320f, OptimalRange = 200f,
        Length = 1.5f, Caliber = 1f, Barrels = 3,
        ArmKick = 0.8f, TorsoYawKick = 0.12f, Trauma = 0.08f,
    };

    /// <summary>The medium's harder hitter.</summary>
    public static readonly WeaponSpec Cannon = new()
    {
        Name = "Cannon", Kind = WeaponKind.Projectile, Damage = 14f, Heat = 0.08f, Cooldown = 0.75f,
        Range = 640f, OptimalRange = 400f, MuzzleVelocity = 450f, Gravity = 4f, MaxLifetime = 2f,
        Length = 3.4f, Caliber = 0.26f, Barrels = 1,
        ArmKick = 3.4f, ElbowKick = 4f, TorsoYawKick = 0.35f, TorsoPitchKick = 0.25f, Trauma = 0.3f,
    };

    /// <summary>A 2 s burn: devastating held on a slow target, mostly smeared across a fast one.</summary>
    public static readonly WeaponSpec HeavyLaser = new()
    {
        Name = "Heavy laser", Kind = WeaponKind.Beam, Damage = 32f, Heat = 0.35f, Cooldown = 3.5f, BurnTime = 2f,
        Range = 380f, OptimalRange = 240f, BeamVisualTime = 0.35f,
        Length = 2f, Caliber = 1.4f, Barrels = 3,
        ArmKick = 1.5f, TorsoYawKick = 0.2f, Trauma = 0.15f,
    };

    public static readonly WeaponSpec HeavyCannon = new()
    {
        Name = "Heavy cannon", Kind = WeaponKind.Projectile, Damage = 32f, Heat = 0.2f, Cooldown = 2.2f,
        Range = 600f, OptimalRange = 360f, MuzzleVelocity = 330f, Gravity = 4f, MaxLifetime = 2.5f,
        Length = 3.9f, Caliber = 0.38f, Barrels = 1,
        ArmKick = 5f, ElbowKick = 6f, TorsoYawKick = 0.6f, TorsoPitchKick = 0.45f, Trauma = 0.5f,
    };

    /// <summary>Short-range unguided rocket salvo: six rounds in a wide cone, a shotgun up close.</summary>
    public static readonly WeaponSpec RocketPod = new()
    {
        Name = "Rocket pod", Kind = WeaponKind.Missile, Damage = 18f, Heat = 0.12f, Cooldown = 3f,
        Range = 220f, OptimalRange = 130f, Pulses = 6, PulseInterval = 0.05f,
        MissileSpeed = 200f, Spread = 0.035f, MaxLifetime = 2f,
        Length = 1.2f, Caliber = 0.5f, Barrels = 6,
        TorsoPitchKick = 0.05f, Trauma = 0.06f,
    };

    /// <summary>Medium-range unguided salvo: ten fast rounds in a tight pattern. No homing: aim it.</summary>
    public static readonly WeaponSpec StrikeRack = new()
    {
        Name = "Strike rack", Kind = WeaponKind.Missile, Damage = 25f, Heat = 0.16f, Cooldown = 4f,
        Range = 400f, OptimalRange = 300f, Pulses = 10, PulseInterval = 0.06f,
        MissileSpeed = 260f, Spread = 0.015f, MaxLifetime = 2.5f,
        Length = 1.4f, Caliber = 0.6f, Barrels = 10,
        TorsoPitchKick = 0.06f, Trauma = 0.08f,
    };

    /// <summary>
    /// Long-range guided salvo, the heavy's support weapon: ten slow rounds launched steeply that dive on a locked
    /// target, over cover. The lock needs the target spotted by any teammate; they don't arm under 120 m. (30 per
    /// salvo at 160 m minimum barely registered in lance games: 11.5 damage per salvo landed, heavies fight closer.)
    /// </summary>
    public static readonly WeaponSpec ArcMissiles = new()
    {
        Name = "Arc missiles", Kind = WeaponKind.Missile, Damage = 40f, Heat = 0.2f, Cooldown = 5f,
        Range = 800f, OptimalRange = 650f, Pulses = 10, PulseInterval = 0.1f,
        MissileSpeed = 140f, Spread = 0.05f, Guided = true, TurnRate = 1.6f, Loft = 0.9f, LockTime = 1.5f, MinRange = 120f,
        MaxLifetime = 9f,
        Length = 1.6f, Caliber = 0.7f, Barrels = 10,
        TorsoPitchKick = 0.08f, Trauma = 0.1f,
    };

    /// <summary>
    /// The assault's gun: one clean torso hit destroys a light (75 vs 60 HP). Slow shell, long reload, very hot:
    /// it has to be aimed at something that isn't dodging.
    /// </summary>
    public static readonly WeaponSpec SiegeCannon = new()
    {
        Name = "Siege cannon", Kind = WeaponKind.Projectile, Damage = 75f, Heat = 0.36f, Cooldown = 4.5f,
        Range = 700f, OptimalRange = 450f, MuzzleVelocity = 240f, Gravity = 5f, MaxLifetime = 3.5f,
        Length = 4.6f, Caliber = 0.55f, Barrels = 1,
        ArmKick = 8f, ElbowKick = 9f, TorsoYawKick = 1f, TorsoPitchKick = 0.8f, Trauma = 0.8f,
    };
}

/// <summary>
/// Point defense: each turret shoots down enemy missiles within <see cref="Range"/> that are closing on its mech or on
/// a teammate within <see cref="Cover"/> of it, up to <see cref="Rate"/> a second, each kill costing
/// <see cref="HeatPerKill"/>. Turrets add up, on one mech or across a lance moving together.
/// </summary>
public static class PointDefense
{
    // One turret should thin a volley, not erase it (design: stopping one takes investment, several turrets or several
    // equipped mechs). At 5 a second one shot down 9 of 10 slow arc missiles (they spend ~0.8 s inside 110 m).
    public const float Range = 110f, Cover = 120f, Rate = 1.5f, HeatPerKill = 0.01f;
}
