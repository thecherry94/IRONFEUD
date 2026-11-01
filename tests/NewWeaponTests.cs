using System;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>Machine guns, flamethrowers, incendiary rounds and particle cannons: what each is for.</summary>
public class NewWeaponTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public NewWeaponTests(ITestOutputHelper output) => _out = output;

    /// <summary>A shooter with <paramref name="weapon"/> in its right arm holds the trigger on a medium's torso.</summary>
    private (BattleUnit Shooter, BattleUnit Target, BattleSim Battle) Range(WeaponSpec weapon, float distance, MechDamage? targetDamage = null)
    {
        var b = new BattleSim(new ArenaMap { Radius = 900f }) { TrackOutcome = false };
        var c = MechChassis.Medium();
        c.RightArmWeapon = weapon;
        c.HeatDissipation = 10f; // the shooter's own heat is not the point here
        var shooter = b.AddUnit("S", 0, c, new Vector3(0, 0, distance), 0f);
        var target = b.AddUnit("T", 1, MechChassis.Medium(MechModel.Sicklefoot), Vector3.Zero, MathF.PI, damage: targetDamage);
        return (shooter, target, b);
    }

    private static void Fire(BattleSim b, BattleUnit shooter, BattleUnit target, float seconds)
    {
        float twist = 0f, aim = 0f;
        for (int i = 0; i < (int)(120 * seconds); i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = 1u, Convergence = Vector3.Distance(shooter.Position, target.Position) };
            b.Step(H);
        }
    }

    [Fact]
    public void MachineGuns_BarelyScratchArmour_ButShredExposedStructure()
    {
        var (s1, armoured, b1) = Range(WeaponSpec.MachineGun, 100f);
        Fire(b1, s1, armoured, 3f);
        var torso = (int)MechSection.Torso;
        float onArmour = armoured.Sim.Damage.Max(MechSection.Torso) - armoured.Sim.Damage[MechSection.Torso];

        var stripped = MechDamage.Create(MechChassis.Medium(MechModel.Sicklefoot));
        stripped.Armor[torso] = 0f;
        var (s2, bare, b2) = Range(WeaponSpec.MachineGun, 100f, stripped);
        Fire(b2, s2, bare, 3f);
        float onStructure = bare.Sim.Damage.MaxStructure[torso] - bare.Sim.Damage.Structure[torso];
        _out.WriteLine($"3 s of machine gun at 100 m: {onArmour:0.0} damage through armour, {onStructure:0.0} into exposed structure ({(bare.IsOut ? "destroyed" : "standing")})");
        Assert.True(onArmour < 15f, "machine guns barely scratch plates");
        Assert.True(bare.IsOut, "and shred what is underneath: the exposed torso goes in 3 s");
        Assert.True(onStructure > 3f * onArmour);
    }

    /// <summary>A target already running hot is pushed past the shutdown point by a flamethrower: it stops dead.</summary>
    [Fact]
    public void Flamethrowers_ShutAHotMechDown()
    {
        var (shooter, target, b) = Range(WeaponSpec.Flamethrower, 40f);
        target.Sim.Weapons.Heat = 0.75f;
        target.Input = new MechInput { Throttle = 1f };
        bool shut = false;
        float speedDuring = 99f;
        float twist = 0f, aim = 0f;
        for (int i = 0; i < 120 * 6; i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = 1u };
            target.Input = new MechInput { Throttle = 1f, FireMask = 0b11u };
            b.Step(H);
            if (target.Sim.Shutdown)
            {
                shut = true;
                speedDuring = MathF.Min(speedDuring, MathF.Abs(target.Sim.Locomotion.Body.V));
                Assert.Empty(target.Sim.Shots);
            }
        }
        _out.WriteLine($"shut down {shut}, slowest speed while shut down {speedDuring:0.0} m/s, heat now {target.Sim.Weapons.Heat:0.00}");
        Assert.True(shut, "the flamer should push it past the shutdown point");
        Assert.True(speedDuring < 2f, "a shut-down mech stops");
    }

    [Fact]
    public void IncendiaryRounds_BuildHeatInTheTarget()
    {
        var (shooter, target, b) = Range(WeaponSpec.IncendiaryCannon, 150f);
        float before = target.Sim.Weapons.Heat;
        Fire(b, shooter, target, 4f);
        _out.WriteLine($"4 s of incendiary rounds: target heat {before:0.00} -> {target.Sim.Weapons.Heat:0.00}");
        // Net of the target's heat sinks: on its own it won't shut an idle mech down, it eats a firing one's heat budget.
        Assert.True(target.Sim.Weapons.Heat > before + 0.1f);
    }

    /// <summary>A particle bolt is a projectile, but arrives at 400 m in a third of a second, hits hard, and scrambles sensors.</summary>
    [Fact]
    public void ParticleBolts_AreFastProjectiles_ThatScrambleSensors()
    {
        Assert.Equal(WeaponKind.Projectile, WeaponSpec.ParticleCannon.Kind);
        var (shooter, target, b) = Range(WeaponSpec.ParticleCannon, 400f);
        float twist = 0f, aim = 0f, firedAt = -1f, hitAt = -1f, scrambled = 0f;
        b.Damaged += (t, r, _, _) => { if (hitAt < 0f) hitAt = b.Time; };
        for (int i = 0; i < 120 * 4 && hitAt < 0f; i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = i > 120 ? 1u : 0u, Convergence = 400f };
            b.Step(H);
            if (firedAt < 0f && shooter.Stats.Shots > 0) firedAt = b.Time;
        }
        scrambled = 1f - target.Sim.SensorQuality;
        float taken = target.Sim.Damage.Max(MechSection.Torso) - target.Sim.Damage[MechSection.Torso];
        _out.WriteLine($"bolt flight {hitAt - firedAt:0.000} s to 400 m, {taken:0.0} damage, sensors {100 * scrambled:0}% scrambled");
        Assert.InRange(hitAt - firedAt, 0.2f, 0.45f);
        Assert.True(taken > 20f);
        Assert.True(scrambled > 0.3f);
        Assert.InRange(WeaponSpec.ParticleCannon.DamageAt(45f), 0.4f * 26f, 0.6f * 26f); // half strength at half the minimum range
    }
}
