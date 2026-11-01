using System;
using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Audio;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using IronFeud.Mech;

namespace IronFeud.Audio;

/// <summary>
/// Battle sound: footfalls, weapon reports, impacts and blow-ups as pooled 3D one-shots, plus a
/// <see cref="ServoVoice"/> on every mech. Each sound is a set of variants: synthesised (<see cref="SynthKit"/>),
/// replaced by recordings when <c>res://assets/audio/&lt;set&gt;/</c> holds any. Far sounds arrive late
/// (speed of sound), so a distant cannon flashes before it is heard.
/// </summary>
public partial class AudioDirector : Node3D
{
    private const float SpeedOfSound = 343f;
    private const int Voices = 48;
    private const int Variants = 6;

    private readonly AudioStreamPlayer3D[] _players = new AudioStreamPlayer3D[Voices];
    private readonly double[] _startedAt = new double[Voices];
    private readonly Dictionary<string, AudioStream[]> _sets = new();
    private readonly List<(double At, Action Play)> _pending = new();
    private readonly DeterministicRng _rng = new(1234);
    private double _time;

    /// <summary>Burning beams: the voice playing each (keyed per shooter weapon) and when its last tick arrived.</summary>
    private readonly Dictionary<int, Burn> _burns = new();

    private sealed class Burn
    {
        public AudioStreamPlayer3D Player = null!;
        public AudioStream Stream = null!;
        public double Seen;
        public float Length;
    }

    /// <summary>One-shots playing right now (debug readout).</summary>
    public int Active { get; private set; }

    public override void _Ready()
    {
        TopLevel = true;
        for (int i = 0; i < Voices; i++)
        {
            _players[i] = new AudioStreamPlayer3D { MaxDistance = 900f, PanningStrength = 0.8f };
            AddChild(_players[i]);
        }
        // Many overlapping blasts must not clip the mix.
        if (AudioServer.GetBusEffectCount(0) == 0) AudioServer.AddBusEffect(0, new AudioEffectHardLimiter { CeilingDb = -0.3f });
        Prewarm();
    }

    /// <summary>Synthesises the whole roster up front (~0.5 s) so the first siege-cannon shot doesn't hitch.</summary>
    private void Prewarm()
    {
        foreach (var cls in Enum.GetValues<ChassisClass>()) FootfallSet(MechChassis.Create(cls, MechModel.Hauberk));
        foreach (var spec in new[] { WeaponSpec.PulseLaser, WeaponSpec.Autocannon, WeaponSpec.Laser, WeaponSpec.Cannon,
                     WeaponSpec.HeavyLaser, WeaponSpec.HeavyCannon, WeaponSpec.SiegeCannon })
            if (spec.BurnTime > 0f) BurnSet(spec);
            else ShotSet(spec);
    }

    public override void _ExitTree()
    {
        foreach (var p in _players)
        {
            p.Stop();
            p.Stream = null;
        }
        foreach (var set in _sets.Values)
            foreach (var s in set) s.Dispose();
        _sets.Clear();
        _pending.Clear();
        _burns.Clear();
    }

    /// <summary>Gives a mech its servo voice. Call once per mech view.</summary>
    public void Attach(MechNode mech) => mech.AddChild(new ServoVoice { Mech = mech });

    public void Footfall(MechNode mech, in Footfall f)
    {
        var c = mech.Sim.Chassis;
        float amp = Mathf.Clamp(0.35f + 0.65f * f.Strength, 0.2f, 1f) * (0.7f + 0.3f * f.SpeedFraction);
        Play(FootfallSet(c), f.Position.ToGodot(), Mathf.LinearToDb(amp), unitSize: 14f * c.Scale);
    }

    /// <summary>A shell fired, an instant beam or one pulse of a pulse laser.</summary>
    public void Shot(WeaponSpec spec, Vector3 at)
    {
        float w = Weight(spec);
        Play(ShotSet(spec), at, Mathf.LinearToDb(Mathf.Clamp(0.6f + 0.25f * w, 0.5f, 1.2f)), unitSize: 30f * w, delayed: true);
    }

    /// <summary>
    /// One tick of a burning beam. The start plays the whole burn sound (ignition, drone, discharge) and later ticks
    /// keep it at the muzzle; if the ticks stop early (arm shot off, mech destroyed) it skips to the discharge.
    /// </summary>
    public void BeamHold(int key, WeaponSpec spec, Vector3 at, bool start)
    {
        if (start)
        {
            var streams = BurnSet(spec);
            var p = Voice();
            p.Stream = streams[(int)(_rng.NextUInt() % (uint)streams.Length)];
            p.GlobalPosition = at;
            p.VolumeDb = 0f;
            p.UnitSize = 30f * Weight(spec);
            p.PitchScale = _rng.Range(0.97f, 1.03f) * (float)Engine.TimeScale;
            p.Play();
            _burns[key] = new Burn { Player = p, Stream = p.Stream, Seen = _time, Length = spec.BurnTime };
        }
        else if (_burns.TryGetValue(key, out var b))
        {
            b.Player.GlobalPosition = at;
            b.Seen = _time;
        }
    }

    /// <param name="onMech">Struck armour rings; the ground thuds.</param>
    public void Impact(WeaponSpec spec, Vector3 at, bool onMech)
    {
        float size = Weight(spec) * (spec.Kind == WeaponKind.Projectile ? 1f : 0.35f);
        string set = (onMech ? "impact_mech_" : "impact_ground_") + (size > 1.4f ? "big" : size > 0.6f ? "mid" : "small");
        float s = size > 1.4f ? 2f : size > 0.6f ? 1f : 0.35f;
        Play(Set(set, k => SynthKit.Impact(s, onMech, k)), at, Mathf.LinearToDb(Mathf.Clamp(0.5f + 0.3f * size, 0.4f, 1.1f)),
            unitSize: 20f * Mathf.Max(0.5f, size), delayed: true);
    }

