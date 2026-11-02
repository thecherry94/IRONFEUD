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

    /// <summary>Heat put into the target by a full hit (scaled like the damage): flamethrowers and incendiary rounds.</summary>
    public float HeatToTarget { get; init; }

    /// <summary>A flame stream rather than a laser (rendering and sound).</summary>
    public bool Flame { get; init; }

    /// <summary>A particle bolt: hits scramble the target's sensors for <see cref="DisruptTime"/> seconds.</summary>
    public float DisruptTime { get; init; }

    /// <summary>
    /// Stability drained per point of damage that strikes (see <see cref="MechSim.Stability"/>). Unset: by kind, shells
    /// 1 (the push of a heavy round), missiles 0.6 (warheads, spread), beams 0.15 (heat, no momentum).
    /// </summary>
    public float? ImpactPerDamage { get; init; }

    public float Impact => ImpactPerDamage ?? Kind switch { WeaponKind.Projectile => 1f, WeaponKind.Missile => 0.6f, _ => 0.15f };

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

    /// <summary>
    /// Missiles arm only past this distance (closer impacts are duds); a particle bolt ramps up to full damage over it
    /// (the field needs that distance to form).
    /// </summary>
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

    /// <summary>
    /// Rounds carried per mount (salvos for missiles); 0 = no ammunition (energy weapons, paid for in heat instead).
    /// First cut: about a minute of continuous fire for guns, 45 s for missile racks.
    /// </summary>
    public int Rounds { get; init; }

    public bool UsesAmmo => Rounds > 0;

    public float DamageAt(float distance)
    {
        if (Kind != WeaponKind.Missile && MinRange > 0f && distance < MinRange) return Damage * MathF.Max(0.2f, distance / MinRange);
        if (distance <= OptimalRange) return Damage;
        return Damage * MathF.Max(0f, 1f - (distance - OptimalRange) / MathF.Max(1e-3f, Range - OptimalRange));
    }

    /// <summary>Distance out to which the weapon still does 40 % damage: the AI doesn't bother firing beyond it.</summary>
    public float EffectiveRange => OptimalRange + 0.6f * (Range - OptimalRange);

    // The roster, light to heavy.
    // Heavier guns hit far harder but cycle slowly, run hot and fire slower shells: a running light can dodge them.

    /// <summary>The filler beam: short reach, a short burn, little heat. Lights' extra gun, bigger mechs' secondaries.</summary>
    public static readonly WeaponSpec SmallLaser = new()
    {
        Name = "Small laser", Kind = WeaponKind.Beam, Damage = 7f, Heat = 0.07f, Cooldown = 1.5f, BurnTime = 0.6f,
        Range = 220f, OptimalRange = 130f,
        Length = 1f, Caliber = 0.6f, Barrels = 1,
        ArmKick = 0.2f, TorsoYawKick = 0.03f, Trauma = 0.02f,
    };

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
        ImpactPerDamage = 0.8f,
        Name = "Autocannon", Kind = WeaponKind.Projectile, Damage = 4f, Heat = 0.03f, Cooldown = 0.25f, Rounds = 240,
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
        Name = "Cannon", Kind = WeaponKind.Projectile, Damage = 14f, Heat = 0.08f, Cooldown = 0.75f, Rounds = 80,
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
        Name = "Heavy cannon", Kind = WeaponKind.Projectile, Damage = 32f, Heat = 0.2f, Cooldown = 2.2f, Rounds = 28,
        Range = 600f, OptimalRange = 360f, MuzzleVelocity = 330f, Gravity = 4f, MaxLifetime = 2.5f,
        Length = 3.9f, Caliber = 0.38f, Barrels = 1,
        ArmKick = 5f, ElbowKick = 6f, TorsoYawKick = 0.6f, TorsoPitchKick = 0.45f, Trauma = 0.5f,
    };

    /// <summary>
    /// Twin machine guns: a hail of small rounds that barely scratch armour plates (15 %) but shred exposed internal
    /// structure (160 %): ~3 damage a second against plates, ~30 against what's underneath.
    /// </summary>
    public static readonly WeaponSpec MachineGun = new()
    {
        ImpactPerDamage = 0.3f,
        Name = "Machine gun", Kind = WeaponKind.Projectile, Damage = 1.5f, Heat = 0.004f, Cooldown = 0.08f, Rounds = 750,
        Range = 260f, OptimalRange = 160f, MuzzleVelocity = 900f, Gravity = 3f, MaxLifetime = 0.6f,
        VsArmor = 0.15f, VsStructure = 1.6f,
        Length = 1.6f, Caliber = 0.08f, Barrels = 2,
        ArmKick = 0.08f, Trauma = 0.01f,
    };

    /// <summary>
    /// A short-range burning stream: little damage, a lot of heat into the target (a full burn adds 0.5; at 0.3 lance
    /// games saw no shutdowns at all). Two burns on a mech running warm shut it down.
    /// </summary>
    public static readonly WeaponSpec Flamethrower = new()
    {
        ImpactPerDamage = 0f,
        Name = "Flamethrower", Kind = WeaponKind.Beam, Damage = 4f, Heat = 0.08f, Cooldown = 2f, BurnTime = 1.2f,
        Range = 80f, OptimalRange = 50f, HeatToTarget = 0.5f, Flame = true,
        Length = 1.3f, Caliber = 0.8f, Barrels = 1,
        Trauma = 0.02f,
    };

    /// <summary>
    /// An autocannon firing incendiary rounds: a third of its damage traded for heat in the target, ~0.2 a second
    /// on target (at 0.03 a round the target's heat sinks kept up with it).
    /// </summary>
    public static readonly WeaponSpec IncendiaryCannon = new()
    {
        ImpactPerDamage = 0.5f,
        Name = "Incendiary cannon", Kind = WeaponKind.Projectile, Damage = 2.6f, Heat = 0.03f, Cooldown = 0.25f, Rounds = 240,
        Range = 460f, OptimalRange = 260f, MuzzleVelocity = 520f, Gravity = 4f, MaxLifetime = 1.5f, HeatToTarget = 0.06f,
        Length = 2.6f, Caliber = 0.12f, Barrels = 2,
        ArmKick = 0.8f, ElbowKick = 1f, TorsoYawKick = 0.06f, TorsoPitchKick = 0.05f, Trauma = 0.05f,
    };

    /// <summary>
    /// A bolt of charged particles: a projectile so fast it is nearly instant at mid range (1300 m/s, no drop), hitting
    /// hard and running very hot. It needs 90 m to form (less damage inside that) and its hit scrambles the target's
    /// sensors for a moment.
    /// </summary>
    public static readonly WeaponSpec ParticleCannon = new()
    {
        ImpactPerDamage = 0.9f,
        Name = "Particle cannon", Kind = WeaponKind.Projectile, Damage = 26f, Heat = 0.32f, Cooldown = 3.2f,
        Range = 650f, OptimalRange = 450f, MinRange = 90f, MuzzleVelocity = 1300f, Gravity = 0f, MaxLifetime = 0.8f,
        DisruptTime = 1.5f,
        Length = 2.4f, Caliber = 0.42f, Barrels = 1,
        ArmKick = 2.2f, ElbowKick = 2.5f, TorsoYawKick = 0.3f, TorsoPitchKick = 0.2f, Trauma = 0.35f,
    };

    /// <summary>Short-range unguided rocket salvo: six rounds in a wide cone, a shotgun up close.</summary>
    public static readonly WeaponSpec RocketPod = new()
    {
        Name = "Rocket pod", Kind = WeaponKind.Missile, Damage = 18f, Heat = 0.12f, Cooldown = 3f, Rounds = 15,
        Range = 220f, OptimalRange = 130f, Pulses = 6, PulseInterval = 0.05f,
        MissileSpeed = 200f, Spread = 0.035f, MaxLifetime = 2f,
        Length = 1.2f, Caliber = 0.5f, Barrels = 6,
        TorsoPitchKick = 0.05f, Trauma = 0.06f,
    };

    /// <summary>Medium-range unguided salvo: ten fast rounds in a tight pattern. No homing: aim it.</summary>
    public static readonly WeaponSpec StrikeRack = new()
    {
        Name = "Strike rack", Kind = WeaponKind.Missile, Damage = 25f, Heat = 0.16f, Cooldown = 4f, Rounds = 12,
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
        Name = "Arc missiles", Kind = WeaponKind.Missile, Damage = 40f, Heat = 0.2f, Cooldown = 5f, Rounds = 10,
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
        Name = "Siege cannon", Kind = WeaponKind.Projectile, Damage = 75f, Heat = 0.36f, Cooldown = 4.5f, Rounds = 14,
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
