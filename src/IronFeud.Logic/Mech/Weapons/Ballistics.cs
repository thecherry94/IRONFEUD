using System.Numerics;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>A shell in flight. Collision is resolved by whoever owns the world geometry.</summary>
public struct Projectile
{
    public Vector3 Position;
    public Vector3 Velocity;

    /// <summary>Where it left the muzzle: damage falls off with the distance flown (<see cref="WeaponSpec.DamageAt"/>).</summary>
    public Vector3 Origin;
    public float Life;
    public WeaponSpec Spec;

    public static Projectile From(in ShotFired shot) => new()
    {
        Position = shot.Origin,
        Origin = shot.Origin,
        Velocity = shot.Direction * shot.Spec.MuzzleVelocity,
        Spec = shot.Spec,
    };

    public float Flown => Vector3.Distance(Origin, Position);

    /// <summary>Past its lifetime or its maximum range (where it would do no damage any more).</summary>
    public bool Expired => Life > Spec.MaxLifetime || Flown > Spec.Range;

    /// <summary>Advances one step (semi-implicit Euler, as in the prototype). Returns the segment start.</summary>
    public Vector3 Step(float h)
    {
        Vector3 start = Position;
        Life += h;
        Velocity.Y -= Spec.Gravity * h;
        Position += Velocity * h;
        return start;
    }
}
