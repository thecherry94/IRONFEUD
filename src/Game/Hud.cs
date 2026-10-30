using System.Collections.Generic;
using System.Text;
using Godot;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Status readout (throttle, speed, twist, heat, weapons, own sections), what the reticle is on,
/// a hit feed, controls help and the convergence reticle.
/// </summary>
public partial class Hud : CanvasLayer
{
    public MechNode? Mech;
    public CameraRig? Camera;
    public PlayerMechController? Pilot;
    public Battlefield? Battlefield;

    private Label _stat = null!, _target = null!, _feed = null!, _banner = null!, _bannerSub = null!;
    private ColorRect _heatBar = null!, _heatBack = null!;
    private Reticle _reticle = null!;
    private double _uiTimer, _time;
    private readonly List<(double Time, string Text)> _feedLines = new();

    private const float HeatBarWidth = 180f;
    private const double FeedLifetime = 8.0;
    private static readonly Color HeatColor = new("ff8a2a"), HeatLockedColor = new("ff3a2a");

    public string HelpText = "";

    private static readonly Dictionary<MechSection, string> SectionNames = new()
    {
        [MechSection.Torso] = "torso", [MechSection.ArmLeft] = "left arm", [MechSection.ArmRight] = "right arm",
        [MechSection.Legs] = "legs", [MechSection.Cockpit] = "cockpit",
    };

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
        Layer = 2; // above the AI panel, so the result banner is never hidden

        _heatBack = new ColorRect { Position = new Vector2(10, 8), Size = new Vector2(HeatBarWidth, 5), Color = new Color(1, 1, 1, 0.1f) };
        AddChild(_heatBack);
        _heatBar = new ColorRect { Position = new Vector2(10, 8), Size = new Vector2(0, 5), Color = HeatColor };
        AddChild(_heatBar);

        _stat = NewLabel(new Vector2(10, 16));
        _feed = NewLabel(new Vector2(10, 150));
        _feed.AddThemeFontSizeOverride("font_size", 13);

        _target = NewLabel(Vector2.Zero);
        _target.HorizontalAlignment = HorizontalAlignment.Center;
        _target.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _target.GrowHorizontal = Control.GrowDirection.Both;
        _target.Position += new Vector2(0, 12);

        var help = NewLabel(Vector2.Zero);
        help.Text = HelpText;
        help.HorizontalAlignment = HorizontalAlignment.Center;
        help.AddThemeFontSizeOverride("font_size", 13);
        help.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        help.GrowHorizontal = Control.GrowDirection.Both;
        help.Position += new Vector2(0, -12);

        var bannerBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        bannerBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(bannerBox);
        _banner = NewLabel(Vector2.Zero);
        _bannerSub = NewLabel(Vector2.Zero);
        foreach (var (l, size) in new[] { (_banner, 44), (_bannerSub, 15) })
        {
            l.Reparent(bannerBox);
            l.AddThemeFontSizeOverride("font_size", size);
            l.HorizontalAlignment = HorizontalAlignment.Center;
            l.Visible = false;
        }

        bool player = Mech != null;
        _heatBack.Visible = _heatBar.Visible = _stat.Visible = _target.Visible = player;
        if (!player) _feed.Position = new Vector2(10, 10);

        _reticle = new Reticle { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_reticle);

