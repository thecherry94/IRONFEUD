using System;
using Godot;
using IronFeud.Logic.Audio;
using IronFeud.Mech;

namespace IronFeud.Audio;

/// <summary>
/// A mech's own machinery, streamed into one 3D audio generator at hip height: its joint motors (a
/// <see cref="ServoSynth"/> fed with how fast its leg joints and torso twist turn each tick) and, while they burn, its
/// jump jets (<see cref="JetSynth"/>). Wrecks fall silent.
/// </summary>
public partial class ServoVoice : AudioStreamPlayer3D
{
    /// <summary>Set before the node enters the tree.</summary>
    public MechNode Mech = null!;

    private ServoSynth _synth = null!;
    private JetSynth _jets = null!;
    private float[] _jetBuf = Array.Empty<float>();
    private bool _burning;
    private AudioStreamGeneratorPlayback? _playback;
    private float[] _mono = Array.Empty<float>();
    private Vector2[] _frames = Array.Empty<Vector2>();
    private readonly float[] _last = new float[7];
    private bool _primed;
    private float _speed;

    public override void _Ready()
    {
        var c = Mech.Sim.Chassis;
        _synth = new ServoSynth(c.Scale, 22050, (uint)Mech.Unit.Id + 7u);
        _jets = new JetSynth(c.Scale, 22050, (uint)Mech.Unit.Id + 11u);
        Stream = new AudioStreamGenerator { MixRate = _synth.Rate, BufferLength = 0.12f };
        Position = new Vector3(0f, c.HipHeight, 0f);
        UnitSize = 10f * c.Scale;
        MaxDistance = 250f;
        VolumeDb = -10f;
        Play();
        _playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();
    }

    /// <summary>Summed joint speed in rad/s, measured per physics tick (the pose only changes there).</summary>
    public override void _PhysicsProcess(double delta)
    {
        var pose = Mech.Sim.Pose;
        Span<float> now = stackalloc float[7];
        now[0] = pose.Legs[0].Angles.HipPitch; now[1] = pose.Legs[0].Angles.Knee; now[2] = pose.Legs[0].Angles.Roll;
        now[3] = pose.Legs[1].Angles.HipPitch; now[4] = pose.Legs[1].Angles.Knee; now[5] = pose.Legs[1].Angles.Roll;
        now[6] = Mech.Locomotion.Body.Twist;

        float sum = 0f;
        if (_primed)
            for (int i = 0; i < 7; i++) sum += MathF.Abs(now[i] - _last[i]) * (i == 6 ? 1.5f : i % 3 == 2 ? 0.5f : 1f);
        now.CopyTo(_last);
        _primed = true;
        _speed = Mech.Sim.IsOut ? 0f : sum / (float)delta;
        _burning = Mech.Locomotion.JetsBurning;
    }

    public override void _Process(double delta)
    {
        if (_playback == null) return;
        int n = _playback.GetFramesAvailable();
        if (n <= 0) return;
        if (_mono.Length < n)
        {
            _mono = new float[n];
            _jetBuf = new float[n];
            _frames = new Vector2[n];
        }
        _synth.Render(_mono.AsSpan(0, n), _speed);
        _jets.Render(_jetBuf.AsSpan(0, n), _burning);
        // The generator plays 10 dB down (servos are a background layer); jets get that back and more.
        for (int i = 0; i < n; i++)
        {
            float v = _mono[i] + 4f * _jetBuf[i];
            _frames[i] = new Vector2(v, v);
        }
        _playback.PushBuffer(n == _frames.Length ? _frames : _frames[..n]);
    }
}
