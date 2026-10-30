using System;
using System.Numerics;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Tests;

public class ChassisTests
{
    private const float H = 1f / 120f;

    [Fact]
    public void Medium_IsThePrototypeMech()
    {
        var c = MechChassis.Medium();
        Assert.Equal(1f, c.Scale);
        Assert.Equal(4.2f, c.VMax);
        Assert.Equal(5.75f, c.HipHeight);
        Assert.Equal(100f, MechDamage.Create(c)[MechSection.Torso]);
    }

    [Fact]
    public void Light_IsSmallerFasterAndThinner()
    {
        MechChassis l = MechChassis.Light(), m = MechChassis.Medium();
        Assert.True(l.VMax > 2f * m.VMax);
        Assert.True(l.HipHeight < m.HipHeight);
        Assert.True(l.TorsoHitRadius < m.TorsoHitRadius);
        Assert.True(l.TorsoHp < m.TorsoHp);
        Assert.Equal(m.RightArmMuzzle * l.Scale, l.RightArmMuzzle);
    }

    [Theory]
    [InlineData(ChassisClass.Light)]
    [InlineData(ChassisClass.Medium)]
    public void TopSpeedWithTurns_FeetStayPlanted_AndLegsStayInReach(ChassisClass cls)
    {
        var c = MechChassis.Create(cls);
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
                maxExtension = MathF.Max(maxExtension, Vector3.Distance(lp.HipWorld, lp.Target.Ankle) / (c.L1 + c.L2));
                if (m.Gait.Legs[l].Swing) { stance[l] = null; continue; }
                var ground = new Vector3(lp.Target.Ankle.X, 0f, lp.Target.Ankle.Z);
                bool landed = false;
                foreach (var f in m.Footfalls) landed |= f.Leg == l;
                if (stance[l] is { } s && !landed) Assert.True(Vector3.Distance(s, ground) < 1e-5f, $"{cls} leg {l} slid at t={time}");
                stance[l] = ground;
            }
        }
        Assert.Equal(c.VMax, MathF.Abs(m.Body.V), 0.5f);
        Assert.True(maxExtension < 0.99f, $"{cls} leg overextends: {maxExtension:0.000} of full length");
    }

    [Fact]
    public void Light_StepsFasterThanMedium()
    {
        static float Cadence(MechChassis c)
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
        Assert.True(Cadence(MechChassis.Light()) > 2f * Cadence(MechChassis.Medium()));
    }
}
