using System;
using System.Numerics;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Tests;

public class WeaponTests
{
    private const float H = 1f / 120f;
    private const uint Cannon = 1u << 0, Laser = 1u << 1;

    private static MechSim NewSim() => new(new MechChassis());

    [Fact]
    public void DefaultLoadout_CannonRight_LaserLeft()
    {
        var sim = NewSim();
        Assert.Equal("Cannon", sim.Weapons.Mounts[0].Spec.Name);
        Assert.Equal(1, sim.Weapons.Mounts[0].Arm);
        Assert.True(sim.Weapons.MuzzlePosition(0, sim.Pose).X > 0f, "cannon muzzle should be on the right (+X)");
        Assert.True(sim.Weapons.MuzzlePosition(1, sim.Pose).X < 0f, "laser emitter should be on the left (-X)");
        Assert.True(sim.Weapons.MuzzlePosition(0, sim.Pose).Z < -3f, "cannon barrel points forward (-Z)");
    }

    [Fact]
    public void HeldTrigger_FiresAtCooldownRate_AndAddsHeat()
    {
        var sim = NewSim();
        int shots = 0;
        float heat0 = sim.Weapons.Heat;
        for (int i = 0; i < 120 * 3; i++)
        {
            sim.Step(new MechInput { FireMask = Cannon }, H);
            shots += sim.Shots.Count;
        }
        Assert.Equal(4, shots); // t = 0, 0.75, 1.5, 2.25 (3 s window)
        Assert.True(sim.Weapons.Heat > heat0 + 4 * 0.08f - 3f * 0.055f - 1e-3f);
    }

    [Fact]
    public void HeatLock_BlocksFiring_UntilBelowOne()
    {
        var sim = NewSim();
        sim.Weapons.Heat = 1.01f; // one tick of dissipation (~0.0005) keeps it above 1
        sim.Step(new MechInput { FireMask = Cannon | Laser }, H);
        Assert.Empty(sim.Shots);

        // Below 1 the cannon fires, and its heat locks the laser in the same tick (checks are sequential).
        sim.Weapons.Heat = 0.99f;
        sim.Step(new MechInput { FireMask = Cannon | Laser }, H);
        Assert.Equal("Cannon", Assert.Single(sim.Shots).Spec.Name);
    }

    [Fact]
    public void Heat_Dissipates_SlowerWhileWalking_DownToFloor()
    {
        var standing = NewSim();
        var walking = NewSim();
        standing.Weapons.Heat = walking.Weapons.Heat = 0.8f;
        for (int i = 0; i < 120 * 5; i++)
        {
            standing.Step(default, H);
            walking.Step(new MechInput { Throttle = 1f }, H);
        }
        Assert.Equal(0.8f - 5f * 0.055f, standing.Weapons.Heat, 3);
        Assert.True(walking.Weapons.Heat > standing.Weapons.Heat);

        for (int i = 0; i < 120 * 30; i++) standing.Step(default, H);
        Assert.Equal(0.08f, standing.Weapons.Heat, 5);
    }

    [Fact]
    public void BothWeapons_ConvergeOnTheAimRay()
    {
        var sim = NewSim();
        // Walk and twist so the pose is not trivial.
        for (int i = 0; i < 120 * 4; i++) sim.Step(new MechInput { Throttle = 1f, TwistTarget = 0.6f, AimTarget = 0.1f }, H);
        const float conv = 85f;
        sim.Step(new MechInput { Throttle = 1f, TwistTarget = 0.6f, AimTarget = 0.1f, FireMask = Cannon | Laser, Convergence = conv }, H);
        Assert.Equal(2, sim.Shots.Count);

        // Shots are computed from the pose of the firing tick, before this tick's recoil lands.
        Vector3 target = sim.Pose.AimOrigin + sim.Pose.AimDirection * conv;
        foreach (var s in sim.Shots)
        {
            float along = Vector3.Dot(target - s.Origin, s.Direction);
            Vector3 closest = s.Origin + s.Direction * along;
            Assert.True(Vector3.Distance(closest, target) < 1e-2f, $"{s.Spec.Name} misses convergence point by {Vector3.Distance(closest, target)}");
        }
    }

    [Fact]
    public void AimRay_FollowsTwistAndAim()
    {
        var sim = NewSim();
        for (int i = 0; i < 120 * 4; i++) sim.Step(new MechInput { TwistTarget = 1.0f, AimTarget = 0.3f }, H);
        var d = sim.Pose.AimDirection;
        Assert.True(d.X < -0.5f, "positive twist turns the torso left (-X)");
        Assert.True(d.Y > 0.2f, "positive aim pitches up");
        Assert.Equal(1f, d.Length(), 4);
    }

