using System;
using IronFeud.Logic.Audio;

namespace IronFeud.Tests;

public class AudioTests
{
    private static float Peak(ReadOnlySpan<float> x)
    {
        float p = 0f;
        foreach (float v in x) p = MathF.Max(p, MathF.Abs(v));
        return p;
    }

    private static float Rms(ReadOnlySpan<float> x)
    {
        double s = 0;
        foreach (float v in x) s += v * v;
        return (float)Math.Sqrt(s / Math.Max(1, x.Length));
    }

    /// <summary>Zero crossings per second: a cheap brightness measure.</summary>
    private static float CrossingRate(float[] x, int rate)
    {
        int n = 0;
        for (int i = 1; i < x.Length; i++) if (x[i - 1] < 0f != x[i] < 0f) n++;
        return n * (float)rate / x.Length;
    }

    [Fact]
    public void OneShots_AreNormalised_AndNeverClip()
    {
        foreach (var x in new[]
                 {
                     SynthKit.Footfall(0.72f, 1), SynthKit.Footfall(1.3f, 2), SynthKit.GunReport(0.5f, 3), SynthKit.GunReport(2.3f, 4),
                     SynthKit.Pulse(0.8f, 5), SynthKit.LaserBurn(1.2f, 1.25f, 6), SynthKit.LaserBurn(1.5f, 2f, 9), SynthKit.Impact(0.3f, false, 7), SynthKit.Impact(3f, true, 8),
                 })
        {
            Assert.InRange(Peak(x), 0.89f, 0.901f);
            Assert.All(x, v => Assert.False(float.IsNaN(v)));
        }
    }

    [Fact]
    public void Footfalls_AreDeterministic_ButVariantsDiffer()
    {
        Assert.Equal(SynthKit.Footfall(1f, 3), SynthKit.Footfall(1f, 3));
        Assert.NotEqual(SynthKit.Footfall(1f, 3), SynthKit.Footfall(1f, 4));
    }

    [Fact]
    public void HeavierMechs_StepLower_AndLonger()
    {
        float[] light = SynthKit.Footfall(0.72f, 1), assault = SynthKit.Footfall(1.3f, 1);
        Assert.True(assault.Length > light.Length);
        Assert.True(CrossingRate(assault, SynthKit.Rate) < 0.8f * CrossingRate(light, SynthKit.Rate),
            $"assault {CrossingRate(assault, SynthKit.Rate):F0}/s vs light {CrossingRate(light, SynthKit.Rate):F0}/s");
    }

    [Fact]
    public void BiggerGuns_RingLonger()
    {
        Assert.True(SynthKit.GunReport(2.3f, 1).Length > 1.5f * SynthKit.GunReport(0.5f, 1).Length);
    }

    [Fact]
    public void Servo_IsSilentAtRest_AndWhinesUnderLoad()
    {
        var servo = new ServoSynth(1f);
        var buf = new float[servo.Rate / 2];
        servo.Render(buf, 0f);
        Assert.Equal(0f, Peak(buf));

        servo.Render(buf, 6f);
        Assert.True(Rms(buf.AsSpan(buf.Length / 2)) > 0.02f);
        Assert.True(Peak(buf) <= 1f);

        servo.Render(buf, 0f);
        servo.Render(buf, 0f);
        Assert.True(Peak(buf.AsSpan(buf.Length / 2)) < 1e-3f, "fades out once the joints stop");
    }

    [Fact]
    public void Jets_RoarOnlyWhileBurning()
    {
        var jet = new JetSynth(1f);
        var buf = new float[jet.Rate / 2];
        jet.Render(buf, false);
        Assert.Equal(0f, Peak(buf));
        jet.Render(buf, true);
        Assert.True(Rms(buf.AsSpan(buf.Length / 2)) > 0.03f);
        Assert.True(Peak(buf) < 1f);
        jet.Render(buf, false); // 150 ms release: still audible a quarter second after the cut
        jet.Render(buf, false);
        Assert.True(Peak(buf.AsSpan(buf.Length / 2)) < 1e-2f, "silent a second after the burn stops");
    }

