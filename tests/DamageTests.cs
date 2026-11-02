using System;
using System.Numerics;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Tests;

public class DamageTests
{
    private const float H = 1f / 120f;

    private static MechSim NewSim(MechDamage? damage = null) => new(new MechChassis(), 1, damage);

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
    public void RaySphere_StaysExact_ForTinySpheresAtRange()
    {
        // A 0.1 m sphere seen from 300 m and 600 m: rays passing 0.12 m off its centre miss, 0.08 m off hit.
        var c = new Vector3(0.3f, 7.2f, -1.4f);
        foreach (float range in new[] { 300f, 600f })
        foreach (float off in new[] { 0.12f, 0.08f })
        {
            var origin = c + new Vector3(off, 0f, -range);
            Assert.Equal(off < 0.1f, HitTest.RaySphere(origin, Vector3.UnitZ, c, 0.1f, out _));
        }
    }

    [Fact]
    public void Cockpit_IsTiny_AndOnlyExposedFromTheFront()
    {
        var sim = NewSim();
        var cockpit = Volume(sim, MechSection.Cockpit);
        Assert.True(cockpit.Radius <= 0.16f);

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

    /// <summary>
    /// Player call: the cockpit kills the pilot in one solid hit, so it must be extremely hard to hit. Aiming at the
    /// torso with 1.5 m scatter (an AI at ~250 m), the share of hits striking it, head-on, from a diving missile and
    /// from the side, must stay under 0.4 % for every class and model. Before (r 0.3 m, canopy proud of the torso)
    /// it was 1.3-3.2 % head-on and up to 3 % from above: one 10-round arc salvo had a ~15 % chance of killing the
    /// pilot, and lights died that way in the first 10 s.
    /// </summary>
    [Fact]
    public void Cockpit_IsExtremelyHardToHit_FromAnyDirection()
    {
        foreach (var cls in Enum.GetValues<ChassisClass>())
        foreach (var model in new[] { MechModel.Bascinet, MechModel.Sicklefoot })
        {
            var sim = new MechSim(MechChassis.Create(cls, model));
            for (int i = 0; i < 240; i++) sim.Step(default, 1f / 120f);
            var rng = new DeterministicRng(7);
            Vector3 aim = Volume(sim, MechSection.Torso).Center;
            foreach (var dir in new[] { Vector3.UnitZ, Vector3.Normalize(new Vector3(0, -1.2f, 1f)), -Vector3.UnitX })
            {
                Vector3 side = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY)), up = Vector3.Cross(side, dir);
                int hits = 0, cockpit = 0;
                for (int i = 0; i < 20000; i++)
                {
                    Vector3 p = aim + side * rng.NextGaussian() * 1.5f + up * rng.NextGaussian() * 1.5f - dir * 300f;
                    if (!sim.Raycast(p, dir, 600f, out var h)) continue;
                    hits++;
                    if (h.Section == MechSection.Cockpit) cockpit++;
                }
                Assert.True(cockpit < 0.004f * hits, $"{cls} {model} from {dir}: {cockpit} of {hits} hits on the cockpit");
            }
        }
    }

    /// <summary>
    /// Cover geometry (player note: shoulder and torso structures that give extra cover): the Bascinet's pauldron
    /// flanges, swept forward beside the head, screen it from the side, so a side shot at the canopy strikes the arm;
    /// head-on the chest is still the torso; with the arm destroyed its plates are gone. Torso-aimed shots (1.5 m
    /// scatter) on the torso: side 78 -> 59 %, at 45° 67 -> 51 % (the arms take the rest). The Sicklefoot has none.
    /// </summary>
    [Fact]
    public void ShoulderPlates_ScreenTheBascinetsCore_FromTheSide()
    {
        var sim = new MechSim(MechChassis.Medium());
        for (int i = 0; i < 240; i++) sim.Step(default, H);
        Assert.Equal(4, sim.Pose.HitBoxes.Length);
        Vector3 canopy = Volume(sim, MechSection.Cockpit).Center;
        Assert.True(sim.Raycast(canopy + new Vector3(100, 0.3f, 0), -Vector3.UnitX, 200f, out var hit));
        Assert.Equal(MechSection.ArmRight, hit.Section); // right side = +X
        Assert.True(sim.Raycast(canopy + new Vector3(-100, 0.3f, 0), Vector3.UnitX, 200f, out hit));
        Assert.Equal(MechSection.ArmLeft, hit.Section);

        Vector3 chest = Volume(sim, MechSection.Torso).Center;
        Assert.True(sim.Raycast(chest + new Vector3(0, 0, -100), Vector3.UnitZ, 200f, out hit));
        Assert.Equal(MechSection.Torso, hit.Section);

        sim.Damage.Structure[(int)MechSection.ArmRight] = 0f;
        Assert.True(sim.Raycast(canopy + new Vector3(100, 0.3f, 0), -Vector3.UnitX, 200f, out hit));
        Assert.NotEqual(MechSection.ArmRight, hit.Section);

        Assert.Empty(new MechSim(MechChassis.Medium(MechModel.Sicklefoot)).Pose.HitBoxes);
    }

    [Fact]
    public void FrontalShots_HitTheExpectedSections()
    {
        var sim = NewSim();
        foreach (var s in new[] { MechSection.Torso, MechSection.ArmLeft, MechSection.ArmRight, MechSection.LegLeft, MechSection.LegRight })
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
        Assert.Equal(sim.Damage.Max(MechSection.Torso), sim.Damage[MechSection.Torso]); // the frame is intact: salvageable
        Assert.True(sim.ApplyHit(MechSection.Torso, 50f).Ignored);
    }

    [Fact]
    public void LaserBurn_MustBeHeldOnTheCockpit()
    {
        // At 280 m (past falloff) the whole burn does ~6.7, enough for a 5 HP cockpit; a 0.25 s glance is not.
        float full = WeaponSpec.Laser.DamageAt(280f), glance = full * 0.25f / WeaponSpec.Laser.BurnTime;
        var sim = NewSim();
        sim.ApplyHit(MechSection.Cockpit, glance);
        Assert.False(sim.IsOut);
        sim.ApplyHit(MechSection.Cockpit, full - glance);
        Assert.True(sim.IsOut);
    }

    [Fact]
    public void DestroyedArm_TakesItsWeaponOffline_AndPassesDamageToItsSideTorso()
    {
        var sim = NewSim();
        var r = sim.ApplyHit(MechSection.ArmRight, 45f);
        Assert.True(r.SectionDestroyed);
        Assert.True(sim.Weapons.Mounts[0].Offline); // cannon, right arm
        Assert.False(sim.Weapons.Mounts[1].Offline);

        sim.Step(new MechInput { FireMask = 0b11 }, H);
        Assert.Equal("Laser", Assert.Single(sim.Shots).Spec.Name);

        r = sim.ApplyHit(MechSection.ArmRight, 10f);
        Assert.Equal(MechSection.TorsoRight, r.Applied);
        Assert.Equal(sim.Damage.Max(MechSection.TorsoRight) - 10f, sim.Damage[MechSection.TorsoRight], 3);
        Assert.Equal(sim.Damage.Max(MechSection.Torso), sim.Damage[MechSection.Torso]);
    }

    [Fact]
    public void ADestroyedLeg_CapsSpeed_ButDoesNotKill()
    {
        var sim = NewSim();
        Assert.True(sim.ApplyHit(MechSection.LegLeft, 70f).SectionDestroyed);
        Assert.False(sim.IsOut);
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
        var chassis = MechChassis.Medium();
        var damage = MechDamage.Create(chassis);
        damage.Apply(MechSection.ArmLeft, 100f);
        damage.Apply(MechSection.Torso, 30f);

        var sim = NewSim(damage);
        Assert.True(sim.Weapons.Mounts[1].Offline); // laser, left arm
        Assert.Equal(MechDamage.CentreShare * chassis.TorsoHp - 30f, sim.Damage[MechSection.Torso], 3);
        Assert.False(sim.IsOut);
    }

    [Fact]
    public void TorsoHitsFromBehind_StrikeTheThinRearPlate()
    {
        // The mech faces -Z: a shot travelling -Z came from behind, +Z from the front, ±X from a side.
        var d0 = MechDamage.Create(new MechChassis());
        foreach (var s in new[] { MechSection.Torso, MechSection.TorsoLeft, MechSection.TorsoRight })
            Assert.True(d0.MaxRearArmor[(int)s] > 0f && d0.MaxRearArmor[(int)s] < 0.35f * d0.MaxArmor[(int)s], $"{s}: the rear plate is thin");
        DamageReport Hit(Vector3 travel, MechSection s = MechSection.Torso) => NewSim().ApplyHit(s, 20f, travel);
        var rear = Hit(-Vector3.UnitZ);
        Assert.Equal(Facing.Rear, rear.Facing);
        Assert.True(rear.Breached, "20 goes through the rear plate");
        Assert.True(rear.ToStructure > 0f);
        foreach (var travel in new[] { Vector3.UnitZ, Vector3.UnitX })
        {
            var front = Hit(travel);
            Assert.Equal(0f, front.ToStructure); // the front plate holds
        }
        Assert.Equal(0f, Hit(-Vector3.UnitZ, MechSection.ArmLeft).ToStructure);  // arms have one plate all round
        Assert.Equal(0f, NewSim().ApplyHit(MechSection.Torso, 20f).ToStructure);  // direction unknown: front
    }

    [Fact]
    public void Facing_FollowsTheTorsoTwist()
    {
        var sim = NewSim();
        Run(sim, new MechInput { TwistTarget = sim.Chassis.TwistMax }, 3f);
        // Twisted left by TwistMax: a shot from straight behind the legs now arrives off the torso's side.
        Assert.NotEqual(Facing.Rear, sim.ApplyHit(MechSection.Torso, 1f, -Vector3.UnitZ).Facing);
        Assert.Equal(Facing.Rear, sim.ApplyHit(MechSection.ArmLeft, 1f, sim.TorsoFacing).Facing);
    }

    [Fact]
    public void Armor_SoaksFirst_ThenTheFragileStructureDegradesTheSection()
    {
        var sim = NewSim();
        var d = sim.Damage;
        float legArmor = d.Armor[(int)MechSection.LegLeft];
        var r = sim.ApplyHit(MechSection.LegLeft, legArmor - 1f);
        Assert.Equal(0f, r.ToStructure);
        Assert.Equal(1f, sim.Locomotion.SpeedFactor); // armour gone almost entirely, legs still fine
        sim.ApplyHit(MechSection.LegLeft, 1f + 0.5f * d.MaxStructure[(int)MechSection.LegLeft]);
        Assert.True(d.Exposed(MechSection.LegLeft));
        Assert.InRange(sim.Locomotion.SpeedFactor, sim.Chassis.CrippledSpeedFactor + 0.01f, 0.99f); // limping

        sim.ApplyHit(MechSection.ArmRight, d.Armor[(int)MechSection.ArmRight] + 0.5f * d.MaxStructure[(int)MechSection.ArmRight]);
        Assert.InRange(sim.Weapons.Mounts[0].CycleFactor, 1.3f, 1.7f); // its gun cycles slower

        sim.ApplyHit(MechSection.Torso, d.Armor[(int)MechSection.Torso] + 0.5f * d.MaxStructure[(int)MechSection.Torso]);
        Assert.InRange(sim.Weapons.DissipationFactor, 0.5f, 0.9f); // heat sinks damaged

        sim.ApplyHit(MechSection.Cockpit, d.Armor[(int)MechSection.Cockpit] + 0.5f * d.MaxStructure[(int)MechSection.Cockpit]);
        Assert.InRange(sim.SensorQuality, 0.5f, 0.9f); // sensors blurred
    }

    [Fact]
    public void AWeapon_CanBeWeakAgainstArmour_AndDeadlyAgainstStructure()
    {
        var d = MechDamage.Create(new MechChassis());
        var r = d.Apply(MechSection.ArmLeft, 10f, Facing.Front, vsArmor: 0.1f, vsStructure: 2.5f);
        Assert.Equal(1f, r.Amount, 3);                 // barely scratches the plate
        d.Armor[(int)MechSection.ArmLeft] = 0f;
        r = d.Apply(MechSection.ArmLeft, 4f, Facing.Front, vsArmor: 0.1f, vsStructure: 2.5f);
        Assert.Equal(10f, r.ToStructure, 3);           // shreds what is underneath
    }

    [Fact]
    public void Hits_ShakeTheTorso()
    {
        var sim = NewSim();
        sim.ApplyHit(MechSection.Torso, 14f);
        Assert.True(sim.Locomotion.Secondary.Trauma > 0.4f);
    }
}
