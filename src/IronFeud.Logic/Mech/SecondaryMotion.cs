using System;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>
/// Spring-driven secondary motion and trauma ("jank") shake.
/// Angle semantics: pitch + = nose/forearm down, roll + = lean right, yaw + = left.
/// <see cref="MechPoseSolver"/> converts these to Godot Euler angles.
/// </summary>
public sealed class SecondaryMotion
{
    public readonly Spring BounceY = new(3.0f, 0.22f);
    public readonly Spring TorsoYaw = new(1.4f, 0.35f);
    public readonly Spring TorsoRoll = new(1.5f, 0.30f);
    public readonly Spring TorsoPitch = new(1.7f, 0.28f);

    /// <summary>Shoulder pitch springs, index 0 = left, 1 = right (same order as legs).</summary>
    public readonly Spring[] Arms = { new(1.1f, 0.25f), new(1.1f, 0.25f) };
    public readonly Spring[] Elbows = { new(2.3f, 0.18f), new(2.3f, 0.18f) };

    /// <summary>0..1, decays 1.6/s. Drives the high-frequency shake.</summary>
    public float Trauma;

    private readonly DeterministicRng _rng;

    public SecondaryMotion(uint seed) => _rng = new DeterministicRng(seed);

    public void Step(BodyState body, GaitController gait, MechChassis t, float h)
    {
        float p = gait.Phase, g = gait.G;
        float c0 = MathF.Cos(MathUtil.Tau * p);
        float cs = MathF.Cos(MathUtil.Tau * (p - 0.31f));
        float spF = MathF.Abs(body.V) / t.VMax;

        TorsoYaw.Step(0.13f * t.Sway * g * c0, h);
        TorsoRoll.Step(-0.05f * t.Sway * g * cs + 0.18f * body.Omega * spF, h);
        TorsoPitch.Step(0.05f + 0.02f * t.Bob * g * MathF.Cos(MathUtil.Tau * (2f * p - 0.1f)) - 0.06f * body.Acc, h);
        Arms[0].Step(0.14f * t.Sway * g * c0, h);
        Arms[1].Step(-0.14f * t.Sway * g * c0, h);
        Elbows[0].Step(0f, h);
        Elbows[1].Step(0f, h);
        BounceY.Step(0f, h);
        Trauma = MathF.Max(0f, Trauma - h * 1.6f);
    }

    /// <summary>Wreck: everything relaxes to rest except the torso, which slumps forward.</summary>
    public void StepCollapsed(float h)
    {
        BounceY.Step(0f, h);
        TorsoYaw.Step(0f, h);
        TorsoRoll.Step(0f, h);
        TorsoPitch.Step(0f, h);
        for (int i = 0; i < 2; i++)
        {
            Arms[i].Step(0f, h);
            Elbows[i].Step(0f, h);
        }
        TorsoPitch.X = MathUtil.Lerp(TorsoPitch.X, 0.45f, h * 1.5f);
        Trauma = MathF.Max(0f, Trauma - h * 1.6f);
    }

    /// <summary>A hit shakes the torso proportionally to damage.</summary>
    public void ApplyHit(float damage)
    {
        AddTrauma(damage * 0.03f);
        TorsoPitch.V += (_rng.NextFloat() - 0.5f) * damage * 0.05f;
        TorsoRoll.V += (_rng.NextFloat() - 0.5f) * damage * 0.05f;
    }

    /// <summary>Signed random number in [-0.5, 0.5) from this mech's stream.</summary>
    public float RandomSigned() => _rng.NextFloat() - 0.5f;

    /// <summary>The "weight": kick every spring when a foot lands. <paramref name="scale"/> = chassis scale (the bounce is in metres).</summary>
    public void ApplyFootfall(in Footfall f, int side, float v, float scale = 1f)
    {
        float k = f.Strength;
        BounceY.V -= 2.4f * scale * k;
        TorsoPitch.V += 0.55f * k * (v < 0f ? -1f : 1f);
        TorsoRoll.V += -side * 0.35f * k;
        Arms[0].V += 0.8f * k;
        Arms[1].V += 0.8f * k;
        Elbows[0].V += 1.6f * k * _rng.Range(0.7f, 1.3f);
        Elbows[1].V += 1.6f * k * _rng.Range(0.7f, 1.3f);
        AddTrauma(0.55f * k);
    }

    public void AddTrauma(float amount) => Trauma = MathF.Min(1f, Trauma + amount);

    /// <summary>Signed shake value: trauma² · two incommensurate sines.</summary>
    public float Jank(float time, MechChassis t) =>
        Trauma * Trauma * t.Jank * (0.6f * MathF.Sin(time * 53.1f) + 0.4f * MathF.Sin(time * 97.3f + 1.7f));
}
