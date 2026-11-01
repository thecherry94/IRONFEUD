using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Mech;

/// <summary>
/// A whole mech at the fixed tick: locomotion, then weapons. Weapons fire from this tick's pose;
/// their recoil impulses land in the springs and show up from the next tick on.
/// Damage arrives between ticks via <see cref="ApplyHit"/>.
/// </summary>
public sealed class MechSim
{
    public readonly MechLocomotion Locomotion;
    public readonly MechWeapons Weapons;

    /// <summary>Persistent per-section damage; pass one in to continue from a previous battle.</summary>
    public readonly MechDamage Damage;

    /// <summary>Shots fired during the last <see cref="Step"/>.</summary>
    public readonly List<ShotFired> Shots = new();

    public MechSim(MechChassis chassis, uint seed = 1, MechDamage? damage = null)
    {
        Locomotion = new MechLocomotion(chassis, seed);
        Weapons = MechWeapons.ForChassis(chassis);
        Damage = damage ?? MechDamage.Create(chassis);
        ApplyDamageEffects();
    }

    public MechChassis Chassis => Locomotion.Chassis;
    public MechPose Pose => Locomotion.Pose;
    public bool IsOut => Damage.IsOut;

    public void Step(in MechInput input, float h)
    {
        Shots.Clear();
        Locomotion.Step(input, h);
        if (Locomotion.JetsBurning) Weapons.Heat += Chassis.JumpHeat * h;
        if (!IsOut) Weapons.Step(input, Locomotion, h, Shots);
    }

    /// <summary>Nearest hit volume along a ray (unit direction).</summary>
    public bool Raycast(Vector3 origin, Vector3 dir, float maxDistance, out RayHit hit) =>
        HitTest.Raycast(Pose.HitVolumes, origin, dir, maxDistance, out hit);

    /// <summary>Torso forward on the ground plane: the way its front armour faces (follows the torso twist).</summary>
    public Vector3 TorsoFacing
    {
        get
        {
            var d = Pose.AimDirection;
            d.Y = 0f;
            return d.LengthSquared() > 1e-8f ? Vector3.Normalize(d) : -Vector3.UnitZ;
        }
    }

    /// <param name="travel">Direction the shot was travelling, if known: torso hits from behind take the rear multiplier.</param>
    public DamageReport ApplyHit(MechSection struck, float amount, Vector3? travel = null)
    {
        var facing = Facing.Front;
        if (travel is { } dir)
        {
            facing = HitFacing.Classify(TorsoFacing, dir);
            if (struck == MechSection.Torso)
                amount *= 1f + (Chassis.RearDamageMultiplier - 1f) * HitFacing.RearWeight(TorsoFacing, dir);
        }
        var report = Damage.Apply(struck, amount);
        report.Facing = facing;
        if (report.Ignored) return report;
        Locomotion.Secondary.ApplyHit(amount);
        if (report.SectionDestroyed) ApplyDamageEffects();
        return report;
    }

    /// <summary>Maps section state onto the simulation: dead arms drop their weapons, dead legs cap speed, out = collapse.</summary>
    private void ApplyDamageEffects()
    {
        foreach (var m in Weapons.Mounts)
            m.Offline = !m.OnTorso && Damage.IsDestroyed(m.Arm == 0 ? MechSection.ArmLeft : MechSection.ArmRight);
        Locomotion.SpeedFactor = Damage.IsDestroyed(MechSection.Legs) ? Chassis.CrippledSpeedFactor : 1f;
        if (IsOut) Locomotion.Collapse(Locomotion.Secondary.RandomSigned() * 0.6f);
    }
}
