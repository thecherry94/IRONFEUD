using System.Numerics;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>A shell in flight. Collision is resolved by whoever owns the world geometry.</summary>
public struct Projectile
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Life;
    public WeaponSpec Spec;

    public static Projectile From(in ShotFired shot) => new()
    {
        Position = shot.Origin,
        Velocity = shot.Direction * shot.Spec.MuzzleVelocity,
        Spec = shot.Spec,
    };

    public bool Expired => Life > Spec.MaxLifetime;

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
