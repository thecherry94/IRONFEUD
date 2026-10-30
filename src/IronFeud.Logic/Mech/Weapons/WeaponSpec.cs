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

    public static readonly WeaponSpec Cannon = new()
    {
        Name = "Cannon",
        Kind = WeaponKind.Projectile,
        Damage = 14f,
        Heat = 0.08f,
        Cooldown = 0.75f,
        Range = 640f,
        MuzzleVelocity = 450f,
        Gravity = 4f,
        MaxLifetime = 2f,
        ArmKick = 3.4f,
        ElbowKick = 4f,
        TorsoYawKick = 0.35f,
        TorsoPitchKick = 0.25f,
        Trauma = 0.3f,
    };

    public static readonly WeaponSpec Laser = new()
    {
        Name = "Laser",
        Kind = WeaponKind.Beam,
        Damage = 9f,
        Heat = 0.11f,
        Cooldown = 1.0f,
        Range = 320f,
        FalloffStart = 200f,
        MinDamageFraction = 0.2f,
        ArmKick = 0.8f,
        TorsoYawKick = 0.12f,
        Trauma = 0.08f,
    };
}
