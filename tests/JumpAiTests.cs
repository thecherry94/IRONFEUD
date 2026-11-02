using System;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using Vector2 = System.Numerics.Vector2;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>
/// The AI's jump-jet tactics, each in a scene built to need it. The statistical payoff (lone light vs assault, medium
/// vs heavy on No Man's Land) is in the opt-in <see cref="BalanceTests"/> report.
/// </summary>
public class JumpAiTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public JumpAiTests(ITestOutputHelper output) => _out = output;

    private static MechDamage Tough(MechChassis c)
    {
        var d = MechDamage.Create(c);
        foreach (var s in Enum.GetValues<MechSection>())
            d.Set(s, d.MaxArmor[(int)s], 1e6f);
        return d;
    }

    /// <summary>
    /// A medium 10 m behind a 14 m berm too steep to walk, its target 170 m beyond, seen moments ago: it pops up,
    /// fires over the crest and lands again. Without pop-ups it can't hit anything from there.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HiddenMedium_PopsUpOverTheCrest_AndHits(bool popUps)
    {
        var f = new HeightField(151, 4f).AddRidge(new Vector2(-300f, 20f), new Vector2(300f, 20f), 15f, 14f, 1.5f);
        f.UpdateBounds();
        var b = new BattleSim(new ArenaMap { Terrain = f, Radius = 280f }, 1)
        {
            TrackOutcome = false, JumpTactics = popUps ? JumpTactics.PopUp : JumpTactics.None,
        };
        var m = b.AddUnit("M", 0, MechChassis.Medium(), new Vector3(0, 0, 45), 0f, Personality.Balanced);
        var heavy = MechChassis.Heavy();
        var target = b.AddUnit("H", 1, heavy, new Vector3(0, 0, -150), MathF.PI, damage: Tough(heavy));
        m.Ai!.Contacts[target] = new Contact { Position = target.Position, LastSeen = 0f, FirstSeen = -5f };

        bool popped = false;
        for (int i = 0; i < 120 * 6; i++)
        {
            b.Step(H);
            popped |= m.Ai.Jump == JumpPlan.PopUp && m.Sim.Locomotion.Body.Airborne;
        }
        _out.WriteLine($"pop-ups {(popUps ? "on" : "off")}: popped {popped}, {m.Stats.Hits}/{m.Stats.Shots} hits; last: {string.Join(" | ", m.Ai.Log)}");
        if (popUps)
        {
            Assert.True(popped, "it should pop up");
            Assert.True(m.Stats.Hits > 0, "and hit from the top of the jump");
        }
        else Assert.Equal(0, m.Stats.Hits);
    }

    /// <summary>
    /// A lone light head-on to an assault on open ground vaults it: it passes overhead (within 15 m, above its torso)
    /// and yaws round in the air to come down facing it.
    /// </summary>
    [Fact]
    public void LoneLight_VaultsAnAssault_AndLandsFacingIt()
    {
        var b = new BattleSim(new ArenaMap { Radius = 400f }, 3) { TrackOutcome = false };
        // Unlimited heat sinking: this checks the vault, not heat management (a light that fights runs too hot to add
        // jet heat most of the time, which is right but would make this scene a lottery).
        var lightChassis = MechChassis.Light();
        lightChassis.HeatDissipation = 10f;
        // No heat weapons either: incendiary rounds can shut a lone assault down, and a light behind a stalled
        // assault has no reason to vault.
        lightChassis.Loadout[0] = new MountDef(WeaponSpec.Autocannon, Hardpoint.RightArm);
        var light = b.AddUnit("L", 0, lightChassis, new Vector3(0, 0, 160), 0f, Personality.Balanced, Tough(lightChassis));
        var assaultChassis = MechChassis.Assault();
        var assault = b.AddUnit("A", 1, assaultChassis, new Vector3(0, 0, -160), MathF.PI, Personality.Balanced, Tough(assaultChassis));
        float closest = float.MaxValue, facingAtLanding = float.NaN;
        bool wasAir = false;
        for (int i = 0; i < 120 * 40 && float.IsNaN(facingAtLanding); i++)
        {
            b.Step(H);
            bool air = light.Sim.Locomotion.Body.Airborne;
            if (air && light.Ai!.Jump == JumpPlan.Vault)
            {
                var torso = assault.Volume(MechSection.Torso);
                if (light.Position.Y > torso.Center.Y) closest = MathF.Min(closest, MathUtil.PlanarDistance(light.Position, assault.Position));
            }
            if (wasAir && !air && closest < float.MaxValue)
                facingAtLanding = MathUtil.WrapAngle(MathUtil.Heading(assault.Position - light.Position) - light.Sim.Locomotion.Body.Psi);
            wasAir = air;
        }
        _out.WriteLine($"passed {closest:0} m from the assault overhead; landed {facingAtLanding:0.00} rad off facing it");
        Assert.True(closest < 15f, $"closest pass overhead {closest:0} m");
        Assert.True(MathF.Abs(facingAtLanding) < 1.2f, $"landed {facingAtLanding:0.00} rad off facing the assault");
    }

    /// <summary>A siege shell on course for a jump-capable medium's torso, ~1.2 s out: it hops and the shell misses.</summary>
    [Fact]
    public void Medium_HopsOverAnIncomingSiegeShell()
    {
        var b = new BattleSim(new ArenaMap { Radius = 400f }, 5) { TrackOutcome = false };
        var m = b.AddUnit("M", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Balanced);
        var gunner = b.AddUnit("G", 1, MechChassis.Assault(), new Vector3(300, 0, 0), MathF.PI / 2f);
        b.Step(H);
        Vector3 from = new(290f, m.Volume(MechSection.Torso).Center.Y, 0f);
        var shot = new ShotFired { Spec = WeaponSpec.SiegeCannon, Origin = from, Direction = Vector3.Normalize(m.Volume(MechSection.Torso).Center - from) };
        var state = Projectile.From(shot);
        state.Velocity -= Vector3.UnitY * 0f;
        b.Shells.Add(new Shell { Id = 999, State = state, Owner = gunner });
        bool dodged = false;
        for (int i = 0; i < 120 * 3; i++)
        {
            b.Step(H);
            dodged |= m.Ai!.Jump == JumpPlan.Dodge;
        }
        _out.WriteLine($"dodge {dodged}, torso {m.Sim.Damage[MechSection.Torso]:0}/{m.Sim.Damage.Max(MechSection.Torso):0}");
        Assert.True(dodged, "it should hop");
        Assert.Equal(m.Sim.Damage.Max(MechSection.Torso), m.Sim.Damage[MechSection.Torso]);
    }
}
