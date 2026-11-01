using System;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Audio;

/// <summary>
/// The hum of a mech's joint drives, synthesised live from how fast its joints are turning: a soft, low motor tone
/// (mostly fundamental, heavily low-passed) whose pitch and loudness rise with load, plus a breath of hydraulic
/// flow noise. Smaller mechs hum higher. Bright harmonics and gated noise were tried first and were hard on the ears.
/// Pure streaming DSP; the Godot side feeds it joint speed every tick and pulls samples for an audio generator.
/// </summary>
public sealed class ServoSynth
{
    public readonly int Rate;

    /// <summary>Motor pitch at full load (Hz).</summary>
    public readonly float BaseHz;

    /// <summary>Summed joint speed (rad/s) that counts as full load.</summary>
    public readonly float FullSpeed;

    /// <summary>Overall level, 0..1.</summary>
    public float Gain = 0.2f;

    private readonly DeterministicRng _rng;
    private double _phase;
    private float _load, _lp1, _lp2, _flow;

    /// <param name="scale">Chassis scale: 0.72 light .. 1.3 assault.</param>
    public ServoSynth(float scale, int rate = 22050, uint seed = 1)
    {
        Rate = rate;
        BaseHz = 160f / MathF.Pow(scale, 1.3f);
        FullSpeed = 6f;
        _rng = new DeterministicRng(seed);
    }

    /// <summary>Current smoothed load, 0..1 (for debug display).</summary>
    public float Load => _load;

    /// <summary>Fills <paramref name="output"/> with mono samples for a summed joint speed of <paramref name="jointSpeed"/> rad/s.</summary>
    public void Render(Span<float> output, float jointSpeed)
    {
        float target = MathF.Pow(MathF.Min(1f, MathF.Max(0f, jointSpeed) / FullSpeed), 0.7f);
        float smooth = 1f - MathF.Exp(-1f / (0.06f * Rate)); // 60 ms: no zipper noise when the load steps per tick
        float lpA = 1f - MathF.Exp(-2f * MathF.PI * 900f / Rate), flowA = 1f - MathF.Exp(-2f * MathF.PI * 400f / Rate);

        for (int i = 0; i < output.Length; i++)
        {
            _load += smooth * (target - _load);
            if (_load < 1e-4f)
            {
                output[i] = 0f;
                continue;
            }
            float f = BaseHz * (0.6f + 0.4f * _load);
            _phase = (_phase + f / Rate) % 1.0;

            float ph = (float)(_phase * 2 * Math.PI);
            float tone = MathF.Sin(ph) + 0.25f * MathF.Sin(2f * ph) + 0.06f * MathF.Sin(3f * ph);
            _flow += flowA * ((_rng.NextFloat() * 2f - 1f) - _flow);

            float y = (tone * 0.7f + _flow * 0.8f) * _load;
            _lp1 += lpA * (y - _lp1);
            _lp2 += lpA * (_lp1 - _lp2);
            output[i] = _lp2 * Gain;
        }
    }
}
