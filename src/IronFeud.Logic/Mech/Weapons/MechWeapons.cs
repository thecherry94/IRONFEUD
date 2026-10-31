using System;
using System.Collections.Generic;
using System.Numerics;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>A weapon bolted to an arm. Offset is in the elbow frame (forward = -Z).</summary>
public sealed class WeaponMount
{
    public readonly WeaponSpec Spec;

    /// <summary>Arm index: 0 = left, 1 = right (same order as legs and <see cref="MechPose.Elbows"/>).</summary>
    public readonly int Arm;
    public readonly Vector3 Offset;
    public float Cooldown;

    /// <summary>The arm carrying it is destroyed.</summary>
    public bool Offline;

    public WeaponMount(WeaponSpec spec, int arm, Vector3 offset)
    {
        Spec = spec;
        Arm = arm;
        Offset = offset;
    }

    public int Side => Arm == 0 ? +1 : -1;
}

/// <summary>A shot leaving a muzzle this tick. The world (Godot or a headless resolver) decides what it hits.</summary>
public struct ShotFired
{
    public int Weapon;
    public WeaponSpec Spec;
    public Vector3 Origin;

    /// <summary>Unit direction from the muzzle toward the convergence point.</summary>
    public Vector3 Direction;
}

/// <summary>Cooldowns, heat and firing. Weapons converge on the torso aim ray at the pilot's convergence distance.</summary>
public sealed class MechWeapons
{
    public const float DefaultConvergence = 200f;

    public readonly List<WeaponMount> Mounts = new();
    public float Heat;

    public bool HeatLocked => Heat >= 1f;

    /// <summary>
    /// The chassis' loadout: right arm weapon is bit 0 of the fire mask, left arm bit 1. Muzzles sit the weapon's
    /// length ahead of the arm's mount point.
    /// </summary>
    public static MechWeapons ForChassis(MechChassis t)
    {
        var w = new MechWeapons { Heat = t.HeatStart };
        w.Mounts.Add(new WeaponMount(t.RightArmWeapon, 1, t.RightArmMount - Vector3.UnitZ * (t.RightArmWeapon.Length * t.Scale)));
        w.Mounts.Add(new WeaponMount(t.LeftArmWeapon, 0, t.LeftArmMount - Vector3.UnitZ * (t.LeftArmWeapon.Length * t.Scale)));
        return w;
    }

    public Vector3 MuzzlePosition(int weapon, MechPose pose)
    {
        var m = Mounts[weapon];
        return pose.ElbowWorldPositions[m.Arm] + Vector3.Transform(m.Offset, pose.ElbowWorldRotations[m.Arm]);
    }

    public bool CanFire(int weapon) => Mounts[weapon].Cooldown <= 0f && !Mounts[weapon].Offline && !HeatLocked;

    /// <summary>Dissipates heat, ticks cooldowns, fires requested weapons and applies recoil to the springs.</summary>
    public void Step(in MechInput input, MechLocomotion loco, float h, List<ShotFired> shots)
    {
        var t = loco.Chassis;
        float speedFrac = MathF.Abs(loco.Body.V) / t.VMax;
        Heat = MathF.Max(t.HeatFloor, Heat - h * (t.HeatDissipation - t.HeatDissipationMovePenalty * speedFrac));

        var pose = loco.Pose;
        float convergence = input.Convergence > 0f ? input.Convergence : DefaultConvergence;
        Vector3 target = pose.AimOrigin + pose.AimDirection * convergence;

        for (int i = 0; i < Mounts.Count; i++)
        {
            var m = Mounts[i];
            m.Cooldown = MathF.Max(0f, m.Cooldown - h);
            if ((input.FireMask & (1u << i)) == 0 || !CanFire(i)) continue;

            m.Cooldown = m.Spec.Cooldown;
            Heat += m.Spec.Heat;
            Vector3 muzzle = MuzzlePosition(i, pose);
            shots.Add(new ShotFired
            {
                Weapon = i,
                Spec = m.Spec,
                Origin = muzzle,
                Direction = Vector3.Normalize(target - muzzle),
            });

            var sec = loco.Secondary;
            sec.Arms[m.Arm].V -= m.Spec.ArmKick;
            sec.Elbows[m.Arm].V -= m.Spec.ElbowKick;
            sec.TorsoYaw.V += m.Side * m.Spec.TorsoYawKick;
            sec.TorsoPitch.V -= m.Spec.TorsoPitchKick;
            sec.AddTrauma(m.Spec.Trauma);
        }
    }
}
