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
        Body.V = Body.Acc = Body.Omega = Body.Twist = Body.Aim = Body.Slope = _groundVel = 0f;
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

        Body.Slope = MathUtil.Lerp(Body.Slope, MathF.Atan(Grade()), MathF.Min(1f, h * 3f));
        BodyKinematics.Step(Body, input, Chassis, h, SpeedFactor * UphillFactor() * RoughGroundFactor());
        // Shorter strides (and so quicker steps) on slopes keep footholds up and down the hill within reach.
        Gait.ReachScale = 1f / (1f + 1.5f * MathF.Abs(MathF.Tan(Body.Slope)));
        Gait.Step(Body, Chassis, h, Footfalls);
        FollowGround(h);
        foreach (var f in Footfalls)
            Secondary.ApplyFootfall(f, Gait.Legs[f.Leg].Side, Body.V, Chassis.Scale);
        Secondary.Step(Body, Gait, Chassis, h);
        Solve();
    }

    /// <summary>Ground rise per metre along the heading over the leg's reach (+ = uphill ahead): crater lips don't count.</summary>
    private float Grade()
    {
        if (Ground == null) return 0f;
        float d = 0.5f * Chassis.LegLength;
        Vector3 f = MathUtil.Forward(Body.Psi) * d, p = Body.Position;
        return (Ground.Height(p.X + f.X, p.Z + f.Z) - Ground.Height(p.X - f.X, p.Z - f.Z)) / (2f * d);
    }

    /// <summary>
    /// Climbing costs top speed: −14 % per 10 % grade, down to a crawl (20 %) up the steepest walls. Gentle descents are
    /// free; steep ones (past 30 %) are taken carefully too, or the feet can't keep up with the ground falling away.
    /// Uses the smoothed slope so speed (and with it the torso's acceleration lean) doesn't jump at every rim.
    /// </summary>
    private float UphillFactor()
    {
        float climb = MathF.Tan(Body.Slope) * MathF.Sign(Body.V);
        return MathUtil.Clamp(1f - 1.4f * MathF.Max(0f, climb) - 0.9f * MathF.Max(0f, -climb - 0.3f), 0.2f, 1f);
    }

    /// <summary>
    /// Rough ground ahead caps speed, like a pilot easing off before a trench: the steepest 3 m step within braking
    /// distance (plus half a second) along the path; grade 0.2 is free, a trench wall (~1.2) allows 35 %.
    /// </summary>
    private float RoughGroundFactor()
    {
        float v = MathF.Abs(Body.V);
        if (Ground == null || v < 3f) return 1f;
        float step = 3f * Chassis.Scale, reach = MathF.Min(60f, v * v / (2f * Chassis.Decel) + 0.5f * v);
        Vector3 dir = MathUtil.Forward(Body.Psi) * MathF.Sign(Body.V), p = Body.Position;
        float prev = Ground.Height(p.X, p.Z), steepest = 0f;
        for (float d = step; d <= reach; d += step)
        {
            Vector3 q = p + dir * d;
            float hq = Ground.Height(q.X, q.Z);
            steepest = MathF.Max(steepest, MathF.Abs(hq - prev) / step);
            prev = hq;
        }
        return MathUtil.Clamp(1.15f - 0.8f * steepest, 0.35f, 1f);
    }

    /// <summary>
    /// The body rides on the ground averaged over its footprint (centre and a ring at half a leg length, here and a
    /// quarter second ahead), through a critically damped spring: trench edges and crater rims don't yank the pelvis.
    /// It never sinks lower than 0.3 hip heights below the ground right under it. The legs absorb the rest (the pose
    /// solver crouches and clamps foot targets).
    /// </summary>
    private void FollowGround(float h)
    {
        if (Ground == null) return;
        Vector3 p = Body.Position, ahead = p + MathUtil.Forward(Body.Psi) * (Body.V * 0.25f);
        float target = 0.5f * (Support(p) + Support(ahead));
        float under = Ground.Height(p.X, p.Z);
        target = MathF.Max(target, under - 0.3f * Chassis.HipHeight);

        const float omega = 9f;
        _groundVel += (omega * omega * (target - Body.Position.Y) - 2f * omega * _groundVel) * h;
        Body.Position.Y += _groundVel * h;
    }

    private float Support(Vector3 p)
    {
        float r = 0.5f * Chassis.LegLength, sum = 2f * Ground!.Height(p.X, p.Z);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.PI / 4f;
            sum += Ground.Height(p.X + MathF.Cos(a) * r, p.Z + MathF.Sin(a) * r);
        }
        return sum / 10f;
    }

    private float _groundVel;

    private void Solve() => MechPoseSolver.Solve(Pose, Body, Gait, Secondary, Chassis, Time, CollapseAmount, CollapseRoll);
}
