using System;
using System.Numerics;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>Heat between the cool range and the cliff: own guns can't scram the reactor, and running hot costs speed and aim.</summary>
public class HeatTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public HeatTests(ITestOutputHelper output) => _out = output;

    /// <summary>A particle bolt fired at heat 0.99 used to push its own mech to 1.31: shut down by its own gun.</summary>
    [Fact]
    public void OwnGuns_NeverShutTheirMechDown()
    {
        var c = MechChassis.Create(ChassisClass.Medium, MechModel.Brigandine);
        var sim = new MechSim(c);
        sim.Weapons.Heat = 0.99f;
        sim.Step(new MechInput { FireMask = 0b111u }, H);
        float peak = sim.Weapons.Heat;
        for (int i = 0; i < 120; i++) sim.Step(default, H);
        _out.WriteLine($"fired everything at 0.99: heat {peak:0.00}, shut down {sim.Shutdown}");
        Assert.True(peak <= MechWeapons.OwnHeatCap + 1e-4f);
        Assert.False(sim.Shutdown);
    }

    /// <summary>Held at heat 0.95 (deep in the running-hot band) a medium runs ~14 % slower and its aim sways more.</summary>
    [Fact]
    public void RunningHot_SlowsTheLegs_AndUnsteadiesTheAim()
    {
        var (coolSpeed, coolSway) = Run(0.3f);
        var (hotSpeed, hotSway) = Run(0.95f);
        _out.WriteLine($"cool: {coolSpeed:0.0} m/s, aim sway {coolSway:0.000} rad · hot: {hotSpeed:0.0} m/s, aim sway {hotSway:0.000} rad");
        Assert.InRange(hotSpeed / coolSpeed, 0.8f, 0.9f);
        Assert.True(hotSway > 2f * coolSway, "the gyro steadies the guns less");
    }

    private static (float Speed, float Sway) Run(float heat)
    {
        var sim = new MechSim(MechChassis.Medium());
        float sway = 0f;
        for (int i = 0; i < 120 * 12; i++)
        {
            sim.Weapons.Heat = heat;
            sim.Step(new MechInput { Throttle = 1f }, H);
            if (i < 120 * 6) continue;
            var body = sim.Locomotion.Body;
            var d = sim.Pose.AimDirection;
            sway = MathF.Max(sway, MathF.Abs(MathUtil.WrapAngle(MathF.Atan2(-d.X, -d.Z) - body.Psi - body.Twist)));
        }
        return (sim.Locomotion.Body.V, sway);
    }
}