    [Fact]
    public void CannonRecoil_KicksSpringsAndTrauma()
    {
        var sim = NewSim();
        sim.Step(new MechInput { FireMask = Cannon }, H);
        var sec = sim.Locomotion.Secondary;
        Assert.True(sec.Arms[1].V < -3f);
        Assert.True(sec.TorsoYaw.V < -0.3f, "right-arm recoil twists the torso right");
        Assert.True(sec.Trauma > 0.25f);
    }

    [Fact]
    public void Projectile_DropsUnderGravity()
    {
        var shot = new ShotFired { Spec = WeaponSpec.Cannon, Origin = new Vector3(0, 10, 0), Direction = -Vector3.UnitZ };
        var p = Projectile.From(shot);
        for (int i = 0; i < 120; i++) p.Step(H);
        Assert.Equal(-WeaponSpec.Cannon.MuzzleVelocity, p.Position.Z, 0);
        Assert.Equal(10f - 0.5f * WeaponSpec.Cannon.Gravity, p.Position.Y, 1);
        Assert.False(p.Expired);
    }

    [Fact]
    public void Damage_IsFullToOptimalRange_ThenFallsToZeroAtMaxRange()
    {
        // Full damage to the optimal range, then linear to nothing at maximum range, for every weapon.
        var l = WeaponSpec.Laser;
        float full = l.Damage;
        Assert.Equal(full, l.DamageAt(100f));
        Assert.Equal(full, l.DamageAt(200f));
        Assert.Equal(full * 0.5f, l.DamageAt(260f), 3);
        Assert.Equal(0f, l.DamageAt(320f), 3);
        Assert.Equal(0f, l.DamageAt(400f), 3);
        var c = WeaponSpec.Cannon;
        Assert.Equal(c.Damage, c.DamageAt(400f));
        Assert.Equal(c.Damage * 0.5f, c.DamageAt(520f), 3);
        Assert.Equal(0f, c.DamageAt(640f), 3);
    }
}

public class AimSolverTests
{
    [Fact]
    public void Track_PutsTheAimRayOnAPoint()
    {
        var sim = new MechSim(new MechChassis());
        var target = new Vector3(25f, 7f, -60f);
        float twist = 0f, aim = 0f, err = 1f;
        for (int i = 0; i < 120 * 4; i++)
        {
            err = AimSolver.Track(sim.Pose, sim.Locomotion.Body, sim.Chassis, target, ref twist, ref aim);
            sim.Step(new MechInput { TwistTarget = twist, AimTarget = aim }, 1f / 120f);
        }
        var p = sim.Pose;
        float along = Vector3.Dot(target - p.AimOrigin, p.AimDirection);
        float miss = Vector3.Distance(p.AimOrigin + p.AimDirection * along, target);
        Assert.True(miss < 0.1f, $"aim ray misses by {miss} m (err {err})");
    }

    /// <summary>
    /// Stabilised weapon mounts: walking or running, the aim ray stays within ~2 m of the pilot's line at 200 m (an
    /// enemy torso is ~5 m across). Before (no stabilisation, torso swing resonating at a running cadence) a running
    /// medium's aim swung 7.6° rms: 27 m at 200 m.
    /// </summary>
    [Theory]
    [InlineData(ChassisClass.Light)]
    [InlineData(ChassisClass.Medium)]
    [InlineData(ChassisClass.Heavy)]
    [InlineData(ChassisClass.Assault)]
    public void AimStaysSteady_WhileWalkingAndRunning(ChassisClass cls)
    {
        foreach (float throttle in new[] { 0.4f, 1f })
        {
            var sim = new MechSim(MechChassis.Create(cls));
            double sum = 0;
            int n = 0;
            for (int i = 0; i < 120 * 12; i++)
            {
                sim.Step(new MechInput { Throttle = throttle }, 1f / 120f);
                if (i < 120 * 4) continue;
                var b = sim.Locomotion.Body;
                var d = sim.Pose.AimDirection;
                float yaw = Logic.Core.MathUtil.WrapAngle(MathF.Atan2(-d.X, -d.Z) - (b.Psi + b.Twist));
                float pitch = MathF.Asin(d.Y) - b.Aim;
                sum += yaw * yaw + pitch * pitch;
                n++;
            }
            float rms = (float)Math.Sqrt(sum / n);
            Assert.True(rms * 200f < 2.5f, $"{cls} at throttle {throttle}: {rms * 200f:0.0} m rms at 200 m");
        }
    }
}
