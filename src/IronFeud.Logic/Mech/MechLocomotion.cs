using System.Collections.Generic;
using System.Numerics;

namespace IronFeud.Logic.Mech;

/// <summary>
/// One mech's locomotion pipeline, stepped at the fixed physics rate:
/// input → body kinematics → gait → secondary motion → pose.
/// </summary>
public sealed class MechLocomotion
{
    public readonly MechTuning Tuning;
    public readonly BodyState Body = new();
    public readonly GaitController Gait = new();
    public readonly SecondaryMotion Secondary;
    public readonly MechPose Pose = new();

    /// <summary>Footfalls that happened during the last <see cref="Step"/>; cleared at the start of each step.</summary>
    public readonly List<Footfall> Footfalls = new();

    public float Time { get; private set; }

    public MechLocomotion(MechTuning tuning, uint seed = 1)
    {
        Tuning = tuning;
        Secondary = new SecondaryMotion(seed);
        Reset(Vector3.Zero, 0f);
    }

    public void Reset(Vector3 position, float psi)
    {
        Body.Position = position;
        Body.Psi = psi;
        Body.V = Body.Acc = Body.Omega = Body.Twist = Body.Aim = 0f;
        Gait.Reset(Body, Tuning);
        MechPoseSolver.Solve(Pose, Body, Gait, Secondary, Tuning, Time);
    }

    public void Step(in MechInput input, float h)
    {
        Footfalls.Clear();
        BodyKinematics.Step(Body, input, Tuning, h);
        Gait.Step(Body, Tuning, h, Footfalls);
        foreach (var f in Footfalls)
            Secondary.ApplyFootfall(f, Gait.Legs[f.Leg].Side, Body.V);
        Secondary.Step(Body, Gait, Tuning, h);
        Time += h;
        MechPoseSolver.Solve(Pose, Body, Gait, Secondary, Tuning, Time);
    }
}
