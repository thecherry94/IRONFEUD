using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Core;

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

    /// <summary>Terrain; null = flat ground at y = 0. Set before <see cref="Reset"/>.</summary>
    public IGround? Ground
    {
        get => Gait.Ground;
        set => Gait.Ground = value;
    }

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
        Body.Position = Ground.OnGround(position);
        Body.Psi = psi;
        Body.V = Body.Acc = Body.Omega = Body.Twist = Body.Aim = Body.Slope = _groundDrop = 0f;
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
            leg.Plant = new FootPlant(Ground.OnGround(ankle), Pose.Legs[i].Target.Yaw);
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

        float grade = Grade();
        BodyKinematics.Step(Body, input, Chassis, h, SpeedFactor * UphillFactor(grade));
        Body.Slope = MathUtil.Lerp(Body.Slope, MathF.Atan(grade), MathF.Min(1f, h * 3f));
        Gait.Step(Body, Chassis, h, Footfalls);
        FollowGround(grade, h);
        foreach (var f in Footfalls)
            Secondary.ApplyFootfall(f, Gait.Legs[f.Leg].Side, Body.V, Chassis.Scale);
        Secondary.Step(Body, Gait, Chassis, h);
        Solve();
    }

    /// <summary>Ground rise per metre along the heading at the body (+ = uphill ahead).</summary>
    private float Grade()
    {
        if (Ground == null) return 0f;
        float d = 2f * Chassis.Scale;
        Vector3 f = MathUtil.Forward(Body.Psi) * d, p = Body.Position;
        return (Ground.Height(p.X + f.X, p.Z + f.Z) - Ground.Height(p.X - f.X, p.Z - f.Z)) / (2f * d);
    }

    /// <summary>Climbing costs top speed: −12 % per 10 % grade, floored at 45 %. Downhill is free.</summary>
    private float UphillFactor(float grade)
    {
        float climb = grade * MathF.Sign(Body.V);
        return MathUtil.Clamp(1f - 1.2f * MathF.Max(0f, climb), 0.45f, 1f);
    }

    /// <summary>
    /// The body rides on the ground under it (exactly: at speed any lag over-extends the legs) minus a smoothed drop:
    /// down to the lowest planted foot, and a little more on slopes, so the downhill leg can always reach.
    /// The swinging leg is ignored; its last plant can be a whole stride behind.
    /// </summary>
    private void FollowGround(float grade, float h)
    {
        if (Ground == null) return;
        float under = Ground.Height(Body.Position.X, Body.Position.Z);
        float lowest = float.PositiveInfinity;
        foreach (var leg in Gait.Legs)
            if (!leg.Swing) lowest = MathF.Min(lowest, leg.Plant.Position.Y);
        if (float.IsFinite(lowest))
        {
            float target = MathF.Min(0f, lowest - under) - 0.5f * MathF.Abs(grade) * Chassis.AMax;
            _groundDrop = MathUtil.Lerp(_groundDrop, target, MathF.Min(1f, h * 6f));
        }
        Body.Position.Y = under + _groundDrop;
    }

    private float _groundDrop;

    private void Solve() => MechPoseSolver.Solve(Pose, Body, Gait, Secondary, Chassis, Time, CollapseAmount, CollapseRoll);
}