    /// <summary>A section blown off or the mech destroyed.</summary>
    public void OnDamaged(MechNode mech, in DamageReport r, Vector3 at)
    {
        if (!r.SectionDestroyed) return;
        bool wreck = r.Applied == MechSection.Torso;
        Vector3 pos = wreck ? mech.Sim.Pose.HitVolumes[(int)MechSection.Torso].Center.ToGodot() : at;
        Play(Set(wreck ? "blowup_mech" : "blowup_section", k => SynthKit.Impact(wreck ? 3f : 1.8f, true, k)), pos,
            wreck ? 2f : 0f, unitSize: wreck ? 80f : 40f, delayed: true);
    }

    public override void _Process(double delta)
    {
        _time += delta;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].At > _time) continue;
            _pending[i].Play();
            _pending.RemoveAt(i);
        }
        int active = 0;
        foreach (var p in _players) if (p.Playing) active++;
        Active = active;

        foreach (var (key, b) in _burns)
        {
            bool alive = b.Player.Playing && b.Player.Stream == b.Stream;
            float at = alive ? b.Player.GetPlaybackPosition() : 0f;
            if (alive && at < b.Length - 0.05f && _time - b.Seen > 0.06) b.Player.Seek(b.Length); // cut short: discharge now
            else if (alive && at < b.Length) continue;
            _burns.Remove(key);
        }
    }

    private void Play(AudioStream[] streams, Vector3 at, float volumeDb, float unitSize, bool delayed = false)
    {
        var stream = streams[(int)(_rng.NextUInt() % (uint)streams.Length)];
        float pitch = _rng.Range(0.94f, 1.06f) * (float)Engine.TimeScale;
        void Start()
        {
            var p = Voice();
            p.Stream = stream;
            p.GlobalPosition = at;
            p.VolumeDb = volumeDb;
            p.UnitSize = unitSize;
            p.PitchScale = pitch;
            p.Play();
        }

        var cam = GetViewport().GetCamera3D();
        float d = cam != null ? cam.GlobalPosition.DistanceTo(at) : 0f;
        if (delayed && d > 30f) _pending.Add((_time + d / SpeedOfSound, Start));
        else Start();
    }

    /// <summary>A free player, or the one that has been playing longest (never one carrying a live burn).</summary>
    private AudioStreamPlayer3D Voice()
    {
        int best = -1;
        for (int i = 0; i < Voices; i++)
        {
            if (!_players[i].Playing) { best = i; break; }
            if (IsBurning(_players[i])) continue;
            if (best < 0 || _startedAt[i] < _startedAt[best]) best = i;
        }
        best = Math.Max(0, best);
        _startedAt[best] = _time;
        return _players[best];
    }

    private bool IsBurning(AudioStreamPlayer3D p)
    {
        foreach (var b in _burns.Values) if (b.Player == p) return true;
        return false;
    }

    private AudioStream[] FootfallSet(MechChassis c) =>
        Set("footfall_" + c.Class.ToString().ToLowerInvariant(), k => SynthKit.Footfall(c.Scale, k));

    private AudioStream[] ShotSet(WeaponSpec spec)
    {
        float w = Weight(spec);
        return spec.Kind == WeaponKind.Projectile
            ? Set("gun_" + Key(spec), k => SynthKit.GunReport(w, k))
            : Set("pulse_" + Key(spec), k => SynthKit.Pulse(w, k));
    }

    private AudioStream[] BurnSet(WeaponSpec spec)
    {
        float w = Weight(spec);
        return Set("burn_" + Key(spec), k => SynthKit.LaserBurn(w, spec.BurnTime, k));
    }

    /// <summary>Recordings from res://assets/audio/&lt;name&gt;/ if there are any, else synthesised variants.</summary>
    private AudioStream[] Set(string name, Func<uint, float[]> synth)
    {
        if (_sets.TryGetValue(name, out var cached)) return cached;
        var streams = Samples(name);
        if (streams.Count == 0)
            for (uint k = 1; k <= Variants; k++) streams.Add(ToStream(synth(k), SynthKit.Rate));
        return _sets[name] = streams.ToArray();
    }

    private static List<AudioStream> Samples(string name)
    {
        var list = new List<AudioStream>();
        using var dir = DirAccess.Open($"res://assets/audio/{name}");
        if (dir == null) return list;
        foreach (string file in dir.GetFiles())
        {
            // Exported builds only list the .import stubs.
            string f = file.EndsWith(".import") ? file[..^7] : file;
            if (f.EndsWith(".wav") || f.EndsWith(".ogg"))
                if (ResourceLoader.Load<AudioStream>($"res://assets/audio/{name}/{f}") is { } s && !list.Contains(s)) list.Add(s);
        }
        return list;
    }

    private static AudioStreamWav ToStream(float[] x, int rate)
    {
        var data = new byte[x.Length * 2];
        for (int i = 0; i < x.Length; i++)
        {
            short v = (short)(Math.Clamp(x[i], -1f, 1f) * 32767f);
            data[2 * i] = (byte)v;
            data[2 * i + 1] = (byte)(v >> 8);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Stereo = false, Data = data };
    }

    /// <summary>Sound weight relative to the cannon (1), the same scale the visual effects use.</summary>
    private static float Weight(WeaponSpec spec) => Mathf.Sqrt(spec.Damage / WeaponSpec.Cannon.Damage);

    private static string Key(WeaponSpec spec) => spec.Name.Replace(" ", "_").ToLowerInvariant();
}
