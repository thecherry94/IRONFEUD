using System.Collections.Generic;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Tests;

public class WeaponGroupTests
{
    private const float H = 1f / 120f;

    [Fact]
    public void Defaults_LongRange_ShortRange_Missiles()
    {
        var sim = new MechSim(MechChassis.Medium()); // heavy cannon, laser, strike rack, small laser, machine gun
        var g = WeaponGroups.Default(sim.Weapons);
        Assert.Equal(0b00001u, g.Members[0]);
        Assert.Equal(0b11010u, g.Members[1]);
        Assert.Equal(0b00100u, g.Members[2]);
    }

    [Fact]
    public void Defaults_NeverLeaveTheFirstGroupEmpty()
    {
        var c = new MechChassis { Loadout = [new(WeaponSpec.Flamethrower, Hardpoint.RightArm), new(WeaponSpec.MachineGun, Hardpoint.LeftArm)] };
        var g = WeaponGroups.Default(new MechSim(c).Weapons);
        Assert.Equal(0b11u, g.Members[0]);
        Assert.Equal(0u, g.Members[1]);
    }

    [Fact]
    public void Alpha_RequestsEveryMember_Chain_RipplesThemABeatApart()
    {
        var c = new MechChassis { Loadout = [new(WeaponSpec.Cannon, Hardpoint.RightArm), new(WeaponSpec.Cannon, Hardpoint.LeftArm)], HeatDissipation = 10f };
        var sim = new MechSim(c);
        var g = new WeaponGroups();
        g.Members[0] = 0b11u;
        Assert.Equal(0b11u, g.FireMask(0b1u, sim.Weapons, H));

        g.ToggleChain(0);
        var fired = new List<(float T, int Weapon)>();
        for (int i = 0; i < 120; i++)
        {
            sim.Step(new MechInput { FireMask = g.FireMask(0b1u, sim.Weapons, H) }, H);
            foreach (var s in sim.Shots) fired.Add((i * H, s.Weapon));
        }
        // Cannons cycle in 0.75 s: right, left a beat later, then right again once it has reloaded.
        Assert.True(fired.Count >= 3);
        Assert.Equal(0, fired[0].Weapon);
        Assert.Equal(1, fired[1].Weapon);
        Assert.InRange(fired[1].T - fired[0].T, WeaponGroups.ChainBeat - 0.01f, WeaponGroups.ChainBeat + 0.02f);
        Assert.Equal(0, fired[2].Weapon);
    }

    [Fact]
    public void Toggle_MovesAWeaponInAndOutOfAGroup()
    {
        var g = new WeaponGroups();
        g.Toggle(3, 2);
        Assert.True(g.Contains(3, 2));
        g.Toggle(3, 2);
        Assert.False(g.Contains(3, 2));
    }
}
