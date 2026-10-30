using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Tests;

public class CoreTests
{
    private static void Near(Vector3 expected, Vector3 actual, float eps = 1e-5f) =>
        Assert.True(Vector3.Distance(expected, actual) < eps, $"expected {expected}, got {actual}");

    [Fact]
    public void Forward_IsMinusZ_AtZeroHeading()
    {
        Near(new Vector3(0, 0, -1), MathUtil.Forward(0f));
        Near(new Vector3(1, 0, 0), MathUtil.Right(0f));
    }

    [Fact]
    public void PositiveHeading_TurnsLeft()
    {
        // Left is -X in Godot; a +90° heading faces left.
        Near(new Vector3(-1, 0, 0), MathUtil.Forward(MathF.PI / 2));
    }

    [Fact]
    public void RotateY_MatchesForwardAndGodotBasis()
    {
        for (float psi = -3f; psi < 3f; psi += 0.37f)
        {
            Near(MathUtil.Forward(psi), MathUtil.RotateY(new Vector3(0, 0, -1), psi));
            Near(Vector3.Transform(new Vector3(0.3f, 0.7f, -1.1f), Rot.Y(psi)), MathUtil.RotateY(new Vector3(0.3f, 0.7f, -1.1f), psi));
        }
    }

    [Fact]
    public void QuaternionProduct_AppliesRightOperandFirst()
    {
        // Godot's Basis/Quaternion composition: (a * b) v = a (b v). Pin that System.Numerics agrees.
        var a = Rot.Y(0.9f);
        var b = Rot.X(0.4f);
        var v = new Vector3(0.2f, -1f, 0.5f);
        Near(Vector3.Transform(Vector3.Transform(v, b), a), Vector3.Transform(v, a * b));
    }

    [Fact]
    public void RotX_PositivePitchesForwardVectorUp()
    {
        // Godot right-hand rule: +X rotation lifts -Z toward +Y.
        Near(new Vector3(0, MathF.Sin(0.5f), -MathF.Cos(0.5f)), Vector3.Transform(new Vector3(0, 0, -1), Rot.X(0.5f)));
    }

    [Fact]
    public void Spring_SettlesOnTarget_AndOscillatesAtNaturalFrequency()
    {
        var s = new Spring(2f, 0.05f);
        const float h = 1f / 120f;
        int zeroCrossings = 0;
        float prev = -1f;
        s.X = -1f;
        for (int i = 0; i < 120; i++)
        {
            s.Step(0f, h);
            if (Math.Sign(s.X) != Math.Sign(prev)) zeroCrossings++;
            prev = s.X;
        }
        Assert.InRange(zeroCrossings, 3, 5); // ~2 Hz → 4 crossings per second

        var settle = new Spring(3f, 0.7f) { X = 1f };
        for (int i = 0; i < 600; i++) settle.Step(0f, h);
        Assert.True(MathF.Abs(settle.X) < 1e-4f);
    }

    [Fact]
    public void Rng_IsDeterministic_AndInRange()
    {
        var a = new DeterministicRng(42);
        var b = new DeterministicRng(42);
        for (int i = 0; i < 1000; i++)
        {
            float x = a.NextFloat();
            Assert.Equal(x, b.NextFloat());
            Assert.InRange(x, 0f, 0.9999999f);
        }
    }
}
