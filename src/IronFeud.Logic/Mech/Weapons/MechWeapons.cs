using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>A weapon bolted to an arm. Offset is in the elbow frame (forward = -Z).</summary>
public sealed class WeaponMount
{
    public readonly WeaponSpec Spec;

    /// <summary>Arm index: 0 = left, 1 = right (same order as legs and <see cref="MechPose.Elbows"/>); -1 = torso (shoulder rack).</summary>
    public readonly int Arm;

    public bool OnTorso => Arm < 0;

    /// <summary>The damage section carrying it: its arm, or the torso section the rack sits in.</summary>
    public readonly MechSection Section;

    public readonly Vector3 Offset;
    public float Cooldown;

    /// <summary>The section carrying it is destroyed.</summary>
    public bool Lost;

    /// <summary>Rounds left (salvos for missiles); -1 = no ammunition needed (energy weapons).</summary>
    public int Ammo;

    /// <summary>Where its rounds sit: its own section, or for an arm weapon the side torso the arm hangs from.</summary>
    public readonly MechSection AmmoSection;

    /// <summary>Can't fire any more: its section is gone or it is out of rounds.</summary>
    public bool Offline => Lost || Ammo == 0;

    /// <summary>Seconds left of the current burn (burn lasers).</summary>
    public float BurnLeft;

    /// <summary>Pulses still to come in the current burst, and the time to the next one (pulse lasers).</summary>
    public int PulsesLeft;
    public float PulseTimer;

    public bool Burning => BurnLeft > 0f;

    /// <summary>Cooldown multiplier from damage to the arm's internals (1 = intact, up to 2).</summary>
    public float CycleFactor = 1f;

    /// <param name="section">For torso mounts (arm -1): the torso section it sits in; arms always use their arm.</param>
    public WeaponMount(WeaponSpec spec, int arm, Vector3 offset, MechSection section = MechSection.Torso)
    {
        Spec = spec;
        Arm = arm;
        Offset = offset;
        Section = arm == 0 ? MechSection.ArmLeft : arm == 1 ? MechSection.ArmRight : section;
        AmmoSection = MechDamage.Parent(Section) is var parent && arm >= 0 ? parent : Section;
        Ammo = spec.UsesAmmo ? spec.Rounds : -1;
    }

    public int Side => Arm == 0 ? +1 : Arm == 1 ? -1 : 0;
}

/// <summary>Single: one shell, instant beam or pulse. Start / Hold: the first and every later tick of a burn.</summary>
public enum ShotPhase { Single, Start, Hold }

/// <summary>A shot leaving a muzzle this tick. The world (Godot or a headless resolver) decides what it hits.</summary>
public struct ShotFired
{
    public int Weapon;
    public WeaponSpec Spec;
    public Vector3 Origin;

    /// <summary>Unit direction from the muzzle toward the convergence point.</summary>
    public Vector3 Direction;

    public ShotPhase Phase;

    /// <summary>Fraction of <see cref="WeaponSpec.Damage"/> this shot carries (a pulse, or one tick of a burn); 0 = all of it.</summary>
    public float Share;

    public float DamageFraction => Share > 0f ? Share : 1f;
}

/// <summary>Cooldowns, heat and firing. Weapons converge on the torso aim ray at the pilot's convergence distance.</summary>
public sealed class MechWeapons
{
    public const float DefaultConvergence = 200f;

    public readonly List<WeaponMount> Mounts = new();
    public float Heat;

    public bool HeatLocked => Heat >= 1f;

    /// <summary>
    /// The most a mech's own guns can heat it: under the shutdown line (<see cref="MechSim.ShutdownHeat"/>), so only
    /// outside heat and jets scram a reactor. A heavy shot fired at 0.99 (a particle bolt adds 0.32) used to shut its
    /// own mech down.
    /// </summary>
    public const float OwnHeatCap = MechSim.ShutdownHeat - 0.05f;

    /// <summary>Heat sink capacity left after damage to the torso's internals (1 = intact, down to 0.4).</summary>
    public float DissipationFactor = 1f;

    /// <summary>
    /// The chassis' loadout, in its order (mount i is bit i of the fire mask). Muzzles sit the weapon's length ahead
    /// of the mount's anchor (<see cref="MechChassis.MountAnchor"/>).
    /// </summary>
    public static MechWeapons ForChassis(MechChassis t)
    {
        var w = new MechWeapons { Heat = t.HeatStart };
        for (int i = 0; i < t.Loadout.Length; i++)
        {
            var (spec, at) = t.Loadout[i];
            Vector3 muzzle = t.MountAnchor(i) - Vector3.UnitZ * (spec.Length * t.Scale);
            w.Mounts.Add(at switch
            {
                Hardpoint.RightArm => new WeaponMount(spec, 1, muzzle),
                Hardpoint.LeftArm => new WeaponMount(spec, 0, muzzle),
                Hardpoint.ChestLeft => new WeaponMount(spec, -1, muzzle, MechSection.TorsoLeft),
                Hardpoint.ChestRight => new WeaponMount(spec, -1, muzzle, MechSection.TorsoRight),
                _ => new WeaponMount(spec, -1, muzzle, TorsoSectionAt(t.TorsoMount.X, t.Scale)),
            });
        }
        return w;
    }

