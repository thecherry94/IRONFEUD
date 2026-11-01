using System;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Audio;

/// <summary>
/// The whine of a mech's joint motors, synthesised live from how fast its joints are turning: a bright motor tone
/// whose pitch and loudness rise with load, plus gear chatter. Smaller mechs whine higher.
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
    public float Gain = 0.3f;

    private readonly DeterministicRng _rng;
    private double _phase, _gearPhase;
    private float _load, _lp;

    /// <param name="scale">Chassis scale: 0.72 light .. 1.3 assault.</param>
    public ServoSynth(float scale, int rate = 22050, uint seed = 1)
    {
        Rate = rate;
        BaseHz = 300f / MathF.Pow(scale, 1.3f);
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
        float lpA = 1f - MathF.Exp(-2f * MathF.PI * 2400f / Rate);

        for (int i = 0; i < output.Length; i++)
        {
            _load += smooth * (target - _load);
            if (_load < 1e-4f)
            {
                output[i] = 0f;
                continue;
            }
            float f = BaseHz * (0.55f + 0.45f * _load);
            _phase = (_phase + f / Rate) % 1.0;
            _gearPhase = (_gearPhase + f * 0.31 / Rate) % 1.0;

            // Band-limited-ish saw from five harmonics: a motor, not a pure beep.
            float ph = (float)(_phase * 2 * Math.PI), tone = 0f;
            for (int h = 1; h <= 5; h++) tone += MathF.Sin(ph * h) / h;

            // Gear chatter: noise gated by the gear-mesh rate.
            float chatter = _gearPhase < 0.25 ? (_rng.NextFloat() * 2f - 1f) * 0.5f : 0f;

            float y = (tone * 0.6f + chatter) * _load;
            _lp += lpA * (y - _lp);
            output[i] = _lp * Gain;
        }
    }
}
