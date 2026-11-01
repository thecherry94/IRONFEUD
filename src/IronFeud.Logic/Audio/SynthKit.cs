using System;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Audio;

/// <summary>
/// Offline synthesis of one-shot sounds as mono float samples in -1..1 at <see cref="Rate"/>.
/// These are the procedural layers (the parts a recording can't scale per class) and stand-ins until sample sets
/// exist. Everything is seeded, so a variant index always gives the same sound.
/// </summary>
public static class SynthKit
{
    public const int Rate = 44100;

    /// <summary>
    /// A foot landing: a long sub-bass drop (the weight), a short punch an octave up, a dull two-pole noise burst (the
    /// ground giving) and low debris settling. No tonal metal ring: struck-metal modes read as a cooking pot.
    /// Bigger mechs are lower, longer and duller.
    /// </summary>
    /// <param name="scale">Chassis scale: 0.72 light .. 1.3 assault.</param>
    public static float[] Footfall(float scale, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float s = scale;
        var x = new float[(int)((0.4f + 0.6f * s) * Rate)];

        float f0 = 72f / MathF.Pow(s, 1.2f) * rng.Range(0.94f, 1.06f);
        Sweep(x, 0, f0, f0 * 0.5f, 0.05f, 0.13f * MathF.Pow(s, 1.3f), 1f);
        Sweep(x, 0, f0 * 2f, f0, 0.015f, 0.04f * s, 0.45f);

        OnePole lp1 = new(500f / s), lp2 = new(500f / s);
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            x[i] += lp2.Low(lp1.Low(Noise(rng))) * (1f - MathF.Exp(-t / 0.003f)) * MathF.Exp(-t / (0.05f * s)) * 4f;
        }

