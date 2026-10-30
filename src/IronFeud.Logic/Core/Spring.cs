using System;

namespace IronFeud.Logic.Core;

/// <summary>Damped spring parameterised by natural frequency and damping ratio, stepped with semi-implicit Euler.</summary>
public sealed class Spring
{
    public readonly float K;
    public readonly float C;
    public float X;
    public float V;

    public Spring(float freqHz, float zeta)
    {
        K = MathUtil.Tau * freqHz * MathUtil.Tau * freqHz;
        C = 2f * zeta * MathF.Sqrt(K);
    }

    public float Step(float target, float h)
    {
        float a = K * (target - X) - C * V;
        V += a * h;
        X += V * h;
        return X;
    }
}
