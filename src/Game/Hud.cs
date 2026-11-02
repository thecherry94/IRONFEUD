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
    private ColorRect _jetBack = null!, _jetBar = null!;
    private static readonly Color JetColor = new("5ad8ff"), JetLowColor = new("2a6a80");
    private const float JetBarWidth = 70f;
    private Reticle _reticle = null!, _torsoReticle = null!, _aimDot = null!;
    private RichTextLabel _weapons = null!;
    private double _uiTimer, _time;
    // A burning beam lands a sliver of damage every tick: those merge into one line (same attacker, target and
    // section, within 0.25 s) whose amount keeps adding up.
    private readonly List<(double Time, string Text, string Key, float Amount)> _feedLines = new();

    private const float HeatBarWidth = 180f;
    private const double FeedLifetime = 8.0;
    private static readonly Color HeatColor = new("ff8a2a"), HeatLockedColor = new("ff3a2a");

    public string HelpText = "";

    private static readonly Dictionary<MechSection, string> SectionNames = new()
    {
        [MechSection.Torso] = "centre torso", [MechSection.TorsoLeft] = "left torso", [MechSection.TorsoRight] = "right torso",
        [MechSection.ArmLeft] = "left arm", [MechSection.ArmRight] = "right arm",
        [MechSection.LegLeft] = "left leg", [MechSection.LegRight] = "right leg", [MechSection.Cockpit] = "cockpit",
    };

    /// <summary>A circle with a dot (the arm weapons' aim), a cross (the torso's), or a small dot (the pilot's aim point).</summary>
    private partial class Reticle : Control
    {
        private bool _hot;
        public bool Cross, Dot;

        public bool Hot
        {
            get => _hot;
            set { if (_hot != value) { _hot = value; QueueRedraw(); } }
        }

        public override void _Draw()
        {
            var c = _hot ? new Color(1f, 0.35f, 0.23f, 0.9f) : new Color(0.49f, 1f, 0.69f, 0.85f);
            if (Dot)
            {
                DrawCircle(Vector2.Zero, 2.5f, new Color(1f, 1f, 1f, 0.85f));
                DrawArc(Vector2.Zero, 4.5f, 0f, Mathf.Tau, 16, new Color(0f, 0f, 0f, 0.5f), 1.5f, true);
                return;
            }
            if (Cross)
            {
                foreach (var d in new[] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down })
                    DrawLine(d * 6f, d * 13f, c, 2f, true);
                return;
            }
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
        // Jump-jet charge, right of the heat bar (dim below the quarter needed to take off).
        _jetBack = new ColorRect { Position = new Vector2(20 + HeatBarWidth, 8), Size = new Vector2(JetBarWidth, 5), Color = new Color(1, 1, 1, 0.1f), Visible = false };
        AddChild(_jetBack);
        _jetBar = new ColorRect { Position = new Vector2(20 + HeatBarWidth, 8), Size = new Vector2(0, 5), Color = JetColor, Visible = false };
        AddChild(_jetBar);

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
        _torsoReticle = new Reticle { MouseFilter = Control.MouseFilterEnum.Ignore, Cross = true };
        AddChild(_torsoReticle);
        _aimDot = new Reticle { MouseFilter = Control.MouseFilterEnum.Ignore, Dot = true };
        AddChild(_aimDot);

        // Weapon panel, bottom right: each weapon with its groups, rounds and state; the groups' fire modes under it.
        _weapons = new RichTextLabel
        {
            BbcodeEnabled = true, FitContent = true, ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = player,
        };
        _weapons.AddThemeFontOverride("normal_font", new SystemFont { FontNames = new[] { "DejaVu Sans Mono", "Liberation Mono", "Consolas", "Menlo", "monospace" } });
        _weapons.AddThemeFontSizeOverride("normal_font_size", 13);
        _weapons.AddThemeColorOverride("default_color", new Color("e8e4dc"));
        _weapons.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        _weapons.GrowHorizontal = Control.GrowDirection.Begin;
        _weapons.GrowVertical = Control.GrowDirection.Begin;
        _weapons.CustomMinimumSize = new Vector2(430, 0);
        _weapons.Position += new Vector2(-12, -84);
        _weapons.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.07f, 0.45f),
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 5, ContentMarginBottom = 5,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        });
        AddChild(_weapons);

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
        if (r.Facing == Facing.Rear) sb.Append(" (rear)");
        if (r.Applied != r.Struck) sb.Append(" (→ ").Append(SectionNames[r.Applied]).Append(')');
        string key = sb.ToString();
        int merge = _feedLines.FindLastIndex(l => l.Key == key && _time - l.Time < 0.25);
        float amount = r.Amount + (merge >= 0 ? _feedLines[merge].Amount : 0f);
        sb.Append($" −{amount:0.#}");
        if (r.SectionDestroyed) sb.Append(" · ").Append(SectionNames[r.Applied].ToUpperInvariant()).Append(" DESTROYED");
        if (r.StatusAfter == MechStatus.PilotKilled) sb.Append(" · PILOT KILLED");
        else if (r.StatusAfter == MechStatus.Destroyed && r.SectionDestroyed) sb.Append(" · MECH DESTROYED");
        var line = (_time, sb.ToString(), r.SectionDestroyed ? "" : key, amount);
        if (merge >= 0) _feedLines[merge] = line;
        else _feedLines.Add(line);
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
            foreach (var (t, line, _, _) in _feedLines)
                if (_time - t < FeedLifetime) feed.Append(line).Append('\n');
            _feed.Text = feed.ToString();
        }
        _reticle.Visible = _torsoReticle.Visible = _aimDot.Visible = false;
        if (Camera == null || Pilot == null || !IsInstanceValid(Mech) || Mech!.Sim is not { } sim) return;
        var weapons = sim.Weapons;

        bool flying = Camera.Mode is CameraMode.Chase or CameraMode.Cockpit && Camera.Target == Mech && !sim.IsOut;
        foreach (var (reticle, point) in new[] { (_reticle, Pilot.ArmAimPoint), (_torsoReticle, Pilot.TorsoAimPoint), (_aimDot, Pilot.PilotAimPoint) })
        {
            reticle.Visible = flying && !Camera.IsPositionBehind(point);
            if (reticle.Visible) reticle.Position = Camera.UnprojectPosition(point);
            reticle.Hot = weapons.HeatLocked;
        }

        if (!refresh) return;

        var body = sim.Locomotion.Body;
        var dmg = sim.Damage;
        string heat = weapons.HeatLocked ? "LOCKED" : $"{Mathf.RoundToInt(Mathf.Min(1f, weapons.Heat) * 100)}%";
        var sb = new StringBuilder();
        sb.Append($"HEAT {heat} · THR {Mathf.RoundToInt(Pilot.Throttle * 100)}% · {body.V * 3.6f:0.0} km/h · TWIST {Mathf.RoundToInt(Mathf.RadToDeg(-body.Twist))}°");
        if (sim.Chassis.HasJumpJets) sb.Append($" · JETS {Mathf.RoundToInt(sim.Locomotion.JumpCharge * 100)}%{(sim.Locomotion.JetsBurning ? " ▲" : "")}");
        if (sim.Weapons.Mounts.Exists(m => m.Spec.Guided) && Mech!.Unit is var unit)
            sb.Append(unit.Locked != null ? $" · LOCKED {unit.Locked.Name}"
                : unit.LockCandidate != null ? $" · LOCK {Mathf.RoundToInt(100f * unit.LockProgress / unit.LockTime)}%" : " · NO LOCK");
        sb.Append('\n').Append(SectionLine(dmg));
        _weapons.Text = WeaponPanel(weapons, Pilot);
        if (sim.IsOut) sb.Append(dmg.Status == MechStatus.PilotKilled ? "\nPILOT KILLED" : "\nMECH DESTROYED");
        else if (sim.Shutdown) sb.Append($"\nHEAT SHUTDOWN · restart in {sim.ShutdownLeft:0.0} s");
        else if (sim.Staggered) sb.Append("\nSTAGGERED");
        else if (sim.FlushLeft > 0f) sb.Append($"\nCOOLANT FLUSH · immune to outside heat {sim.FlushLeft:0.0} s");
        else if (sim.Hot > 0.05f) sb.Append($"\nRUNNING HOT · legs -{100f * IronFeud.Logic.Mech.MechSim.HotSpeedLoss * sim.Hot:0}% · aim unsteady");
        if (!sim.IsOut && sim.Instability > 0.2f) sb.Append($"\nSTABILITY {100f * (1f - sim.Instability):0}%");
        else if (sim.DisruptLeft > 0f) sb.Append("\nSENSORS SCRAMBLED");
        if (Mech!.Unit is { MarkedBy: { } marker } me && Battlefield?.Battle.Marked(marker.Team, me) == true) sb.Append("\nMARKED BY DESIGNATOR");
        if (Battlefield?.Battle.Jammed(Mech.Unit.Team, Mech.Unit) == true) sb.Append("\nECM: SENSORS JAMMED");
        if (Battlefield?.Battle is { } battle)
        {
            int inbound = battle.IncomingGuided(Mech.Unit);
            if (inbound > 0) sb.Append($"\nINCOMING MISSILES ×{inbound} · keep turning");
            else if (battle.LockedOn(Mech.Unit)) sb.Append("\nMISSILE LOCK");
        }
        if (GetTree().Paused) sb.Append("\nPAUSED");
        else if (Engine.TimeScale < 1.0) sb.Append("\nSLOW-MO");
        _stat.Text = sb.ToString();

        _heatBar.Size = new Vector2(HeatBarWidth * Mathf.Clamp(weapons.Heat, 0f, 1f), 5);
        _heatBar.Color = weapons.HeatLocked ? HeatLockedColor : HeatColor;
        bool jets = sim.Chassis.HasJumpJets;
        _jetBack.Visible = _jetBar.Visible = jets;
        if (jets)
        {
            float charge = sim.Locomotion.JumpCharge;
            _jetBar.Size = new Vector2(JetBarWidth * charge, 5);
            _jetBar.Color = charge >= Logic.Mech.MechLocomotion.JumpMinCharge || sim.Locomotion.Body.Airborne ? JetColor : JetLowColor;
        }

        var hit = Pilot.AimHit;
        if (hit.Mech != null && IsInstanceValid(hit.Mech))
        {
            var td = hit.Mech.Sim.Damage;
            string status = td.Status switch { MechStatus.PilotKilled => " · PILOT KILLED", MechStatus.Destroyed => " · DESTROYED", _ => "" };
            // Its internals show only once the lance has scanned it (a sensor lock, shared across the team).
            var self = Mech!.Unit;
            string internals = Battlefield?.Battle.Scan(self.Team, hit.Mech.Unit) != null ? SectionLine(td)
                : self.LockCandidate == hit.Mech.Unit ? $"SCANNING {Mathf.RoundToInt(100f * self.LockProgress / self.LockTime)}%"
                : "NOT SCANNED";
            string marked = Battlefield?.Battle.Marked(self.Team, hit.Mech.Unit) == true ? " · MARKED" : "";
            _target.Text = $"{hit.Mech.Name} · {hit.Distance:0} m · on {SectionNames[hit.Section].ToUpperInvariant()}{status}{marked}\n{internals}";
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

    /// <summary>
    /// "> Heavy cannon   1 · · · · ·   27  READY": the weapon picked for group editing, its name, the groups it is in,
    /// rounds left (-- for energy weapons) and its state; then each used group's fire mode.
    /// </summary>
    private static string WeaponPanel(IronFeud.Logic.Mech.Weapons.MechWeapons weapons, PlayerMechController pilot)
    {
        var g = pilot.Groups;
        var sb = new StringBuilder("  WEAPON              GROUPS        AMMO  STATE\n");
        for (int i = 0; i < weapons.Mounts.Count; i++)
        {
            var m = weapons.Mounts[i];
            var groups = new StringBuilder();
            for (int k = 0; k < IronFeud.Logic.Mech.Weapons.WeaponGroups.Count; k++) groups.Append(g.Contains(k, i) ? $"{k + 1} " : "· ");
            string ammo = m.Ammo < 0 ? "--" : m.Ammo.ToString();
            string state = m.Lost ? "[color=#ff4a3a]LOST[/color]" : m.Ammo == 0 ? "[color=#ff8a2a]EMPTY[/color]"
                : m.Cooldown > 0f ? $"[color=#c8c2b8]{m.Cooldown:0.0}s[/color]" : "[color=#7dffb0]READY[/color]";
            sb.Append(i == pilot.SelectedWeapon ? "> " : "  ").Append($"{m.Spec.Name,-19} {groups} {ammo,5}  {state}\n");
        }
        var modes = new StringBuilder();
        for (int k = 0; k < IronFeud.Logic.Mech.Weapons.WeaponGroups.Count; k++)
            if (g.Members[k] != 0u) modes.Append($"{k + 1} {(g.Chain[k] ? "chain" : "alpha")}   ");
        sb.Append($"  {modes}");
        return sb.ToString();
    }

    private static string SectionLine(MechDamage d) =>
        $"CKPT {Hp(d, MechSection.Cockpit)}  TOR L {Hp(d, MechSection.TorsoLeft)}  C {Hp(d, MechSection.Torso)}  R {Hp(d, MechSection.TorsoRight)}  " +
        $"ARM L {Hp(d, MechSection.ArmLeft)}  R {Hp(d, MechSection.ArmRight)}  LEG L {Hp(d, MechSection.LegLeft)}  R {Hp(d, MechSection.LegRight)}";

    /// <summary>
    /// "armour/structure" per section; torso sections show front+rear plates; "!" marks exposed internals (the section
    /// is degrading: limp, slow gun, weak heat sinks, blurred sensors).
    /// </summary>
    private static string Hp(MechDamage d, MechSection s)
    {
        if (d.IsDestroyed(s)) return "XX";
        int i = (int)s;
        string plate = MechDamage.IsTorso(s) ? $"{Mathf.CeilToInt(d.Armor[i])}+{Mathf.CeilToInt(d.RearArmor[i])}" : $"{Mathf.CeilToInt(d.Armor[i])}";
        return $"{plate}/{Mathf.CeilToInt(d.Structure[i])}{(d.Exposed(s) ? "!" : "")}";
    }
}
