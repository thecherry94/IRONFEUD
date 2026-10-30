using System;
using System.Numerics;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Tests;

public class DamageTests
{
    private const float H = 1f / 120f;

    private static MechSim NewSim(MechDamage? damage = null) => new(new MechTuning(), 1, damage);

    private static void Run(MechSim sim, MechInput input, float seconds)
    {
        for (int i = 0; i < (int)(seconds / H); i++) sim.Step(input, H);
    }

    private static HitVolume Volume(MechSim sim, MechSection s) => sim.Pose.HitVolumes[(int)s];

    [Fact]
    public void RaySphere_EntryDistance()
    {
        Assert.True(HitTest.RaySphere(Vector3.Zero, -Vector3.UnitZ, new Vector3(0, 0, -10), 2f, out float t));
        Assert.Equal(8f, t, 4);
        Assert.False(HitTest.RaySphere(Vector3.Zero, Vector3.UnitZ, new Vector3(0, 0, -10), 2f, out _));
        Assert.False(HitTest.RaySphere(Vector3.Zero, -Vector3.UnitZ, new Vector3(3, 0, -10), 2f, out _));
    }

    [Fact]
    public void Cockpit_IsTiny_AndOnlyExposedFromTheFront()
    {
        var sim = NewSim();
        var cockpit = Volume(sim, MechSection.Cockpit);
        Assert.True(cockpit.Radius <= 0.3f);

        // From the front (the mech faces -Z): aimed at the canopy, the cockpit is struck.
        Vector3 front = cockpit.Center + new Vector3(0, 0, -100);
        Assert.True(sim.Raycast(front, Vector3.UnitZ, 200f, out var hit));
        Assert.Equal(MechSection.Cockpit, hit.Section);

        // From behind along the same line, the torso absorbs it first.
        Vector3 back = cockpit.Center + new Vector3(0, 0, 100);
        Assert.True(sim.Raycast(back, -Vector3.UnitZ, 200f, out hit));
        Assert.Equal(MechSection.Torso, hit.Section);

        // Half a metre off the canopy centre is no longer a cockpit hit.
        Assert.True(sim.Raycast(front + new Vector3(0.5f, 0, 0), Vector3.UnitZ, 200f, out hit));
        Assert.NotEqual(MechSection.Cockpit, hit.Section);
    }

    [Fact]
    public void FrontalShots_HitTheExpectedSections()
    {
        var sim = NewSim();
        foreach (var s in new[] { MechSection.Torso, MechSection.ArmLeft, MechSection.ArmRight, MechSection.Legs })
        {
            var v = Volume(sim, s);
            Assert.True(sim.Raycast(v.Center + new Vector3(0, 0, -100), Vector3.UnitZ, 200f, out var hit));
            Assert.Equal(s, hit.Section);
        }
        Assert.True(Volume(sim, MechSection.ArmLeft).Center.X < 0f);
        Assert.True(Volume(sim, MechSection.ArmRight).Center.X > 0f);
    }

    [Fact]
    public void OneCannonHitToTheCockpit_KillsThePilot()
    {
        var sim = NewSim();
        var r = sim.ApplyHit(MechSection.Cockpit, WeaponSpec.Cannon.Damage);
        Assert.True(r.SectionDestroyed);
        Assert.Equal(MechStatus.PilotKilled, r.StatusAfter);
        Assert.True(sim.IsOut);
        Assert.Equal(100f, sim.Damage[MechSection.Torso]); // the frame is intact: salvageable
        Assert.True(sim.ApplyHit(MechSection.Torso, 50f).Ignored);
    }

    [Fact]
    public void LaserPastFalloff_NeedsTwoCockpitHits()
    {
        // One laser hit kills the pilot only inside ~127 m (damage >= 5); at 140 m it does 3.
        var sim = NewSim();
        sim.ApplyHit(MechSection.Cockpit, WeaponSpec.Laser.DamageAt(140f));
        Assert.False(sim.IsOut);
        sim.ApplyHit(MechSection.Cockpit, WeaponSpec.Laser.DamageAt(140f));
        Assert.True(sim.IsOut);
    }

    [Fact]
    public void DestroyedArm_TakesItsWeaponOffline_AndPassesDamageToTorso()
    {
        var sim = NewSim();
        var r = sim.ApplyHit(MechSection.ArmRight, 45f);
        Assert.True(r.SectionDestroyed);
        Assert.True(sim.Weapons.Mounts[0].Offline); // cannon, right arm
        Assert.False(sim.Weapons.Mounts[1].Offline);

        sim.Step(new MechInput { FireMask = 0b11 }, H);
        Assert.Equal("Laser", Assert.Single(sim.Shots).Spec.Name);

        r = sim.ApplyHit(MechSection.ArmRight, 10f);
        Assert.Equal(MechSection.Torso, r.Applied);
        Assert.Equal(90f, sim.Damage[MechSection.Torso]);
    }

    [Fact]
    public void DestroyedLegs_CapSpeed()
    {
        var sim = NewSim();
        sim.ApplyHit(MechSection.Legs, 70f);
        Run(sim, new MechInput { Throttle = 1f }, 6f);
        Assert.Equal(4.2f * 0.45f, sim.Locomotion.Body.V, 3);
    }

    [Fact]
    public void TorsoDestroyed_Collapses_WithoutSlidingFeet_AndStopsFiring()
    {
        var sim = NewSim();
        Run(sim, new MechInput { Throttle = 1f }, 4f);
        float standingY = sim.Pose.PelvisPosition.Y;
        var r = sim.ApplyHit(MechSection.Torso, 100f);
        Assert.Equal(MechStatus.Destroyed, r.StatusAfter);

        var plants = new[] { sim.Locomotion.Gait.Legs[0].Plant.Position, sim.Locomotion.Gait.Legs[1].Plant.Position };
        Vector3 bodyAt = sim.Locomotion.Body.Position;
        Run(sim, new MechInput { Throttle = 1f, FireMask = 0b11 }, 3f);

        Assert.True(sim.Pose.PelvisPosition.Y < standingY - 2f);
        Assert.Equal(bodyAt, sim.Locomotion.Body.Position);
        Assert.Equal(plants[0], sim.Locomotion.Gait.Legs[0].Plant.Position);
        Assert.Equal(plants[1], sim.Locomotion.Gait.Legs[1].Plant.Position);
        Assert.Empty(sim.Shots);
        foreach (var leg in sim.Pose.Legs)
            Assert.True(Vector3.Distance(leg.Target.Ankle, leg.AnkleWorld) < 0.01f, "IK still reaches the planted feet");
    }

    [Fact]
    public void Damage_PersistsIntoANewSim()
    {
        var damage = MechDamage.CreateDefault();
        damage.Apply(MechSection.ArmLeft, 100f);
        damage.Apply(MechSection.Torso, 30f);

        var sim = NewSim(damage);
        Assert.True(sim.Weapons.Mounts[1].Offline); // laser, left arm
        Assert.Equal(70f, sim.Damage[MechSection.Torso]);
        Assert.False(sim.IsOut);
    }

    [Fact]
    public void Hits_ShakeTheTorso()
    {
        var sim = NewSim();
        sim.ApplyHit(MechSection.Torso, 14f);
        Assert.True(sim.Locomotion.Secondary.Trauma > 0.4f);
    }
}
