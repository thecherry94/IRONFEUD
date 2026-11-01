using System;

namespace IronFeud.Logic.Mech.Weapons;

public enum WeaponKind { Projectile, Beam }

/// <summary>Static weapon data. The prototype's damage/heat/cooldown; ranges and shell speed rescaled for genre mech speeds (docs/combat-ai.md).</summary>
public sealed record WeaponSpec
{
    public required string Name { get; init; }
    public required WeaponKind Kind { get; init; }
    public required float Damage { get; init; }
    public required float Heat { get; init; }
    public required float Cooldown { get; init; }
    public required float Range { get; init; }

    // Projectile
    public float MuzzleVelocity { get; init; }
    public float Gravity { get; init; }
    public float MaxLifetime { get; init; } = 3f;

    // Beam: full damage up to FalloffStart, then linear down to MinDamageFraction at Range.
    public float FalloffStart { get; init; } = float.PositiveInfinity;
    public float MinDamageFraction { get; init; } = 1f;
    public float BeamVisualTime { get; init; } = 0.22f;

    /// <summary>
    /// Burn lasers: the beam stays on this long after the trigger and deals <see cref="Damage"/> spread evenly over
    /// it, every tick along wherever the arm points then. Holding it on target is the skill; a dodging target takes
    /// a fraction, smeared across sections. 0 = the whole damage in one instant ray. Cooldown counts from the trigger.
    /// </summary>
    public float BurnTime { get; init; }

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
        if (distance <= FalloffStart) return Damage;
        float t = (distance - FalloffStart) / MathF.Max(1e-3f, Range - FalloffStart);
        return Damage * MathF.Max(MinDamageFraction, 1f - t);
    }

    // The roster, light to heavy. Projectile damage is the same at any range; beams fall off past FalloffStart.
    // Heavier guns hit far harder but cycle slowly, run hot and fire slower shells: a running light can dodge them.

    /// <summary>Light beam: a quick burst of three pulses, the damage up front instead of held on target.</summary>
    public static readonly WeaponSpec PulseLaser = new()
    {
        Name = "Pulse laser", Kind = WeaponKind.Beam, Damage = 9f, Heat = 0.09f, Cooldown = 0.9f,
        Pulses = 3, PulseInterval = 0.09f,
        Range = 280f, FalloffStart = 180f, MinDamageFraction = 0.2f, BeamVisualTime = 0.06f,
        Length = 1.1f, Caliber = 0.6f, Barrels = 1,
        ArmKick = 0.3f, TorsoYawKick = 0.05f, Trauma = 0.03f,
    };

    /// <summary>Light gun: twin barrels, fast shells, small rounds four times a second.</summary>
    public static readonly WeaponSpec Autocannon = new()
    {
        Name = "Autocannon", Kind = WeaponKind.Projectile, Damage = 4f, Heat = 0.03f, Cooldown = 0.25f,
        Range = 520f, MuzzleVelocity = 560f, Gravity = 4f, MaxLifetime = 1.5f,
        Length = 2.6f, Caliber = 0.12f, Barrels = 2,
        ArmKick = 0.8f, ElbowKick = 1f, TorsoYawKick = 0.06f, TorsoPitchKick = 0.05f, Trauma = 0.05f,
    };

    /// <summary>A 1.25 s burn; same damage per second as the old instant 9-point laser if it is held on target.</summary>
    public static readonly WeaponSpec Laser = new()
    {
        Name = "Laser", Kind = WeaponKind.Beam, Damage = 20f, Heat = 0.24f, Cooldown = 2.25f, BurnTime = 1.25f,
        Range = 320f, FalloffStart = 200f, MinDamageFraction = 0.2f,
        Length = 1.5f, Caliber = 1f, Barrels = 3,
        ArmKick = 0.8f, TorsoYawKick = 0.12f, Trauma = 0.08f,
    };

    /// <summary>The medium's harder hitter.</summary>
    public static readonly WeaponSpec Cannon = new()
    {
        Name = "Cannon", Kind = WeaponKind.Projectile, Damage = 14f, Heat = 0.08f, Cooldown = 0.75f,
        Range = 640f, MuzzleVelocity = 450f, Gravity = 4f, MaxLifetime = 2f,
        Length = 3.4f, Caliber = 0.26f, Barrels = 1,
        ArmKick = 3.4f, ElbowKick = 4f, TorsoYawKick = 0.35f, TorsoPitchKick = 0.25f, Trauma = 0.3f,
    };

    /// <summary>A 2 s burn: devastating held on a slow target, mostly smeared across a fast one.</summary>
    public static readonly WeaponSpec HeavyLaser = new()
    {
        Name = "Heavy laser", Kind = WeaponKind.Beam, Damage = 32f, Heat = 0.35f, Cooldown = 3.5f, BurnTime = 2f,
        Range = 380f, FalloffStart = 240f, MinDamageFraction = 0.25f, BeamVisualTime = 0.35f,
        Length = 2f, Caliber = 1.4f, Barrels = 3,
        ArmKick = 1.5f, TorsoYawKick = 0.2f, Trauma = 0.15f,
    };

    public static readonly WeaponSpec HeavyCannon = new()
    {
        Name = "Heavy cannon", Kind = WeaponKind.Projectile, Damage = 32f, Heat = 0.2f, Cooldown = 2.2f,
        Range = 600f, MuzzleVelocity = 330f, Gravity = 4f, MaxLifetime = 2.5f,
        Length = 3.9f, Caliber = 0.38f, Barrels = 1,
        ArmKick = 5f, ElbowKick = 6f, TorsoYawKick = 0.6f, TorsoPitchKick = 0.45f, Trauma = 0.5f,
    };

    /// <summary>
    /// The assault's gun: one clean torso hit destroys a light (75 vs 60 HP). Slow shell, long reload, very hot:
    /// it has to be aimed at something that isn't dodging.
    /// </summary>
    public static readonly WeaponSpec SiegeCannon = new()
    {
        Name = "Siege cannon", Kind = WeaponKind.Projectile, Damage = 75f, Heat = 0.36f, Cooldown = 4.5f,
        Range = 700f, MuzzleVelocity = 240f, Gravity = 5f, MaxLifetime = 3.5f,
        Length = 4.6f, Caliber = 0.55f, Barrels = 1,
        ArmKick = 8f, ElbowKick = 9f, TorsoYawKick = 1f, TorsoPitchKick = 0.8f, Trauma = 0.8f,
    };
}
