using Godot;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>Status readout (throttle, speed, twist, heat, weapons), controls help and the convergence reticle.</summary>
public partial class Hud : CanvasLayer
{
    public MechNode? Mech;
    public CameraRig? Camera;
    public PlayerMechController? Pilot;
    public GaitDebugDraw? Debug;

    private Label _stat = null!;
    private ColorRect _heatBar = null!;
    private Reticle _reticle = null!;
    private double _uiTimer;

    private const float HeatBarWidth = 180f;
    private static readonly Color HeatColor = new("ff8a2a"), HeatLockedColor = new("ff3a2a");

    private const string Help =
        "W/S throttle (holds) · X full stop · A/D turn legs · mouse twist & aim · arrows fine twist/aim · C center torso\n" +
        "LMB/Space cannon · RMB/F laser · 1/2/3 chase / cockpit / orbit · F3 gait debug · P pause · O slow-mo · Esc release mouse";

    private partial class Reticle : Control
    {
        private bool _hot;

        public bool Hot
        {
            get => _hot;
            set { if (_hot != value) { _hot = value; QueueRedraw(); } }
        }

        public override void _Draw()
        {
            var c = _hot ? new Color(1f, 0.35f, 0.23f, 0.9f) : new Color(0.49f, 1f, 0.69f, 0.85f);
            DrawArc(Vector2.Zero, 15f, 0f, Mathf.Tau, 32, c, 2f, true);
            DrawCircle(Vector2.Zero, 2f, c);
        }
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _stat = new Label { Position = new Vector2(10, 10) };
        _stat.AddThemeColorOverride("font_color", new Color("e8e4dc"));
        AddChild(_stat);

        AddChild(new ColorRect { Position = new Vector2(10, 100), Size = new Vector2(HeatBarWidth, 5), Color = new Color(1, 1, 1, 0.1f) });
        _heatBar = new ColorRect { Position = new Vector2(10, 100), Size = new Vector2(0, 5), Color = HeatColor };
        AddChild(_heatBar);

        var help = new Label { Text = Help, HorizontalAlignment = HorizontalAlignment.Center };
        help.AddThemeFontSizeOverride("font_size", 13);
        help.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        help.GrowHorizontal = Control.GrowDirection.Both;
        help.Position += new Vector2(0, -12);
        AddChild(help);

        _reticle = new Reticle { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_reticle);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Debug != null && e.IsActionPressed("debug_draw")) Debug.Visible = !Debug.Visible;
    }

    public override void _Process(double delta)
    {
        if (Camera == null || Pilot == null || Mech?.Sim is not { } sim) return;
        var weapons = sim.Weapons;

        Vector3 aim = Pilot.AimPoint;
        bool visible = Camera.Mode != CameraMode.Orbit && !Camera.IsPositionBehind(aim);
        _reticle.Visible = visible;
        if (visible) _reticle.Position = Camera.UnprojectPosition(aim);
        _reticle.Hot = weapons.HeatLocked;

        _uiTimer += delta;
        if (_uiTimer < 0.1) return;
        _uiTimer = 0;

        var body = sim.Locomotion.Body;
        string heat = weapons.HeatLocked ? "LOCKED" : $"{Mathf.RoundToInt(Mathf.Min(1f, weapons.Heat) * 100)}%";
        var text = $"THR {Mathf.RoundToInt(Pilot.Throttle * 100)}% · {body.V * 3.6f:0.0} km/h\n" +
                   $"TWIST {Mathf.RoundToInt(Mathf.RadToDeg(-body.Twist))}° · HEAT {heat}\n";
        foreach (var m in weapons.Mounts)
            text += $"{m.Spec.Name.ToUpperInvariant()} {(m.Cooldown > 0f ? $"{m.Cooldown:0.0}s" : "READY")}   ";
        text += GetTree().Paused ? "\nPAUSED" : Engine.TimeScale < 1.0 ? "\nSLOW-MO" : "";
        _stat.Text = text;

        _heatBar.Size = new Vector2(HeatBarWidth * Mathf.Clamp(weapons.Heat, 0f, 1f), 5);
        _heatBar.Color = weapons.HeatLocked ? HeatLockedColor : HeatColor;
    }
}
