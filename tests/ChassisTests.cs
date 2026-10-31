using System;
using System.Numerics;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Tests;

public class ChassisTests
{
    private const float H = 1f / 120f;

    [Fact]
    public void Prototype_IsTheWebPrototypeWalker_AndNeverRuns()
    {
        var c = MechChassis.Prototype();
        Assert.Equal(1f, c.Scale);
        Assert.Equal(4.2f, c.VMax);
        Assert.Equal(5.75f, c.HipHeight);
        Assert.Equal(0.62f, c.DutyAt(c.VMax), 4);
        Assert.Equal(100f, MechDamage.Create(c)[MechSection.Torso]);
    }

    [Theory]
    [InlineData(ChassisClass.Light, 100f, 150f)]
    [InlineData(ChassisClass.Medium, 60f, 100f)]
    [InlineData(ChassisClass.Heavy, 50f, 70f)]
    [InlineData(ChassisClass.Assault, 30f, 50f)]
    public void TopSpeed_IsInTheClassBand(ChassisClass cls, float minKmh, float maxKmh)
    {
        float kmh = MechChassis.Create(cls).VMax * 3.6f;
        Assert.InRange(kmh, minKmh, maxKmh);
    }

    [Fact]
    public void Light_IsSmallerFasterAndThinner()
    {
        MechChassis l = MechChassis.Light(), m = MechChassis.Medium();
        Assert.True(l.VMax > 1.5f * m.VMax);
        Assert.True(l.HipHeight < m.HipHeight);
        Assert.True(l.TorsoHitRadius < m.TorsoHitRadius);
        Assert.True(l.TorsoHp < m.TorsoHp);
        Assert.Equal(m.RightArmMount * l.Scale, l.RightArmMount);
    }

    public static TheoryData<ChassisClass, MechModel> AllMechs()
    {
        var data = new TheoryData<ChassisClass, MechModel>();
        foreach (var cls in Enum.GetValues<ChassisClass>())
        foreach (var model in Enum.GetValues<MechModel>())
            data.Add(cls, model);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllMechs))]
    public void TopSpeedWithTurns_FeetStayPlanted_AndLegsStayInReach(ChassisClass cls, MechModel model)
    {
        var c = MechChassis.Create(cls, model);
        var m = new MechLocomotion(c);
        var stance = new Vector3?[2];
        float maxExtension = 0f, time = 0f;
        for (int i = 0; i < 120 * 15; i++)
        {
            time += H;
            m.Step(new MechInput { Throttle = 1f, Turn = MathF.Sin(time * 0.9f) }, H);
            for (int l = 0; l < 2; l++)
            {
                var lp = m.Pose.Legs[l];
                maxExtension = MathF.Max(maxExtension, Vector3.Distance(lp.HipWorld, lp.Target.Ankle) / c.LegLength);
                if (m.Gait.Legs[l].Swing) { stance[l] = null; continue; }
                var ground = new Vector3(lp.Target.Ankle.X, 0f, lp.Target.Ankle.Z);
                bool landed = false;
                foreach (var f in m.Footfalls) landed |= f.Leg == l;
                if (stance[l] is { } s && !landed) Assert.True(Vector3.Distance(s, ground) < 1e-5f, $"{cls} {model} leg {l} slid at t={time}");
                stance[l] = ground;
            }
        }
        Assert.True(MathF.Abs(m.Body.V) > 0.6f * c.VMax, $"{cls} only reached {m.Body.V:0.0} m/s"); // turning costs speed
        Assert.True(maxExtension < 0.99f, $"{cls} {model} leg overextends: {maxExtension:0.000} of full length");
    }

    [Fact]
    public void Running_HasAFlightPhase_AndLightStepsFasterThanMedium()
    {
        var light = MechChassis.Light();
        Assert.True(light.DutyAt(light.VMax) < 0.5f, "at top speed both feet leave the ground between steps");
        Assert.True(Cadence(light) > 1.5f * Cadence(MechChassis.Medium()));
    }

    [Theory]
    [InlineData(ChassisClass.Light, MechModel.Hauberk)]
    [InlineData(ChassisClass.Medium, MechModel.Hauberk)]
    [InlineData(ChassisClass.Heavy, MechModel.Hauberk)]
    [InlineData(ChassisClass.Light, MechModel.Sicklefoot)]
    [InlineData(ChassisClass.Medium, MechModel.Sicklefoot)]
    public void AtTopSpeed_StrideIsSetByLegReach_NotByAShuffle(ChassisClass cls, MechModel model)
    {
        // Regression: the walking stride cap (DMax) once stayed on at a run and made the light take 16 steps/s.
        var c = MechChassis.Create(cls, model);
        var m = new MechLocomotion(c);
        for (int i = 0; i < 120 * 10; i++) m.Step(new MechInput { Throttle = 1f }, H);
        float reachLimited = c.VMax * m.Gait.Duty / (2f * c.AMax);
        Assert.Equal(reachLimited, m.Gait.F, 2);
        Assert.True(2f * m.Gait.F < 6f, $"{cls}: {2f * m.Gait.F:0.0} steps/s");
    }

    private static float Cadence(MechChassis c)
    {
        var m = new MechLocomotion(c);
        int steps = 0;
        for (int i = 0; i < 120 * 10; i++)
        {
            m.Step(new MechInput { Throttle = 1f }, H);
            if (i >= 120 * 5) steps += m.Footfalls.Count;
        }
        return steps / 5f;
    }
}
