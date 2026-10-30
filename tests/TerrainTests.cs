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
        int stanceTicks = 0, floating = 0;
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
                maxExt = MathF.Max(maxExt, Vector3.Distance(lp.HipWorld, lp.AnkleWorld) / c.LegLength);
                Assert.Equal(map.Height(leg.Plant.Position.X, leg.Plant.Position.Z), leg.Plant.Position.Y, 3);
                if (leg.Swing) { stance[l] = null; continue; }
                stanceTicks++;
                if (Vector3.Distance(lp.Target.Ankle, lp.AnkleWorld) > 0.3f) floating++;
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
        Assert.True(floating < 0.05f * stanceTicks, $"{cls} {model}: planted feet off target {100f * floating / stanceTicks:0}% of the time");
    }

    [Fact]
    public void NoMansLand_HasTrenches_Craters_SteepCover_AndBrokenSightLines()
    {
        var map = ArenaMap.NoMansLand(3);
        var f = map.Terrain!;
        Assert.Equal(f.Heights, ArenaMap.NoMansLand(3).Terrain!.Heights);
        Assert.Equal(2, map.Trenches.Count);

        float lo = float.MaxValue, hi = float.MinValue;
        foreach (float h in f.Heights) { lo = MathF.Min(lo, h); hi = MathF.Max(hi, h); }
        Assert.True(hi - lo > 18f, $"relief only {hi - lo:0.0} m");
        float g = f.MaxGrade();
        Assert.InRange(g, 0.8f, 2.5f); // steep trench walls and crater rims, no cliffs

        // The trench in front of Alpha's spawn is a ditch with a parapet toward the enemy.
        var line = map.Trenches[1];
        Vector2 mid = (line[2] + line[3]) / 2f;
        Assert.True(map.Height(mid.X, mid.Y) < -3f, "trench bottom");
        Assert.True(map.Height(mid.X, mid.Y - 11f) > 2.5f, "parapet in front");

        int terrainCover = 0;
        foreach (var c in map.Cover) if (!map.InsideAnyBlock(c.Position.X, c.Position.Z, 10f)) terrainCover++;
        Assert.True(terrainCover > 100, $"only {terrainCover} terrain cover points");

        // Standing at the trenches, a mech's eye (~9.5 m) mostly can't see the other trench line.
        int blocked = 0, lanes = 0;
        for (float x = -180f; x <= 180f; x += 20f, lanes++)
        {
            var a = new Vector3(x, map.Height(x, 160f) + 9.5f, 160f);
            var b = new Vector3(x, map.Height(x, -160f) + 7.5f, -160f);
            if (!map.LineClear(a, b)) blocked++;
        }
        Assert.True(blocked > lanes / 2, $"only {blocked}/{lanes} lanes blocked");
    }

    /// <summary>A 4.5 m trench (parapet in front) at z = -60 and a 12 m, 40° mound at z = -160.</summary>
    private static ArenaMap Obstacles()
    {
        var f = HeightField.Create(300f, 2f)
            .AddTrench(new[] { new Vector2(-80f, -60f), new Vector2(80f, -60f) }, 6f, 4.5f, 5.5f, new Vector2(0f, -1f))
            .AddMound(0f, -160f, 30f, 12f, 0.85f);
        f.UpdateBounds();
        return new ArenaMap { Terrain = f };
    }

    [Theory]
    [MemberData(nameof(Mechs))]
    public void AcrossATrenchAndOverASteepMound_LegsStayInReach_AndFeetStayOnTheGround(ChassisClass cls, MechModel model)
    {
        var map = Obstacles();
        var c = MechChassis.Create(cls, model);
        var m = new MechLocomotion(c) { Ground = map };
        m.Reset(Vector3.Zero, 0f);
        float maxExt = 0f;
        int stanceTicks = 0, floating = 0;
        for (int i = 0; i < 120 * 60 && m.Body.Position.Z > -220f; i++)
        {
            m.Step(new MechInput { Throttle = Math.Min(1f, i / 240f) }, H);
            for (int l = 0; l < 2; l++)
            {
                var leg = m.Gait.Legs[l];
                var lp = m.Pose.Legs[l];
                maxExt = MathF.Max(maxExt, Vector3.Distance(lp.HipWorld, lp.AnkleWorld) / c.LegLength);
                Assert.Equal(map.Height(leg.Plant.Position.X, leg.Plant.Position.Z), leg.Plant.Position.Y, 3);
                if (leg.Swing) continue;
                stanceTicks++;
                if (Vector3.Distance(lp.Target.Ankle, lp.AnkleWorld) > 0.3f) floating++;
            }
        }
        Assert.True(m.Body.Position.Z < -220f, $"{cls} {model} got stuck at z = {m.Body.Position.Z:0}");
        Assert.True(maxExt < 0.99f, $"{cls} {model} leg overextends: {maxExt:0.000}");
        Assert.True(floating < 0.1f * stanceTicks, $"{cls} {model}: planted feet off target {100f * floating / stanceTicks:0}% of the time");
    }

    public static TheoryData<MechModel, float> PoseRuns() => new()
    {
        { MechModel.Hauberk, 0.35f }, { MechModel.Hauberk, 1f }, { MechModel.Sicklefoot, 0.35f }, { MechModel.Sicklefoot, 1f },
    };

    /// <summary>
    /// Pose quality across No Man's Land (trenches, craters, mounds), walking and at a full run. Pins the fixes for
    /// "legs twist into impossible poses" and "torso swings like a pendulum". Flat-ground reference values: torso pitch
    /// −0.12..0.06 rad at ≤ 0.8 rad/s.
    /// </summary>
    [Theory]
    [MemberData(nameof(PoseRuns))]
    public void AcrossNoMansLand_PosesStayNatural(MechModel model, float throttle)
    {
        var map = ArenaMap.NoMansLand(3);
        var c = MechChassis.Medium(model);
        var m = new MechLocomotion(c) { Ground = map };
        m.Reset(new Vector3(-60, 0, 190), 0f);
        float maxRoll = 0, minKnee = 9, maxKnee = 0, minPitch = 9, maxPitch = -9, prevPitch = float.NaN, maxPitchRate = 0, minPelvis = 99;
        int stance = 0, floating = 0;
        for (int i = 0; i < 120 * 40 && m.Body.Position.Z > -190f; i++)
        {
            m.Step(new MechInput { Throttle = throttle, Turn = 0.15f * MathF.Sin(i / 300f) }, H);
            var p = m.Pose;
            float pitch = MathF.Asin(Vector3.Transform(-Vector3.UnitZ, p.PelvisRotation * p.TorsoRotation).Y);
            if (!float.IsNaN(prevPitch)) maxPitchRate = MathF.Max(maxPitchRate, MathF.Abs(pitch - prevPitch) / H);
            prevPitch = pitch;
            minPitch = MathF.Min(minPitch, pitch);
            maxPitch = MathF.Max(maxPitch, pitch);
            minPelvis = MathF.Min(minPelvis, p.PelvisPosition.Y - map.Height(m.Body.Position.X, m.Body.Position.Z));
            for (int l = 0; l < 2; l++)
            {
                var lp = p.Legs[l];
                maxRoll = MathF.Max(maxRoll, MathF.Abs(lp.Angles.Roll));
                minKnee = MathF.Min(minKnee, -lp.Angles.Knee);
                maxKnee = MathF.Max(maxKnee, -lp.Angles.Knee);
                Assert.True(2f * MathF.Acos(MathF.Min(1f, MathF.Abs(lp.Ankle.W))) < 0.81f, "ankle bent past its limit");
                if (m.Gait.Legs[l].Swing) continue;
                stance++;
                if (Vector3.Distance(lp.Target.Ankle, lp.AnkleWorld) > 0.3f) floating++;
            }
        }
        string what = $"{model} at throttle {throttle}";
        Assert.True(maxRoll < 0.36f, $"{what}: hip roll {maxRoll:0.00} rad");
        Assert.InRange(minKnee, 0.3f, 3f);
        Assert.True(maxKnee < 2.5f, $"{what}: knee folds to {maxKnee:0.00} rad");
        Assert.True(minPitch > -0.4f && maxPitch < 0.25f, $"{what}: torso pitch {minPitch:0.00}..{maxPitch:0.00} rad");
        Assert.True(maxPitchRate < 3f, $"{what}: torso pitches at up to {maxPitchRate:0.0} rad/s");
        Assert.True(minPelvis > 0.3f * c.HipHeight, $"{what}: pelvis {minPelvis:0.0} m over the ground");
        Assert.True(floating < 0.1f * stance, $"{what}: planted feet off target {100f * floating / stance:0}% of the time");
    }
}