        Debris(x, rng, 0.05f, 0.4f * s, density: 40f, amp: 0.2f, cutoff: 1400f / s);
        return Finish(x, drive: 1.8f);
    }

    /// <summary>
    /// A gun report: a sharp supersonic crack, a low boom with a falling pitch and a long rumbling tail.
    /// <paramref name="weight"/> 1 is the cannon; 0.5 an autocannon round, 2.3 the siege cannon.
    /// </summary>
    public static float[] GunReport(float weight, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float w = MathF.Max(0.3f, weight), sq = MathF.Sqrt(w);
        var x = new float[(int)((0.35f + 0.9f * sq) * Rate)];

        var hp = new OnePole(1800f);
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            float n = Noise(rng);
            x[i] += (n - hp.Low(n)) * MathF.Exp(-t / (0.012f + 0.006f * sq)) * 1.2f;
        }

        Sweep(x, 0, 70f / MathF.Pow(w, 0.3f), 32f, 0.06f, 0.16f * sq, 1.1f);

        var lp = new OnePole(260f / MathF.Pow(w, 0.25f));
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            float env = (1f - MathF.Exp(-t / 0.01f)) * MathF.Exp(-t / (0.22f * sq));
            x[i] += lp.Low(Noise(rng)) * env * 2.2f;
        }
        Debris(x, rng, 0.08f, 0.6f * sq, density: 25f, amp: 0.12f, cutoff: 1500f);
        return Finish(x, drive: 2.2f);
    }

    /// <summary>
    /// A beam discharge: a falling electric tone with a sub-octave, a buzzing crackle riding on it and a short
    /// capacitor thump. <paramref name="weight"/> 1 is the laser; heavier beams are lower and longer.
    /// </summary>
    public static float[] Beam(float weight, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float w = MathF.Max(0.3f, weight), len = 0.22f + 0.28f * MathF.Sqrt(w);
        var x = new float[(int)((len + 0.15f) * Rate)];
        float fTop = 1900f / MathF.Pow(w, 0.4f) * rng.Range(0.95f, 1.05f), fEnd = fTop * 0.18f;

        double ph = 0, sub = 0;
        OnePole lp1 = new(2800f), lp2 = new(2800f);
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            float f = fEnd + (fTop - fEnd) * MathF.Exp(-t / (len * 0.35f));
            ph += 2 * Math.PI * f / Rate;
            sub += Math.PI * f / Rate;
            float env = (1f - MathF.Exp(-t / 0.002f)) * MathF.Exp(-t / (len * 0.45f));
            float tone = (float)Math.Sin(ph + 1.2 * Math.Sin(ph * 0.5)) * 0.5f + (float)Math.Sin(sub) * 0.3f;
            float gate = 0.5f + 0.5f * MathF.Sin(t * 2f * MathF.PI * 120f);
            float buzz = lp2.Low(lp1.Low(Noise(rng))) * gate * gate * 1.6f;
            x[i] = (tone + buzz) * env;
        }
        Sweep(x, 0, 140f, 60f, 0.02f, 0.05f, 0.6f);
        return Finish(x, drive: 1.4f);
    }

    /// <summary>
    /// Something hitting a mech or the ground. On a mech (<paramref name="metal"/>) it rings like struck armour;
    /// on the ground it is a dull burst with dirt falling back.
    /// <paramref name="size"/>: 0.3 a beam scorch, 1 a cannon shell, 3 a mech blowing apart.
    /// </summary>
    public static float[] Impact(float size, bool metal, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float z = MathF.Max(0.2f, size), sq = MathF.Sqrt(z);
        var x = new float[(int)((0.3f + 0.8f * sq) * Rate)];

        var lp = new OnePole(metal ? 3000f : 1200f / sq);
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            x[i] += lp.Low(Noise(rng)) * MathF.Exp(-t / (0.02f + 0.05f * sq)) * 2.5f;
        }
        Sweep(x, 0, 90f / MathF.Pow(z, 0.3f), 35f, 0.05f, 0.12f * sq, 0.9f);
        if (metal)
            Modes(x, rng, 0.002f, 320f / MathF.Pow(z, 0.25f), new[] { 1f, 1.73f, 2.41f, 3.17f, 4.6f },
                new[] { 0.35f, 0.25f, 0.2f, 0.12f, 0.08f }, 0.18f * sq);
        Debris(x, rng, 0.05f, 0.5f * sq, density: 40f * sq, amp: 0.3f, cutoff: 2200f);
        return Finish(x, drive: 1.8f);
    }

    // --- building blocks -----------------------------------------------------------------------------------------

    private struct OnePole
    {
        private readonly float _a;
        private float _y;

        public OnePole(float cutoffHz)
        {
            _a = 1f - MathF.Exp(-2f * MathF.PI * MathF.Min(cutoffHz, Rate * 0.45f) / Rate);
            _y = 0f;
        }

        public float Low(float x) => _y += _a * (x - _y);
    }

    private static float Noise(DeterministicRng rng) => rng.NextFloat() * 2f - 1f;

    /// <summary>Adds a sine whose pitch glides from f0 to f1 (time constant <paramref name="glide"/>), with a 2 ms attack.</summary>
    private static void Sweep(float[] x, int start, float f0, float f1, float glide, float decay, float amp)
    {
        double ph = 0;
        for (int i = start; i < x.Length; i++)
        {
            float t = (i - start) / (float)Rate;
            ph += 2 * Math.PI * (f1 + (f0 - f1) * MathF.Exp(-t / glide)) / Rate;
            x[i] += amp * (float)Math.Sin(ph) * (1f - MathF.Exp(-t / 0.002f)) * MathF.Exp(-t / decay);
        }
    }

    /// <summary>Struck-metal modes: inharmonic partials (detuned per seed), each decaying faster the higher it is.</summary>
    private static void Modes(float[] x, DeterministicRng rng, float at, float baseHz, float[] ratios, float[] amps, float decay)
    {
        int start = (int)(at * Rate);
        for (int m = 0; m < ratios.Length; m++)
        {
            float f = baseHz * ratios[m] * rng.Range(0.96f, 1.04f), d = decay / (1f + 0.6f * m);
            for (int i = start; i < x.Length; i++)
            {
                float t = (i - start) / (float)Rate;
                x[i] += amps[m] * MathF.Sin(MathF.Tau * f * t) * MathF.Exp(-t / d); // zero phase: no click
            }
        }
    }

    /// <summary>
    /// Sparse grains of low-passed noise (two poles, 1 ms attack, so they read as grit rather than digital clicks),
    /// thinning out between <paramref name="from"/> and <paramref name="to"/> seconds.
    /// </summary>
    private static void Debris(float[] x, DeterministicRng rng, float from, float to, float density, float amp, float cutoff)
    {
        OnePole lp1 = new(cutoff), lp2 = new(cutoff);
        int a = (int)(from * Rate), b = Math.Min(x.Length, (int)(to * Rate));
        float target = 0f, grain = 0f, attack = 1f - MathF.Exp(-1f / (0.001f * Rate)), release = MathF.Exp(-1f / (0.012f * Rate));
        for (int i = a; i < b; i++)
        {
            float u = (i - a) / (float)Math.Max(1, b - a);
            if (rng.NextFloat() < density * (1f - u) / Rate) target = amp * rng.Range(0.3f, 1f);
            grain += attack * (target - grain);
            target *= release;
            x[i] += lp2.Low(lp1.Low(Noise(rng) * grain)) * 3f;
        }
    }

    /// <summary>Soft saturation for punch, a 5 ms fade-out, then normalised to a 0.9 peak.</summary>
    private static float[] Finish(float[] x, float drive)
    {
        float peak = 1e-9f;
        int fade = Math.Min(x.Length, Rate / 200);
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = MathF.Tanh(drive * x[i]);
            if (i >= x.Length - fade) x[i] *= (x.Length - i) / (float)fade;
            peak = MathF.Max(peak, MathF.Abs(x[i]));
        }
        for (int i = 0; i < x.Length; i++) x[i] *= 0.9f / peak;
        return x;
    }
}
