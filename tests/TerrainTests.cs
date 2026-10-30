using System;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Tests;

public class TerrainTests
{
    private const float H = 1f / 120f;

    /// <summary>A single 20 m hill (sine bump, 200 m across) at z = -150, flat elsewhere.</summary>
    private static ArenaMap Hill()
    {
        var f = new HeightField(201, 4f);
        for (int iz = 0; iz < f.N; iz++)
        for (int ix = 0; ix < f.N; ix++)
        {
            float x = f.Origin + ix * f.Cell, z = f.Origin + iz * f.Cell;
            float r = MathF.Sqrt(x * x + (z + 150f) * (z + 150f));
            f[ix, iz] = r < 100f ? 10f * (1f + MathF.Cos(MathF.PI * r / 100f)) : 0f;
        }
        f.UpdateBounds();
        return new ArenaMap { Terrain = f };
    }

    [Fact]
    public void HeightField_InterpolatesNodes_AndRaysHitHills()
    {
        var map = Hill();
        Assert.Equal(20f, map.Height(0f, -150f), 1);
        Assert.Equal(0f, map.Height(0f, 0f), 3);
        Assert.Equal(0f, map.Height(1000f, 0f)); // off the grid

        // Eye level at 8 m: the 20 m hill hides a mech behind it, a ray over it is clear.
        Assert.False(map.LineClear(new Vector3(0, 8, -20), new Vector3(0, 8, -280)));
        Assert.True(map.LineClear(new Vector3(0, 30, -20), new Vector3(0, 30, -280)));
        float d = map.RayDistance(new Vector3(0, 8, 0), -Vector3.UnitZ, 400f);
        Assert.InRange(d, 60f, 110f);
        Assert.Equal(map.Height(0f, -d), 8f, 1);
    }

    [Fact]
    public void Arena_Hills_AreDeterministic_FlatAtSpawns_AndBlocksSitOnTheGround()
    {
        var a = ArenaMap.Arena(5);
        Assert.Equal(a.Terrain!.Heights, ArenaMap.Arena(5).Terrain!.Heights);
        Assert.True(a.Terrain.MaxHeight > 3f, "should be hilly");
        Assert.Equal(0f, a.Height(0f, ArenaMap.ArenaSpawnZ), 2);
        foreach (var b in a.Blocks)
        {
            Vector3 c = b.Center;
            Assert.True(b.Min.Y <= a.Height(c.X, c.Z), "block floats above the ground");
        }
        foreach (var c in a.Cover) Assert.Equal(a.Height(c.Position.X, c.Position.Z), c.Position.Y, 3);
    }

    [Fact]
    public void YawOnSlope_IsPlainYawOnFlatGround_AndTiltsTheSoleOntoASlope()
    {
        for (float yaw = -3f; yaw < 3f; yaw += 0.7f)
            Assert.True(MathF.Abs(Quaternion.Dot(Rot.YawOnSlope(yaw, Vector3.UnitY), Rot.Y(yaw))) > 0.99999f);
        var n = Vector3.Normalize(new Vector3(0f, 1f, 0.3f));
        Assert.True(Vector3.Distance(Vector3.Transform(Vector3.UnitY, Rot.YawOnSlope(0.4f, n)), n) < 1e-5f);
    }

    public static TheoryData<ChassisClass, MechModel> Mechs() => new()
    {
        { ChassisClass.Light, MechModel.Hauberk }, { ChassisClass.Medium, MechModel.Hauberk },
        { ChassisClass.Assault, MechModel.Hauberk }, { ChassisClass.Light, MechModel.Sicklefoot },
        { ChassisClass.Medium, MechModel.Sicklefoot }, { ChassisClass.Heavy, MechModel.Sicklefoot },
    };

    [Theory]
    [MemberData(nameof(Mechs))]
    public void OverAHill_FeetPlantOnTheGround_NeverSlide_AndLegsStayInReach(ChassisClass cls, MechModel model)
    {
        var map = Hill();
        var c = MechChassis.Create(cls, model);
        var m = new MechLocomotion(c) { Ground = map };
        m.Reset(Vector3.Zero, 0f);
        var stance = new Vector3?[2];
        float maxExt = 0f, minSpeed = float.MaxValue, peak = 0f;
        for (int i = 0; i < 120 * 30 && m.Body.Position.Z > -300f; i++)
        {
            float throttle = Math.Min(1f, i / 240f);
            m.Step(new MechInput { Throttle = throttle }, H);
            if (i > 600 && m.Body.Position.Z > -260f) minSpeed = MathF.Min(minSpeed, m.Body.V);
            peak = MathF.Max(peak, m.Body.Position.Y);
            for (int l = 0; l < 2; l++)
            {
                var leg = m.Gait.Legs[l];
                var lp = m.Pose.Legs[l];
                maxExt = MathF.Max(maxExt, Vector3.Distance(lp.HipWorld, lp.Target.Ankle) / c.LegLength);
                Assert.Equal(map.Height(leg.Plant.Position.X, leg.Plant.Position.Z), leg.Plant.Position.Y, 3);
                if (leg.Swing) { stance[l] = null; continue; }
                bool landed = false;
                foreach (var f in m.Footfalls) landed |= f.Leg == l;
                if (stance[l] is { } s && !landed) Assert.True(Vector3.Distance(s, lp.Target.Ankle) < 0.6f * c.Scale + 1e-4f, $"leg {l} slid");
                stance[l] = lp.Target.Ankle;
            }
            Assert.True(m.Pose.PelvisPosition.Y - map.Height(m.Body.Position.X, m.Body.Position.Z) > 0.5f * c.HipHeight, "pelvis sank into the hill");
        }
        Assert.True(peak > 15f, $"never climbed the hill (peak {peak:0.0} m)");
        Assert.True(minSpeed < 0.95f * c.VMax, "climbing should cost speed");
        Assert.True(maxExt < 0.99f, $"{cls} {model} leg overextends on the hill: {maxExt:0.000}");
    }
}
