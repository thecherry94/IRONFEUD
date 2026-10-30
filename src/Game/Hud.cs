using Godot;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>Status readout, controls help and the torso-aim reticle.</summary>
public partial class Hud : CanvasLayer
{
    public MechNode? Mech;
    public CameraRig? Camera;
    public PlayerMechController? Pilot;
    public GaitDebugDraw? Debug;

    private Label _stat = null!;
    private Reticle _reticle = null!;
    private Vector3 _aimPoint;
    private double _uiTimer;

    private const string Help =
        "W/S throttle (holds) · X full stop · A/D turn legs · mouse twist & aim · arrows fine twist/aim · C center torso\n" +
        "1/2/3 chase / cockpit / orbit · F3 gait debug · P pause · O slow-mo · Esc release mouse";

    private partial class Reticle : Control
    {
        public override void _Draw()
        {
            var c = new Color(0.49f, 1f, 0.69f, 0.85f);
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

    public override void _PhysicsProcess(double delta)
    {
        if (Mech?.Rig is not { } rig) return;
        // Where the head is looking: the prototype's reticle is the cockpit boresight, not the cursor.
        Transform3D mount = rig.CamMount.GlobalTransform;
        Vector3 from = mount.Origin, dir = -mount.Basis.Z.Normalized();
        var query = PhysicsRayQueryParameters3D.Create(from, from + dir * 400f);
        var hit = Mech.GetWorld3D().DirectSpaceState.IntersectRay(query);
        _aimPoint = hit.Count > 0 ? (Vector3)hit["position"] : from + dir * 400f;
    }

    public override void _Process(double delta)
    {
        if (Camera == null || Mech?.Locomotion is not { } loco) return;

        bool visible = Camera.Mode != CameraMode.Orbit && !Camera.IsPositionBehind(_aimPoint);
        _reticle.Visible = visible;
        if (visible) _reticle.Position = Camera.UnprojectPosition(_aimPoint);

        _uiTimer += delta;
        if (_uiTimer < 0.1) return;
        _uiTimer = 0;
        float throttle = Pilot?.Throttle ?? 0f;
        _stat.Text =
            $"THR {Mathf.RoundToInt(throttle * 100)}% · {loco.Body.V * 3.6f:0.0} km/h\n" +
            $"TWIST {Mathf.RoundToInt(Mathf.RadToDeg(-loco.Body.Twist))}°" +
            (GetTree().Paused ? " · PAUSED" : Engine.TimeScale < 1.0 ? " · SLOW-MO" : "");
    }
}