    [Fact]
    public void LighterMechs_WhineHigher()
    {
        Assert.True(new ServoSynth(0.72f).BaseHz > new ServoSynth(1.3f).BaseHz * 1.8f);
    }

    /// <summary>A pulse laser's burst: <paramref name="n"/> copies <paramref name="gap"/> seconds apart, as the game mixes them.</summary>
    private static float[] Burst(float[] pulse, int n, float gap)
    {
        int step = (int)(gap * SynthKit.Rate);
        var x = new float[pulse.Length + step * (n - 1)];
        for (int k = 0; k < n; k++)
            for (int i = 0; i < pulse.Length; i++) x[k * step + i] += pulse[i] * 0.7f;
        return x;
    }

    [Fact]
    public void LaserBurn_LastsTheBurnPlusItsTail_AndStaysLowPitched()
    {
        var x = SynthKit.LaserBurn(1.2f, 1.25f, 1);
        Assert.Equal((int)((1.25f + SynthKit.BurnTail) * SynthKit.Rate), x.Length);
        Assert.True(Rms(x.AsSpan(SynthKit.Rate / 2, SynthKit.Rate / 2)) > 0.15f, "sustains through the burn");
        // A drone, not a whistle: well under 1 kHz worth of zero crossings mid-burn.
        Assert.True(CrossingRate(x[(SynthKit.Rate / 2)..SynthKit.Rate], SynthKit.Rate) < 1000f);
    }

    /// <summary>Opt-in: IRONFEUD_AUDIO_DUMP=dir writes every synthesised sound as a WAV for listening and spectrograms.</summary>
    [Fact]
    public void DumpSounds()
    {
        string? dir = Environment.GetEnvironmentVariable("IRONFEUD_AUDIO_DUMP");
        if (string.IsNullOrEmpty(dir)) return;
        System.IO.Directory.CreateDirectory(dir);
        void Save(string name, float[] x, int rate = SynthKit.Rate)
        {
            using var w = new System.IO.BinaryWriter(System.IO.File.Create(System.IO.Path.Combine(dir, name + ".wav")));
            w.Write("RIFF"u8); w.Write(36 + x.Length * 2); w.Write("WAVEfmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16); w.Write("data"u8); w.Write(x.Length * 2);
            foreach (float v in x) w.Write((short)(Math.Clamp(v, -1f, 1f) * 32767));
        }
        foreach (var (cls, s) in new[] { ("light", 0.72f), ("medium", 1f), ("heavy", 1.15f), ("assault", 1.3f) })
        {
            for (uint k = 0; k < 3; k++) Save($"footfall_{cls}_{k}", SynthKit.Footfall(s, k + 1));
            var servo = new ServoSynth(s);
            var buf = new float[servo.Rate * 3];
            for (int i = 0; i < 6; i++) servo.Render(buf.AsSpan(i * buf.Length / 6, buf.Length / 6), i % 2 == 0 ? 5f : 1.5f);
            Save($"servo_{cls}", buf, servo.Rate);
        }
        foreach (var (name, w) in new[] { ("autocannon", 0.53f), ("cannon", 1f), ("heavy_cannon", 1.51f), ("siege_cannon", 2.31f) })
            Save($"gun_{name}", SynthKit.GunReport(w, 1));
        Save("pulse_laser_burst", Burst(SynthKit.Pulse(0.8f, 1), 3, 0.09f));
        Save("burn_laser", SynthKit.LaserBurn(1.2f, 1.25f, 1));
        Save("burn_heavy_laser", SynthKit.LaserBurn(1.51f, 2f, 1));
        Save("impact_ground_shell", SynthKit.Impact(1f, false, 1));
        Save("impact_mech_shell", SynthKit.Impact(1f, true, 1));
        Save("impact_mech_destroyed", SynthKit.Impact(3f, true, 1));
    }
}
