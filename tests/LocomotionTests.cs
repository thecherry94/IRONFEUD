using System;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Tests;

public class LocomotionTests
{
    private const float H = 1f / 120f;

    private static MechLocomotion Run(MechInput input, float seconds, MechLocomotion? m = null, Action<MechLocomotion>? perTick = null)
    {
        m ??= new MechLocomotion(new MechTuning());
        int n = (int)MathF.Round(seconds / H);
        for (int i = 0; i < n; i++)
        {
            m.Step(input, H);
            perTick?.Invoke(m);
        }
        return m;
    }

    [Fact]
    public void FullThrottle_WalksAlongMinusZ_AtTopSpeed()
    {
        var m = Run(new MechInput { Throttle = 1f }, 6f);
        Assert.Equal(4.2f, m.Body.V, 3);
        Assert.True(m.Body.Position.Z < -10f);
        Assert.True(MathF.Abs(m.Body.Position.X) < 1e-3f);
    }

    [Fact]
    public void PositiveTurn_TurnsLeftTowardMinusX()
    {
        var m = Run(new MechInput { Throttle = 1f, Turn = 1f }, 4f);
        Assert.True(m.Body.Psi > 0f);
        Assert.True(m.Body.Position.X < 0f);
    }

    [Fact]
    public void TwistAndAim_AreClampedAndRateLimited()
    {
        var m = new MechLocomotion(new MechTuning());
        m.Step(new MechInput { TwistTarget = 10f, AimTarget = 10f }, H);
        Assert.Equal(1.3f * H, m.Body.Twist, 5);
        Run(new MechInput { TwistTarget = 10f, AimTarget = 10f }, 5f, m);
        Assert.Equal(1.75f, m.Body.Twist, 4);
        Assert.Equal(0.45f, m.Body.Aim, 4);
    }

    [Fact]
    public void LeftLeg_IsAtMinusX()
    {
        var m = new MechLocomotion(new MechTuning());
        Assert.Equal(+1, m.Gait.Legs[0].Side);
        Assert.True(m.Gait.Legs[0].Plant.Position.X < 0f);
        Assert.True(m.Pose.Legs[0].HipWorld.X < 0f);
    }

    [Fact]
    public void StanceFoot_NeverSlides()
    {
        var m = new MechLocomotion(new MechTuning());
        var stanceStart = new Vector3?[2];
        int checkedTicks = 0;
        float time = 0f;
        Run(default, 0f, m);
        for (int i = 0; i < 120 * 20; i++)
        {
            time += H;
            // Vary input over time: accelerate, turn, reverse.
            var input = new MechInput
            {
                Throttle = time < 8f ? 1f : time < 12f ? 0.3f : -0.8f,
                Turn = MathF.Sin(time * 0.7f),
            };
            m.Step(input, H);
            for (int l = 0; l < 2; l++)
            {
                var leg = m.Gait.Legs[l];
                if (leg.Swing) { stanceStart[l] = null; continue; }
                var ground = new Vector3(m.Pose.Legs[l].Target.Ankle.X, 0f, m.Pose.Legs[l].Target.Ankle.Z);
                if (stanceStart[l] is { } start && m.Footfalls.All(f => f.Leg != l))
                {
                    Assert.True(Vector3.Distance(start, ground) < 1e-5f, $"leg {l} slid at t={time}");
                    checkedTicks++;
                }
                stanceStart[l] = ground;
            }
        }
        Assert.True(checkedTicks > 1000);
    }

    [Fact]
    public void Walking_AlternatesLegs_AndEmitsFootfalls()
    {
        var m = new MechLocomotion(new MechTuning());
        var legs = new System.Collections.Generic.List<int>();
        Run(new MechInput { Throttle = 1f }, 10f, m, mm => legs.AddRange(mm.Footfalls.Select(f => f.Leg)));
        Assert.True(legs.Count >= 8);
        for (int i = 1; i < legs.Count; i++) Assert.NotEqual(legs[i - 1], legs[i]);
    }

    [Fact]
    public void AfterStopping_MechSettles_AndStandsStill()
    {
        var m = Run(new MechInput { Throttle = 1f, Turn = 0.5f }, 6f);
        Run(default, 15f, m);
        Assert.False(m.Gait.Active);
        Assert.All(m.Gait.Legs, l => Assert.False(l.Swing));
        foreach (var leg in m.Gait.Legs)
        {
            var n = GaitController.Neutral(leg, m.Body, m.Tuning);
            Assert.True(MathUtil.PlanarDistance(n.Position, leg.Plant.Position) <= 0.45f);
        }
        Assert.True(m.Gait.G < 0.01f);
    }

    [Fact]
    public void StandingStill_DoesNotStep()
    {
        var m = new MechLocomotion(new MechTuning());
        int footfalls = 0;
        Run(default, 5f, m, mm => footfalls += mm.Footfalls.Count);
        Assert.Equal(0, footfalls);
        Assert.Equal(0f, m.Gait.Phase);
    }

    [Fact]
    public void Simulation_IsDeterministic()
    {
        MechLocomotion Go() => Run(new MechInput { Throttle = 1f, Turn = 0.3f, TwistTarget = 0.5f }, 12f);
        var a = Go();
        var b = Go();
        Assert.Equal(a.Body.Position, b.Body.Position);
        Assert.Equal(a.Pose.PelvisPosition, b.Pose.PelvisPosition);
        Assert.Equal(a.Secondary.Elbows[0].X, b.Secondary.Elbows[0].X);
    }
}