        if (Battlefield != null) Battlefield.Damaged += OnDamaged;
    }

    private Label NewLabel(Vector2 pos)
    {
        var l = new Label { Position = pos };
        l.AddThemeColorOverride("font_color", new Color("e8e4dc"));
        l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.6f));
        AddChild(l);
        return l;
    }

    private void OnDamaged(MechNode mech, DamageReport r, Vector3 at, MechNode? attacker)
    {
        var sb = new StringBuilder();
        sb.Append(IsInstanceValid(attacker) ? attacker!.Name : "?").Append(" → ").Append(mech.Name).Append(": ");
        sb.Append(SectionNames[r.Struck]);
        if (r.Applied != r.Struck) sb.Append(" (→ ").Append(SectionNames[r.Applied]).Append(')');
        sb.Append($" −{r.Amount:0.#}");
        if (r.SectionDestroyed) sb.Append(" · ").Append(SectionNames[r.Applied].ToUpperInvariant()).Append(" DESTROYED");
        if (r.StatusAfter == MechStatus.PilotKilled) sb.Append(" · PILOT KILLED");
        else if (r.StatusAfter == MechStatus.Destroyed && r.SectionDestroyed) sb.Append(" · MECH DESTROYED");
        _feedLines.Add((_time, sb.ToString()));
        if (_feedLines.Count > 8) _feedLines.RemoveAt(0);
    }

    public override void _Process(double delta)
    {
        _time += delta;
        _uiTimer += delta;
        bool refresh = _uiTimer >= 0.1;
        if (refresh)
        {
            _uiTimer = 0;
            var feed = new StringBuilder();
            foreach (var (t, line) in _feedLines)
                if (_time - t < FeedLifetime) feed.Append(line).Append('\n');
            _feed.Text = feed.ToString();
        }
        _reticle.Visible = false;
        if (Camera == null || Pilot == null || !IsInstanceValid(Mech) || Mech!.Sim is not { } sim) return;
        var weapons = sim.Weapons;

        Vector3 aim = Pilot.AimPoint;
        bool visible = Camera.Mode is CameraMode.Chase or CameraMode.Cockpit && Camera.Target == Mech && !Camera.IsPositionBehind(aim) && !sim.IsOut;
        _reticle.Visible = visible;
        if (visible) _reticle.Position = Camera.UnprojectPosition(aim);
        _reticle.Hot = weapons.HeatLocked;

        if (!refresh) return;

        var body = sim.Locomotion.Body;
        var dmg = sim.Damage;
        string heat = weapons.HeatLocked ? "LOCKED" : $"{Mathf.RoundToInt(Mathf.Min(1f, weapons.Heat) * 100)}%";
        var sb = new StringBuilder();
        sb.Append($"HEAT {heat} · THR {Mathf.RoundToInt(Pilot.Throttle * 100)}% · {body.V * 3.6f:0.0} km/h · TWIST {Mathf.RoundToInt(Mathf.RadToDeg(-body.Twist))}°\n");
        for (int i = 0; i < weapons.Mounts.Count; i++)
        {
            var m = weapons.Mounts[i];
            string state = m.Offline ? "OFFLINE" : m.Cooldown > 0f ? $"{m.Cooldown:0.0}s" : "READY";
            sb.Append($"{m.Spec.Name.ToUpperInvariant()} {state}   ");
        }
        sb.Append('\n').Append(SectionLine(dmg));
        if (sim.IsOut) sb.Append(dmg.Status == MechStatus.PilotKilled ? "\nPILOT KILLED" : "\nMECH DESTROYED");
        if (GetTree().Paused) sb.Append("\nPAUSED");
        else if (Engine.TimeScale < 1.0) sb.Append("\nSLOW-MO");
        _stat.Text = sb.ToString();

        _heatBar.Size = new Vector2(HeatBarWidth * Mathf.Clamp(weapons.Heat, 0f, 1f), 5);
        _heatBar.Color = weapons.HeatLocked ? HeatLockedColor : HeatColor;

        var hit = Pilot.AimHit;
        if (hit.Mech != null && IsInstanceValid(hit.Mech))
        {
            var td = hit.Mech.Sim.Damage;
            string status = td.Status switch { MechStatus.PilotKilled => " · PILOT KILLED", MechStatus.Destroyed => " · DESTROYED", _ => "" };
            _target.Text = $"{hit.Mech.Name} · {hit.Distance:0} m · on {SectionNames[hit.Section].ToUpperInvariant()}{status}\n{SectionLine(td)}";
        }
        else _target.Text = "";
    }

    /// <summary>Big centred message with a small line under it (match result); null hides it.</summary>
    public void ShowBanner(string? text, Color color, string sub = "")
    {
        _banner.Visible = _bannerSub.Visible = text != null;
        _banner.Text = text ?? "";
        _bannerSub.Text = sub;
        _banner.AddThemeColorOverride("font_color", color);
    }

    private static string SectionLine(MechDamage d) =>
        $"TOR {Hp(d, MechSection.Torso)}  ARM L {Hp(d, MechSection.ArmLeft)}  ARM R {Hp(d, MechSection.ArmRight)}  " +
        $"LEGS {Hp(d, MechSection.Legs)}  CKPT {Hp(d, MechSection.Cockpit)}";

    private static string Hp(MechDamage d, MechSection s) => d.IsDestroyed(s) ? "XX" : $"{Mathf.CeilToInt(d[s])}";
}
