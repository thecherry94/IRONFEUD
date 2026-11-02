using System;
using System.Numerics;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>Stability: heavy shells stagger, lasers barely rock; a stagger silences the guns briefly and can't chain.</summary>
public class StabilityTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public StabilityTests(ITestOutputHelper output) => _out = output;

    private static MechSim Tough(MechChassis c)
    {
        var d = MechDamage.Create(c);
        foreach (var s in Enum.GetValues<MechSection>())
            d.Set(s, d.MaxArmor[(int)s], 1e6f);
        return new MechSim(c, damage: d);
    }

    [Fact]
    public void HeavyShells_StaggerAMedium_LasersDont()
    {
        var shelled = Tough(MechChassis.Medium());
        int shells = 0;
        while (!shelled.Staggered && shells < 10) { shelled.ApplyHit(MechSection.Torso, WeaponSpec.HeavyCannon.Damage, -Vector3.UnitZ * -1f, WeaponSpec.HeavyCannon); shells++; }
        var lasered = Tough(MechChassis.Medium());
        for (int i = 0; i < 10; i++) lasered.ApplyHit(MechSection.Torso, WeaponSpec.Laser.Damage, null, WeaponSpec.Laser);
        _out.WriteLine($"medium: staggered by heavy-cannon hit #{shells}; ten laser hits leave it at {100f * (1f - lasered.Instability):0}% stability");
        Assert.Equal(3, shells);
        Assert.False(lasered.Staggered);
        Assert.True(lasered.Instability < 0.5f);
    }

    [Fact]
    public void Stagger_SilencesTheGuns_ThenCannotChain()
    {
        var m = Tough(MechChassis.Medium());
        m.ApplyHit(MechSection.Torso, 200f, null, WeaponSpec.SiegeCannon);
        Assert.True(m.Staggered);
        int fired = 0;
        for (int i = 0; i < (int)(MechSim.StaggerTime * 120) - 2; i++)
        {
            m.Step(new MechInput { FireMask = 0b111u }, H);
            fired += m.Shots.Count;
        }
        Assert.Equal(0, fired);
        for (int i = 0; i < 10; i++) m.Step(default, H);
        Assert.False(m.Staggered);
        // Within the grace a second siege shell drains it again but cannot stagger it.
        m.ApplyHit(MechSection.Torso, 200f, null, WeaponSpec.SiegeCannon);
        Assert.False(m.Staggered);
        Assert.Equal(1, m.Staggers);
        // It recovers once the shelling stops.
        for (int i = 0; i < 120 * 6; i++) m.Step(default, H);
        _out.WriteLine($"after 6 s quiet: stability {100f * (1f - m.Instability):0}%");
        Assert.True(m.Instability < 0.05f);
    }

    [Fact]
    public void BiggerAndSquatterChassis_TakeMoreToStagger()
    {
        float Budget(MechModel model) => MechChassis.Design(model).StabilityMax;
        _out.WriteLine($"stability: Bodkin {Budget(MechModel.Bodkin)}, Bascinet {Budget(MechModel.Bascinet)}, Harrow {Budget(MechModel.Harrow)}, Barbican {Budget(MechModel.Barbican)}");
        Assert.True(Budget(MechModel.Bodkin) < Budget(MechModel.Bascinet));
        Assert.True(Budget(MechModel.Bascinet) < Budget(MechModel.Harrow));
        Assert.True(Budget(MechModel.Harrow) < Budget(MechModel.Barbican));
    }
}
