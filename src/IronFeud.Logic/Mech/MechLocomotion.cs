using System.Collections.Generic;
using System.Numerics;

namespace IronFeud.Logic.Mech;

/// <summary>
/// One mech's locomotion pipeline, stepped at the fixed physics rate:
/// input → body kinematics → gait → secondary motion → pose.
/// </summary>
public sealed class MechLocomotion
{
    public readonly MechChassis Chassis;
    public readonly BodyState Body = new();
    public readonly GaitController Gait = new();
    public readonly SecondaryMotion Secondary;
    public readonly MechPose Pose = new();

    /// <summary>Footfalls that happened during the last <see cref="Step"/>; cleared at the start of each step.</summary>
    public readonly List<Footfall> Footfalls = new();

    public float Time { get; private set; }

    /// <summary>Forward top-speed multiplier; MechSim lowers it when the legs are destroyed.</summary>
    public float SpeedFactor = 1f;

    /// <summary>The mech is out (wreck or dead pilot): it ignores input and slumps over <see cref="MechChassis.CollapseTime"/>.</summary>
    public bool Collapsed { get; private set; }
    public float CollapseTime { get; private set; }

    /// <summary>Sideways lean of the slump, radians (+ = lean right).</summary>
    public float CollapseRoll { get; private set; }

    /// <summary>0..1 eased progress of the slump.</summary>
    public float CollapseAmount => Collapsed ? Core.MathUtil.Smoothstep(0f, Chassis.CollapseTime, CollapseTime) : 0f;

    public MechLocomotion(MechChassis chassis, uint seed = 1)
    {
        Chassis = chassis;
        Secondary = new SecondaryMotion(seed);
        Reset(Vector3.Zero, 0f);
    }

    public void Reset(Vector3 position, float psi)
    {
        Body.Position = position;
        Body.Psi = psi;
        Body.V = Body.Acc = Body.Omega = Body.Twist = Body.Aim = 0f;
        Gait.Reset(Body, Chassis);
        Solve();
    }

    /// <summary>Stops the mech for good. Swinging feet are put down where they are so nothing slides.</summary>
    public void Collapse(float roll)
    {
        if (Collapsed) return;
        Collapsed = true;
        CollapseTime = 0f;
        CollapseRoll = roll;
        for (int i = 0; i < Gait.Legs.Length; i++)
        {
            var leg = Gait.Legs[i];
            if (!leg.Swing) continue;
            var ankle = Pose.Legs[i].Target.Ankle;
            leg.Plant = new FootPlant(new Vector3(ankle.X, Body.Position.Y, ankle.Z), Pose.Legs[i].Target.Yaw);
            leg.Swing = false;
        }
    }

    public void Step(in MechInput input, float h)
    {
        Footfalls.Clear();
        Time += h;
        if (Collapsed)
        {
            CollapseTime += h;
            Body.V = Body.Acc = Body.Omega = 0f;
            Gait.G = System.MathF.Max(0f, Gait.G - h);
            Secondary.StepCollapsed(h);
            Solve();
            return;
        }

        BodyKinematics.Step(Body, input, Chassis, h, SpeedFactor);
        Gait.Step(Body, Chassis, h, Footfalls);
        foreach (var f in Footfalls)
            Secondary.ApplyFootfall(f, Gait.Legs[f.Leg].Side, Body.V, Chassis.Scale);
        Secondary.Step(Body, Gait, Chassis, h);
        Solve();
    }

    private void Solve() => MechPoseSolver.Solve(Pose, Body, Gait, Secondary, Chassis, Time, CollapseAmount, CollapseRoll);
}
