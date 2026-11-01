using System.Text;
using Godot;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;

namespace IronFeud.AI;

/// <summary>
/// Inspector for the selected mech, as in the prototype's side panel: action and why, utility scores, target and
/// line of sight, aim error, shots/hits/damage, heat against the personality's limit, section HP and the decision log.
/// </summary>
public partial class AiPanel : CanvasLayer
{
    public MechNode? Selected;
    public BattleSim? Battle;

    private RichTextLabel _text = null!;
    private PanelContainer _box = null!;
    private double _timer;

    private static readonly System.Collections.Generic.Dictionary<AiAction, string> Help = new()
    {
        [AiAction.Engage] = "Hold preferred range, orbit target, fire when aligned",
        [AiAction.Cool] = "Break line of sight behind cover until heat drops",
        [AiAction.Flank] = "Swing wide to regain line of sight",
        [AiAction.Retreat] = "Fall back to cover near allies, fire opportunistically",
        [AiAction.Search] = "Move to last known position / sweep the arena",
    };

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _box = new PanelContainer
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 0f,
            OffsetLeft = -360f, OffsetRight = -10f, OffsetTop = 10f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _box.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.05f, 0.06f, 0.07f, 0.72f), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 });
        AddChild(_box);
        _text = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            CustomMinimumSize = new Vector2(340f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _text.AddThemeFontSizeOverride("normal_font_size", 13);
        _text.AddThemeFontSizeOverride("bold_font_size", 15);
        _box.AddChild(_text);
    }

    public override void _Process(double delta)
    {
        _timer -= delta;
        if (_timer > 0) return;
        _timer = 0.12;
        _box.Visible = Visible && IsInstanceValid(Selected) && Battle != null;
        if (!_box.Visible) return;

        var m = Selected!;
        var u = m.Unit;
        var ai = u.Ai;
        var sim = u.Sim;
        var dmg = sim.Damage;
        string team = Hex(m.Accent.Lightened(0.35f));
        var sb = new StringBuilder();
        sb.Append($"[b][color={team}]{u.Name}[/color][/b]  {(ai != null ? ai.Personality.Name : "player")} · {sim.Chassis.Name}\n");

        if (u.IsOut) sb.Append(dmg.Status == MechStatus.PilotKilled ? "[color=#ff4a3a]PILOT KILLED[/color]\n" : "[color=#ff4a3a]DESTROYED[/color]\n");
        else if (ai != null)
        {
            sb.Append($"[b]{AiPilot.Name(ai.Action)}[/b]{(ai.TwistingAway ? " + TWIST-AWAY" : "")}{(ai.HoldingFire ? " (holding fire)" : "")}\n");
            sb.Append($"[color=#a8b0b8]{Help[ai.Action]}{(ai.Why != "" ? $" — because {ai.Why}" : "")}[/color]\n");
            float max = 1f;
            foreach (float s in ai.Scores) max = Mathf.Max(max, s);
            for (int i = 0; i < AiPilot.ActionCount; i++)
            {
                var a = (AiAction)i;
                string col = a == ai.Action ? "#ffd24a" : "#7d858c";
                sb.Append($"[color={col}][code]{AiPilot.Name(a),-8}{Bar(ai.Scores[i] / max, 14)} {ai.Scores[i]:0.00}[/code][/color]\n");
            }
            Battle!.TeamFocus.TryGetValue(u.Team, out var focus);
            if (ai.Target is { } t && ai.Contacts.TryGetValue(t, out var k))
            {
                float range = Logic.Core.MathUtil.PlanarDistance(u.Position, k.Position);
                string los = k.Visible ? "[color=#7dffb0]yes[/color]" : $"[color=#ff4a3a]no ({Battle.Time - k.LastSeen:0.0} s)[/color]";
                sb.Append($"target  {t.Name}{(focus == t ? " ★focus" : "")} · {range:0} m · LOS {los}\n");
            }
            else sb.Append("target  none\n");
            if (ai.Jump != JumpPlan.None || ai.VaultArmed) sb.Append($"jump    {(ai.VaultArmed ? "vault lined up" : ai.Jump.ToString())}\n");
            sb.Append($"role    {ai.Role}{(ai.Ward is { IsOut: false } w ? $" · escorting {w.Name}{(ai.Threat is { IsOut: false } th ? $" · threat {th.Name}" : "")}" : "")}\n");
            sb.Append($"aim error σ {ai.Sigma * 1000f:0.0} mrad\n");
        }

        sb.Append($"shots · hits · dmg  {u.Stats.Shots} · {u.Stats.Hits} · {u.Stats.DamageDealt:0}\n");
        float heat = sim.Weapons.Heat;
        float limit = ai?.Personality.HeatLimit ?? 1f;
        sb.Append($"[code]HEAT    [color={(heat > limit ? "#ff3a2a" : "#ff8a2a")}]{Bar(heat, 14)}[/color] {heat * 100f:0}%[/code]{(ai != null ? $"  limit {limit * 100f:0}%" : "")}\n");
        foreach (var (name, s) in new[] { ("TORSO", MechSection.Torso), ("ARM L", MechSection.ArmLeft), ("ARM R", MechSection.ArmRight), ("LEGS", MechSection.Legs), ("CKPT", MechSection.Cockpit) })
        {
            // Two bars: armour (grey-blue) and the fragile structure under it (green / amber / red once exposed).
            int i = (int)s;
            float armor = dmg.MaxArmor[i] > 0f ? dmg.Armor[i] / dmg.MaxArmor[i] : 0f, f = dmg.StructureFraction(s);
            string col = !dmg.Exposed(s) ? "#7dffb0" : f > 0.5f ? "#ffd24a" : "#ff4a3a";
            sb.Append($"[code]{name,-8}[color=#8fb4d8]{Bar(armor, 7)}[/color][color={col}]{Bar(f, 7)}[/color] {Mathf.CeilToInt(dmg.Armor[i])}/{Mathf.CeilToInt(dmg.Structure[i])}[/code]\n");
        }
        if (ai != null)
        {
            sb.Append("[color=#a8b0b8]");
            foreach (var line in ai.Log) sb.Append(line).Append('\n');
            sb.Append("[/color]");
        }
        _text.Text = sb.ToString();
    }

    private static string Bar(float f, int n)
    {
        int full = Mathf.Clamp(Mathf.RoundToInt(f * n), 0, n);
        return new string('█', full) + new string('░', n - full);
    }

    private static string Hex(Color c) => "#" + c.ToHtml(false);
}
