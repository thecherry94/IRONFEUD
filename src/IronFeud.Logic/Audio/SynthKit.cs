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

    /// <summary>Seconds of discharge tail after a burn ends.</summary>
    public const float BurnTail = 0.35f;

    /// <summary>
    /// A burning laser, <paramref name="burn"/> seconds plus <see cref="BurnTail"/>: an ignition thump and a short
    /// bright flare while the drone spins up, then a deep sustained drone (three detuned band-limited saws over a sine
    /// sub-octave through a resonant low-pass that wobbles slightly, with a pulsing tremolo) and electric sizzle on
    /// top; when the burn ends the pitch sags, the filter closes and the drone dies away.
    /// Heavier lasers are lower and darker. Deliberately no fast pitch sweep: that is what makes a "pew".
    /// </summary>
    public static float[] LaserBurn(float weight, float burn, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float w = MathF.Max(0.3f, weight);
        var x = new float[(int)((burn + BurnTail) * Rate)];
        float f0 = 55f / MathF.Pow(w, 0.3f) * rng.Range(0.97f, 1.03f), bright = 1300f / MathF.Pow(w, 0.4f);
        const float charge = 0.08f;

        double p1 = 0, p2 = 0, p3 = 0, sub = 0;
        Svf filter = new(), fizz1 = new(), fizz2 = new();
        float grain = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate, after = t - burn;
            float pitch = t < charge ? 0.6f + 0.4f * t / charge : after > 0f ? 0.65f + 0.35f * MathF.Exp(-after / 0.08f) : 1f;
            float f = f0 * pitch * (1f + 0.008f * MathF.Sin(MathF.Tau * 2.7f * t));
            float saw = (Saw(ref p1, f) + Saw(ref p2, f * 1.007f) + Saw(ref p3, f * 0.993f)) / 3f;
            sub += 2 * Math.PI * f * 0.5 / Rate;
            float drone = saw * 0.8f + (float)Math.Sin(sub) * 0.6f;

            float open = t < charge ? 1f + 1.5f * (1f - t / charge) : after > 0f ? MathF.Exp(-after / 0.1f) : 1f + 0.2f * MathF.Sin(MathF.Tau * 6.5f * t);
            float y = filter.Low(drone, 120f + bright * open, 0.35f);

            // Electric sizzle: band-passed noise in crackling grains.
            if (rng.NextFloat() < 90f / Rate) grain = rng.Range(0.3f, 1f);
            grain *= 0.9985f;
            float sizzle = fizz2.Band(fizz1.Band(Noise(rng), 3500f, 0.7f), 3500f, 0.7f) * grain * 0.12f;

            float env = (1f - MathF.Exp(-t / 0.004f)) * (after > 0f ? MathF.Exp(-after / 0.12f) : 1f);
            float tremolo = 1f - 0.18f * (0.5f + 0.5f * MathF.Sin(MathF.Tau * 17f * t));
            x[i] = (y * tremolo + sizzle) * env;
        }
        Sweep(x, 0, 150f / MathF.Pow(w, 0.3f), 45f, 0.03f, 0.1f, 0.9f);
        Sweep(x, (int)(burn * Rate), 90f, 40f, 0.03f, 0.08f, 0.4f);
        return Finish(x, drive: 1.6f);
    }

    /// <summary>
    /// One pulse of a pulse laser: a short, heavy bolt (a saw burst whose filter snaps shut, a sub thump and a crack
    /// of sizzle). Bursts of these overlap into a "thud-thud-thud" rather than a "pew".
    /// </summary>
    public static float[] Pulse(float weight, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float w = MathF.Max(0.3f, weight);
        var x = new float[(int)(0.25f * Rate)];
        float f0 = 85f / MathF.Pow(w, 0.3f) * rng.Range(0.96f, 1.04f);
        double p1 = 0, p2 = 0;
        Svf filter = new(), fizz1 = new(), fizz2 = new();
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate;
            float f = f0 * (1f + 0.5f * MathF.Exp(-t / 0.02f));
            float saw = (Saw(ref p1, f) + Saw(ref p2, f * 1.01f)) * 0.5f;
            float y = filter.Low(saw, 150f + 2600f * MathF.Exp(-t / 0.035f), 0.3f);
            float sizzle = fizz2.Band(fizz1.Band(Noise(rng), 3200f, 0.7f), 3200f, 0.7f) * MathF.Exp(-t / 0.02f) * 0.2f;
            x[i] = (y + sizzle) * (1f - MathF.Exp(-t / 0.001f)) * MathF.Exp(-t / 0.07f);
        }
        Sweep(x, 0, 160f, 55f, 0.02f, 0.06f, 0.9f);
        return Finish(x, drive: 1.6f);
    }

    /// <summary>
    /// A missile leaving its tube: a short ignition thump and a rushing whoosh (band-passed noise whose band rises as
    /// the motor spools up), heavier rounds lower and longer.
    /// </summary>
    public static float[] MissileLaunch(float weight, uint seed)
    {
        var rng = new DeterministicRng(seed);
        float w = MathF.Max(0.3f, weight), len = 0.35f + 0.25f * MathF.Sqrt(w);
        var x = new float[(int)(len * Rate)];
        Svf band = new();
        for (int i = 0; i < x.Length; i++)
        {
            float t = i / (float)Rate, u = t / len;
            float fc = (500f + 2200f * MathF.Sqrt(u)) / MathF.Pow(w, 0.3f);
            float env = (1f - MathF.Exp(-t / 0.01f)) * MathF.Pow(1f - u, 1.5f);
            x[i] = band.Band(Noise(rng), fc, 0.5f) * env * 2.5f;
        }
        Sweep(x, 0, 120f, 50f, 0.02f, 0.05f, 0.6f);
        return Finish(x, drive: 1.5f);
    }

    /// <summary>A point-defense burst: five sharp cracks 25 ms apart over a short mechanical rattle.</summary>
    public static float[] PointDefenseBurst(uint seed)
    {
        var rng = new DeterministicRng(seed);
        var x = new float[(int)(0.22f * Rate)];
        OnePole hp = new(3000f);
        for (int k = 0; k < 5; k++)
        {
            int start = (int)((k * 0.025f + rng.Range(0f, 0.004f)) * Rate);
            for (int i = start; i < x.Length; i++)
            {
                float t = (i - start) / (float)Rate, n = Noise(rng);
                x[i] += (n - hp.Low(n)) * MathF.Exp(-t / 0.006f) * 1.5f;
            }
        }
        Sweep(x, 0, 220f, 140f, 0.01f, 0.03f, 0.3f);
        return Finish(x, drive: 1.3f);
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

    /// <summary>State-variable filter (Chamberlin). Stable for cutoffs well under a sixth of the sample rate.</summary>
    private struct Svf
    {
        private float _low, _band;

        /// <param name="damping">1/Q: 0.3 rings, 1.4 is flat.</param>
        public float Low(float x, float cutoffHz, float damping)
        {
            Run(x, cutoffHz, damping);
            return _low;
        }

        public float Band(float x, float cutoffHz, float damping)
        {
            Run(x, cutoffHz, damping);
            return _band;
        }

        private void Run(float x, float cutoffHz, float damping)
        {
            float f = 2f * MathF.Sin(MathF.PI * MathF.Min(cutoffHz, Rate / 7f) / Rate);
            _low += f * _band;
            _band += f * (x - _low - damping * _band);
        }
    }

    /// <summary>Band-limited sawtooth (PolyBLEP): no aliasing hiss on low, bright drones.</summary>
    private static float Saw(ref double phase, float hz)
    {
        double dt = hz / Rate;
        phase += dt;
        if (phase >= 1.0) phase -= 1.0;
        float t = (float)phase, d = (float)dt, y = 2f * t - 1f;
        if (t < d) { t /= d; y -= t + t - t * t - 1f; }
        else if (t > 1f - d) { t = (t - 1f) / d; y -= t * t + t + t + 1f; }
        return y;
    }

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
