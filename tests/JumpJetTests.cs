using System;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using Xunit.Abstractions;

namespace IronFeud.Tests;

public class JumpJetTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public JumpJetTests(ITestOutputHelper output) => _out = output;

    /// <summary>Holds the jets for <paramref name="burn"/> seconds, then lets go; returns apex height and touchdown speed.</summary>
    private static (float Apex, float Landing, int Footfalls, float AirTime) Jump(MechSim sim, float burn, float throttle = 0f)
    {
        var loco = sim.Locomotion;
        float startY = loco.Body.Position.Y, apex = 0f, landing = 0f, air = 0f;
        int footfalls = 0;
        for (int i = 0; i < 120 * 8; i++)
        {
            sim.Step(new MechInput { Throttle = throttle, Jump = i * H < burn }, H);
            if (loco.Body.Airborne) air += H;
            apex = MathF.Max(apex, loco.Body.Position.Y - startY);
            if (loco.LandingSpeed > 0f)
            {
                landing = loco.LandingSpeed;
                footfalls = loco.Footfalls.Count;
                break;
            }
        }
        return (apex, landing, footfalls, air);
    }

    [Theory]
    [InlineData(ChassisClass.Light, 20f, 26f)]
    [InlineData(ChassisClass.Medium, 10f, 16f)]
    public void FullBurn_ReachesItsClassHeight_AndLandsHard(ChassisClass cls, float minApex, float maxApex)
    {
        var sim = new MechSim(MechChassis.Create(cls));
        var (apex, landing, footfalls, air) = Jump(sim, 5f);
        _out.WriteLine($"{cls}: apex {apex:0.0} m, {air:0.0} s in the air, touchdown {landing:0.0} m/s");
        Assert.InRange(apex, minApex, maxApex);
        Assert.False(sim.Locomotion.Body.Airborne);
        Assert.Equal(2, footfalls);
        Assert.True(landing > 10f);
        Assert.Equal(0f, sim.Locomotion.JumpFuel, 3);
    }

    [Theory]
    [InlineData(ChassisClass.Heavy)]
    [InlineData(ChassisClass.Assault)]
    public void HeaviesAndAssaults_HaveNoJets(ChassisClass cls)
    {
        var sim = new MechSim(MechChassis.Create(cls));
        for (int i = 0; i < 240; i++) sim.Step(new MechInput { Jump = true }, H);
        Assert.False(sim.Locomotion.Body.Airborne);
    }

    [Fact]
    public void AnEmptyTank_MustRechargeAQuarterBeforeTheNextJump()
    {
        var sim = new MechSim(MechChassis.Light());
        Jump(sim, 5f);
        sim.Step(new MechInput { Jump = true }, H);
        Assert.False(sim.Locomotion.Body.Airborne, "no fuel, no jump");
        float wait = MechLocomotion.JumpMinCharge * sim.Chassis.JumpRecharge;
        for (int i = 0; i < (int)(wait / H) + 2; i++) sim.Step(default, H);
        sim.Step(new MechInput { Jump = true }, H);
        Assert.True(sim.Locomotion.Body.Airborne);
    }

    [Fact]
    public void Burning_AddsHeat()
    {
        // At the end of a 1 s burn, against a twin that stood still meanwhile (heat sinks work for both).
        MechSim jumper = new(MechChassis.Medium()), idle = new(MechChassis.Medium());
        for (int i = 0; i < 120; i++)
        {
            jumper.Step(new MechInput { Jump = true }, H);
            idle.Step(default, H);
        }
        // The idle twin bottoms out at the heat floor, so it sheds less than the jumper: ask for half the burn heat.
        Assert.True(jumper.Weapons.Heat > idle.Weapons.Heat + 0.5f * jumper.Chassis.JumpHeat, $"jumper {jumper.Weapons.Heat:0.000} idle {idle.Weapons.Heat:0.000} burning {jumper.Locomotion.JetsBurning} air {jumper.Locomotion.Body.Airborne}");
    }

    [Fact]
    public void ARunningJump_CarriesItsMomentum()
    {
        var sim = new MechSim(MechChassis.Light());
        for (int i = 0; i < 120 * 5; i++) sim.Step(new MechInput { Throttle = 1f }, H);
        float v = sim.Locomotion.Body.V, z0 = sim.Locomotion.Body.Position.Z;
        var (_, _, _, air) = Jump(sim, 0.6f, throttle: 1f);
        float travelled = z0 - sim.Locomotion.Body.Position.Z;
        _out.WriteLine($"{v:0} m/s run, {air:0.00} s jump: {travelled:0} m");
        Assert.True(travelled > 0.85f * v * air, $"travelled {travelled:0} m in {air:0.00} s at {v:0} m/s");
    }

    /// <summary>
    /// Jumping repeatedly while running across No Man's Land (craters, trenches, ridges): the legs stay natural in
    /// the air and on every landing, and the pelvis never teleports. Feet may dip into a crest briefly right after a
    /// touchdown while the next stride catches up (measured when added: Bascinet worst 1.3 m in 0.5 % of foot-ticks;
    /// the Sicklefoot already dips up to 1.6 m when only walking here, its toe joint on steep faces).
    /// </summary>
    [Theory]
    [InlineData(MechModel.Bascinet)]
    [InlineData(MechModel.Sicklefoot)]
    [InlineData(MechModel.Bodkin)]
    [InlineData(MechModel.Brigandine)]
    public void JumpingAcrossNoMansLand_PosesStayNatural(MechModel model)
    {
        var map = ArenaMap.NoMansLand(3);
        var c = TerrainTests.PoseTestChassis(model);
        var sim = new MechSim(c);
        var m = sim.Locomotion;
        m.Ground = map;
        m.Reset(new Vector3(-60, 0, 190), 0f);
        float maxRoll = 0, maxKnee = 0, maxPelvisSpeed = 0, worstSink = 0, maxReach = 0;
        int footTicks = 0, sunk = 0;
        Vector3 prevPelvis = m.Pose.PelvisPosition;
        int jumps = 0, landings = 0;
        for (int i = 0; i < 120 * 40 && m.Body.Position.Z > -190f; i++)
        {
            // Burn 0.8 s out of every 3 s, while running and weaving.
            bool jump = i % 360 < 96, wasAirborne = m.Body.Airborne;
            sim.Step(new MechInput { Throttle = 1f, Turn = 0.15f * MathF.Sin(i / 300f), Jump = jump }, H);
            if (!wasAirborne && m.Body.Airborne) jumps++;
            if (wasAirborne && !m.Body.Airborne) landings++;
            var p = m.Pose;
            maxPelvisSpeed = MathF.Max(maxPelvisSpeed, Vector3.Distance(p.PelvisPosition, prevPelvis) / H);
            prevPelvis = p.PelvisPosition;
            for (int l = 0; l < 2; l++)
            {
                var lp = p.Legs[l];
                Assert.False(float.IsNaN(lp.AnkleWorld.X) || float.IsNaN(lp.Angles.Knee), "NaN in the legs");
                maxRoll = MathF.Max(maxRoll, MathF.Abs(lp.Angles.Roll));
                maxKnee = MathF.Max(maxKnee, -lp.Angles.Knee);
                maxReach = MathF.Max(maxReach, Vector3.Distance(lp.HipWorld, lp.AnkleWorld) / c.LegLength);
                float ground = map.Height(lp.AnkleWorld.X, lp.AnkleWorld.Z);
                float sink = ground + 0.5f * c.AnkleHeight - lp.AnkleWorld.Y;
                worstSink = MathF.Max(worstSink, sink);
                footTicks++;
                if (sink > 0.5f) sunk++;
            }
        }
        _out.WriteLine($"{model}: {jumps} jumps, {landings} landings; hip roll ≤ {maxRoll:0.00}, knee ≤ {maxKnee:0.00} rad, " +
                       $"reach ≤ {maxReach:0.00} L, ankle sinks ≤ {worstSink:0.00} m ({100f * sunk / footTicks:0.0} % of foot-ticks), pelvis ≤ {maxPelvisSpeed:0} m/s");
        Assert.True(landings >= jumps - 1, "every jump comes down");
        Assert.True(maxRoll < 0.36f, $"hip roll {maxRoll:0.00} rad");
        Assert.True(maxKnee < 2.5f, $"knee folds to {maxKnee:0.00} rad");
        Assert.True(maxReach < 1.0f, $"leg over-extended to {maxReach:0.00} L");
        Assert.True(worstSink < 2.5f, $"an ankle went {worstSink:0.00} m into the ground");
        Assert.True(sunk < 0.02f * footTicks, $"feet in the ground {100f * sunk / footTicks:0.0} % of the time");
        Assert.True(maxPelvisSpeed < 45f, $"pelvis moved at {maxPelvisSpeed:0} m/s");
    }
}
