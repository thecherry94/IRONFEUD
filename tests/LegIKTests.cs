using System;
using System.Numerics;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Tests;

public class LegIKTests
{
    private const float L1 = 3.2f, L2 = 3.3f;

    [Fact]
    public void ReachableTargets_AreReachedExactly()
    {
        var rng = new DeterministicRng(7);
        for (int i = 0; i < 2000; i++)
        {
            var d = new Vector3(rng.Range(-1.5f, 1.5f), rng.Range(-6.3f, -3f), rng.Range(-2.5f, 2.5f));
            if (d.Length() > L1 + L2 - 0.01f) continue;
            var a = LegIK.Solve(d, L1, L2);
            var (_, ankle) = LegIK.Forward(a, L1, L2);
            Assert.True(Vector3.Distance(d, ankle) < 1e-3f, $"target {d} reached {ankle}");
        }
    }

    [Fact]
    public void Knee_PointsForward()
    {
        // Ankle straight below the hip: the knee must bulge toward -Z (mech forward).
        var a = LegIK.Solve(new Vector3(0, -5.5f, 0), L1, L2);
        var (knee, _) = LegIK.Forward(a, L1, L2);
        Assert.True(knee.Z < -0.5f, $"knee at {knee}");
        Assert.True(a.HipPitch > 0f && a.Knee < 0f);
    }

    [Fact]
    public void UnreachableTarget_StretchesTowardIt()
    {
        var d = new Vector3(0.5f, -9f, -1f);
        var (_, ankle) = LegIK.Forward(LegIK.Solve(d, L1, L2), L1, L2);
        Assert.True(Vector3.Dot(Vector3.Normalize(ankle), Vector3.Normalize(d)) > 0.9999f);
    }

    [Fact]
    public void PoseSolver_PlacesAnkleOnTarget_WithWorldFootOrientation()
    {
        var m = new MechLocomotion(new MechChassis());
        for (int i = 0; i < 120 * 8; i++)
        {
            m.Step(new MechInput { Throttle = 1f, Turn = 0.6f, TwistTarget = 1f }, 1f / 120f);
            foreach (var leg in m.Pose.Legs)
            {
                Assert.True(Vector3.Distance(leg.Target.Ankle, leg.AnkleWorld) < 2e-3f);
                // Composing the local chain must reproduce the requested world foot rotation.
                var world = m.Pose.PelvisRotation * leg.Hip * leg.Knee * leg.Ankle;
                Assert.True(MathF.Abs(Quaternion.Dot(world, leg.FootWorld)) > 0.99999f);
            }
        }
    }

    private const float D1 = 2.3f, D2 = 3.0f, D3 = 1.9f;

    [Fact]
    public void Digitigrade_ReachesTargets_WithTheFootBoneParallelToTheThigh()
    {
        var rng = new DeterministicRng(11);
        for (int i = 0; i < 2000; i++)
        {
            var d = new Vector3(rng.Range(-1.5f, 1.5f), rng.Range(-6.8f, -3f), rng.Range(-3f, 3f));
            if (d.Length() > D1 + D2 + D3 - 0.01f) continue;
            var a = LegIK.SolveDigitigrade(d, D1, D2, D3);
            var (knee, hock, toe) = LegIK.ForwardDigitigrade(a, D1, D2, D3);
            Assert.True(Vector3.Distance(d, toe) < 1e-3f, $"target {d} reached {toe}");
            Vector3 thigh = Vector3.Normalize(knee), foot = Vector3.Normalize(toe - hock);
            Assert.True(Vector3.Dot(thigh, foot) > 0.99999f, "foot bone not parallel to the thigh");
        }
    }

    [Fact]
    public void Digitigrade_KneePointsForward_HockBackward()
    {
        // Toe straight below the hip: a "Z" leg.
        var a = LegIK.SolveDigitigrade(new Vector3(0, -5.2f, 0), D1, D2, D3);
        var (knee, hock, toe) = LegIK.ForwardDigitigrade(a, D1, D2, D3);
        Assert.True(knee.Z < -0.3f, $"knee at {knee}");
        Assert.True(hock.Z > knee.Z + 0.5f && hock.Z > toe.Z + 0.3f, $"hock at {hock} should sit behind knee {knee} and toe {toe}");
    }

    [Fact]
    public void PoseSolver_Digitigrade_PlacesToeOnTarget_WithWorldFootOrientation()
    {
        var m = new MechLocomotion(MechChassis.Medium(MechModel.Sicklefoot));
        for (int i = 0; i < 120 * 8; i++)
        {
            m.Step(new MechInput { Throttle = 1f, Turn = 0.6f, TwistTarget = 1f }, 1f / 120f);
            foreach (var leg in m.Pose.Legs)
            {
                Assert.True(Vector3.Distance(leg.Target.Ankle, leg.AnkleWorld) < 2e-3f);
                var world = m.Pose.PelvisRotation * leg.Hip * leg.Knee * leg.Hock * leg.Ankle;
                Assert.True(MathF.Abs(Quaternion.Dot(world, leg.FootWorld)) > 0.99999f);
            }
        }
    }
}
