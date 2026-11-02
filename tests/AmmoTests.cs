using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Tests;

/// <summary>Ammunition: guns and missile racks run dry, energy weapons don't; stored rounds cook off with their section.</summary>
public class AmmoTests
{
    private const float H = 1f / 120f;

    [Fact]
    public void Guns_RunDry_EnergyWeaponsDont()
    {
        var sim = new MechSim(new MechChassis { HeatDissipation = 10f }); // cannon right (bit 0), laser left (bit 1); heat no object
        var cannon = sim.Weapons.Mounts[0];
        var laser = sim.Weapons.Mounts[1];
        Assert.Equal(WeaponSpec.Cannon.Rounds, cannon.Ammo);
        Assert.Equal(-1, laser.Ammo);
        cannon.Ammo = 2;
        int fired = 0;
        for (int i = 0; i < 120 * 6; i++)
        {
            sim.Step(new MechInput { FireMask = 0b01u }, H);
            foreach (var s in sim.Shots) if (s.Weapon == 0) fired++;
        }
        Assert.Equal(2, fired);
        Assert.Equal(0, cannon.Ammo);
        Assert.True(cannon.Offline);
        Assert.False(sim.Weapons.CanFire(0));
        Assert.False(laser.Offline);
    }

    [Fact]
    public void AnArmWeaponsRounds_SitInItsSideTorso_AndCookOffWithIt()
    {
        var sim = new MechSim(new MechChassis());
        var cannon = sim.Weapons.Mounts[0]; // right arm
        Assert.Equal(MechSection.TorsoRight, cannon.AmmoSection);

        // Losing the arm alone: the rounds are safe in the torso (and the weapon is gone).
        var r = sim.ApplyHit(MechSection.ArmRight, 1000f);
        Assert.True(r.SectionDestroyed);
        Assert.Equal(0f, r.CookOff);

        // Losing the side torso: what is left in the bin goes off inside the centre torso.
        cannon.Ammo = 10;
        float centre = sim.Damage.Structure[(int)MechSection.Torso];
        r = sim.ApplyHit(MechSection.TorsoRight, 1000f);
        Assert.True(r.SectionDestroyed);
        float blast = MechSim.CookOffShare * 10 * WeaponSpec.Cannon.Damage;
        Assert.Equal(blast, r.CookOff, 3);
        Assert.Equal(System.MathF.Max(0f, centre - blast), sim.Damage.Structure[(int)MechSection.Torso], 3);
        Assert.Equal(0, cannon.Ammo);
    }

    [Fact]
    public void AFullBin_CookingOff_CanDestroyTheMech()
    {
        var sim = new MechSim(MechChassis.Design(MechModel.Harrow)); // heavy cannon, full bin, right arm
        var r = sim.ApplyHit(MechSection.TorsoRight, 10000f);
        Assert.True(r.CookOff > sim.Damage.MaxStructure[(int)MechSection.Torso]);
        Assert.Equal(MechStatus.Destroyed, r.StatusAfter);
        Assert.True(sim.IsOut);
    }
}