    /// <summary>The torso section a torso-local x falls in: a side torso unless within 0.3 m (medium size) of the middle; left = -x.</summary>
    public static MechSection TorsoSectionAt(float x, float scale) =>
        x < -0.3f * scale ? MechSection.TorsoLeft : x > 0.3f * scale ? MechSection.TorsoRight : MechSection.Torso;

    public Vector3 MuzzlePosition(int weapon, MechPose pose)
    {
        var m = Mounts[weapon];
        if (m.OnTorso) return pose.TorsoWorldPosition + Vector3.Transform(m.Offset, pose.TorsoWorldRotation);
        return pose.ElbowWorldPositions[m.Arm] + Vector3.Transform(m.Offset, pose.ElbowWorldRotations[m.Arm]);
    }

    public bool CanFire(int weapon) => Mounts[weapon].Cooldown <= 0f && !Mounts[weapon].Offline && !HeatLocked;

    /// <summary>Dissipates heat, ticks cooldowns, fires requested weapons and applies recoil to the springs.</summary>
    public void Step(in MechInput input, MechLocomotion loco, float h, List<ShotFired> shots)
    {
        var t = loco.Chassis;
        float speedFrac = MathF.Abs(loco.Body.V) / t.VMax;
        Heat = MathF.Max(t.HeatFloor, Heat - h * (t.HeatDissipation * DissipationFactor - t.HeatDissipationMovePenalty * speedFrac));

        var pose = loco.Pose;
        float convergence = input.Convergence > 0f ? input.Convergence : DefaultConvergence;
        Vector3 torsoTarget = pose.AimOrigin + pose.AimDirection * convergence;
        Vector3 armTarget = pose.AimOrigin + pose.ArmAimDirection * convergence;

        for (int i = 0; i < Mounts.Count; i++)
        {
            var m = Mounts[i];
            Vector3 target = m.OnTorso ? torsoTarget : armTarget; // arm weapons converge on the arms' aim
            m.Cooldown = MathF.Max(0f, m.Cooldown - h);
            if (m.Offline) m.BurnLeft = m.PulsesLeft = 0;

            // A burn started on an earlier tick keeps firing along the current aim; a burst fires its next pulse.
            if (m.Burning)
            {
                float dt = MathF.Min(h, m.BurnLeft);
                m.BurnLeft -= dt;
                shots.Add(Shot(i, pose, target, ShotPhase.Hold, dt / m.Spec.BurnTime));
            }
            if (m.PulsesLeft > 0 && (m.PulseTimer -= h) <= 0f)
            {
                m.PulsesLeft--;
                m.PulseTimer += m.Spec.PulseInterval;
                shots.Add(Shot(i, pose, target, ShotPhase.Single, 1f / m.Spec.Pulses));
                Recoil(loco.Secondary, m, 0.6f);
            }

            if ((input.FireMask & (1u << i)) == 0 || !CanFire(i)) continue;

            m.Cooldown = m.Spec.Cooldown * m.CycleFactor;
            if (m.Ammo > 0) m.Ammo--;
            Heat = MathF.Max(Heat, MathF.Min(Heat + m.Spec.Heat, OwnHeatCap));
            if (m.Spec.BurnTime > 0f)
            {
                float dt = MathF.Min(h, m.Spec.BurnTime);
                m.BurnLeft = m.Spec.BurnTime - dt;
                shots.Add(Shot(i, pose, target, ShotPhase.Start, dt / m.Spec.BurnTime));
            }
            else if (m.Spec.Pulses > 1)
            {
                m.PulsesLeft = m.Spec.Pulses - 1;
                m.PulseTimer = m.Spec.PulseInterval;
                shots.Add(Shot(i, pose, target, ShotPhase.Single, 1f / m.Spec.Pulses));
            }
            else shots.Add(Shot(i, pose, target, ShotPhase.Single, 1f));
            Recoil(loco.Secondary, m, 1f);
        }
    }

    private ShotFired Shot(int i, MechPose pose, Vector3 target, ShotPhase phase, float share)
    {
        Vector3 muzzle = MuzzlePosition(i, pose);
        return new ShotFired
        {
            Weapon = i,
            Spec = Mounts[i].Spec,
            Origin = muzzle,
            Direction = Vector3.Normalize(target - muzzle),
            Phase = phase,
            Share = share,
        };
    }

    private static void Recoil(SecondaryMotion sec, WeaponMount m, float k)
    {
        if (!m.OnTorso)
        {
            sec.Arms[m.Arm].V -= m.Spec.ArmKick * k;
            sec.Elbows[m.Arm].V -= m.Spec.ElbowKick * k;
        }
        sec.TorsoYaw.V += m.Side * m.Spec.TorsoYawKick * k;
        sec.TorsoPitch.V -= m.Spec.TorsoPitchKick * k;
        sec.AddTrauma(m.Spec.Trauma * k);
    }
}
