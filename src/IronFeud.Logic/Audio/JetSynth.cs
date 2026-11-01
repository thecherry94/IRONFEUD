using System;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Audio;

/// <summary>
/// Jump-jet roar, streamed live: a dull low-passed noise rush over a sub rumble, with a fast ignition (40 ms) and a
/// short fade when the burn stops (150 ms). Bigger mechs roar lower.
/// </summary>
public sealed class JetSynth
{
    public readonly int Rate;
    public float Gain = 0.5f;

    private readonly DeterministicRng _rng;
    private readonly float _rushA, _rumbleA;
    private float _level, _rush1, _rush2, _rumble1, _rumble2;

    /// <param name="scale">Chassis scale: 0.72 light .. 1.3 assault.</param>
    public JetSynth(float scale, int rate = 22050, uint seed = 3)
    {
        Rate = rate;
        _rng = new DeterministicRng(seed);
        _rushA = 1f - MathF.Exp(-2f * MathF.PI * (1100f / scale) / rate);
        _rumbleA = 1f - MathF.Exp(-2f * MathF.PI * (110f / scale) / rate);
    }

    public float Level => _level;

    public void Render(Span<float> output, bool burning)
    {
        float tau = burning ? 0.04f : 0.15f, k = 1f - MathF.Exp(-1f / (tau * Rate)), target = burning ? 1f : 0f;
        for (int i = 0; i < output.Length; i++)
        {
            _level += k * (target - _level);
            if (_level < 1e-4f)
            {
                output[i] = 0f;
                continue;
            }
            float n = _rng.NextFloat() * 2f - 1f;
            _rush1 += _rushA * (n - _rush1);
            _rush2 += _rushA * (_rush1 - _rush2);
            _rumble1 += _rumbleA * (n - _rumble1);
            _rumble2 += _rumbleA * (_rumble1 - _rumble2);
            output[i] = (_rush2 * 1.6f + _rumble2 * 5f) * _level * Gain;
        }
    }
}
