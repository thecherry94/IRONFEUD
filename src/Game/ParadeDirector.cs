using System.Collections.Generic;
using System.Text;
using Godot;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Gait showcase: each mech laps its own circle of a concentric track (one lane per class) on a shared throttle
/// cycle: walk → run up → full speed → back down to a walk → stop. Shows speed, duty and cadence per mech.
/// Writes only MechInput.
/// </summary>
public partial class ParadeDirector : CanvasLayer
{
    public readonly List<(MechNode Mech, float Radius)> Lanes = new();

    /// <summary>Throttle cycle (s): ramp up, hold, ramp down, walk, stop.</summary>
    private const float RampUp = 12f, Hold = 15f, RampDown = 6f, Walk = 7f, Stop = 5f;
    private const float Period = RampUp + Hold + RampDown + Walk + Stop;
    private const float WalkThrottle = 0.2f;

    private float _time;
    private Label _label = null!;

    public override void _Ready()
    {
        ProcessPhysicsPriority = -100; // before the battle tick
        _label = new Label { Position = new Vector2(10, 10) };
        _label.AddThemeColorOverride("font_color", new Color("e8e4dc"));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.6f));
        _label.AddThemeFontSizeOverride("font_size", 15);
        AddChild(_label);
    }

    /// <summary>Throttle at time t into the cycle.</summary>
    public static float ThrottleAt(float t)
    {
        t %= Period;
        if (t < RampUp) return MathUtil.Smoothstep(0f, RampUp, t);
        t -= RampUp;
        if (t < Hold) return 1f;
        t -= Hold;
        if (t < RampDown) return MathUtil.Lerp(1f, WalkThrottle, MathUtil.Smoothstep(0f, RampDown, t));
        t -= RampDown;
        return t < Walk ? WalkThrottle : 0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        float throttle = ThrottleAt(_time);
        foreach (var (mech, radius) in Lanes)
        {
            // Pure pursuit on the lane circle, centre on the mech's left.
            var body = mech.Locomotion.Body;
            var p = new System.Numerics.Vector2(body.Position.X, body.Position.Z);
            float angle = Mathf.Atan2(p.Y, p.X);
            float lookAhead = Mathf.Max(20f, 1.2f * Mathf.Abs(body.V)) / radius;
            var target = new System.Numerics.Vector3(Mathf.Cos(angle - lookAhead) * radius, 0f, Mathf.Sin(angle - lookAhead) * radius);
            float err = MathUtil.WrapAngle(MathUtil.Heading(target - body.Position) - body.Psi);
            mech.PilotInput = new MechInput { Throttle = throttle, Turn = Mathf.Clamp(err * 2.5f, -1f, 1f) };
        }
    }

    public override void _Process(double delta)
    {
        var sb = new StringBuilder();
        sb.Append($"GAIT PARADE · throttle {ThrottleAt(_time) * 100f:0}% · cycle {_time % Period:0}/{Period:0} s\n");
        foreach (var (mech, _) in Lanes)
        {
            var loco = mech.Locomotion;
            float v = loco.Body.V;
            var gait = loco.Gait;
            string phase = gait.Duty < 0.5f ? "run" : gait.Duty < 0.6f ? "jog" : "walk";
            sb.Append($"{loco.Chassis.Name,-8} {v * 3.6f,5:0} km/h   duty {gait.Duty:0.00} ({phase})   {gait.F:0.00} Hz · {2f * gait.F:0.0} steps/s\n");
        }
        _label.Text = sb.ToString();
    }
}
