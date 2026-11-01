using Godot;
using IronFeud.Core;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;

namespace IronFeud.AI;

/// <summary>
/// Draws what the selected mech's AI is thinking, like the prototype's overlay: destination (gold line + ring),
/// line of sight to its target (green visible / red remembered), lead point (cyan), the target's rear arc as this
/// pilot last saw it (magenta wedge: where its thin rear armour is), an escort's ward (light blue line and threat-range ring,
/// red line to the enemy threatening it), a cyan column while a jump is planned, steering heading (yellow) and every cover point
/// (grey; green = hidden from the target; gold = chosen). The selection ring is always drawn.
/// </summary>
public partial class AiDebugDraw : MeshInstance3D
{
    public MechNode? Selected;

    /// <summary>The player's own mech: nothing is drawn for it.</summary>
    public MechNode? Player;
    public BattleSim? Battle;

    /// <summary>Draw the AI layer (the selection ring is drawn regardless).</summary>
    public bool ShowAi = true;

    private readonly ImmediateMesh _mesh = new();
    private bool[] _hidden = System.Array.Empty<bool>();
    private double _coverTimer;

    private static readonly Color Dest = new("ffd24a");
    private static readonly Color Seen = new("7dffb0");
    private static readonly Color Remembered = new("ff4a3a");
    private static readonly Color Lead = new("40e0ff");
    private static readonly Color Steer = new(1f, 1f, 0.4f, 0.8f);
    private static readonly Color CoverIdle = new(0.4f, 0.42f, 0.45f);
    private static readonly Color RearArc = new(1f, 0.35f, 0.9f, 0.85f);
    private static readonly Color Escort = new("9fd8ff");

    public override void _Ready()
    {
        TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        CastShadow = ShadowCastingSetting.Off;
        Mesh = _mesh;
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            NoDepthTest = true,
            DisableFog = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
    }

    public override void _Process(double delta)
    {
        _mesh.ClearSurfaces();
        if (!IsInstanceValid(Selected) || Battle == null) return;
        var m = Selected!;
        var unit = m.Unit;
        if (m == Player) return; // no AI, and no ring under your own feet
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        Vector3 ground = unit.Position.ToGodot();
        Ring(ground + Vector3.Up * 0.1f, 4.6f * unit.Chassis.Scale, m.Accent.Lightened(0.4f));

        if (ShowAi && unit.Ai is { } ai && !unit.IsOut)
        {
            Vector3 dest = ai.Destination.ToGodot();
            Line(ground + Vector3.Up * 0.4f, dest + Vector3.Up * 0.4f, Dest);
            Ring(dest + Vector3.Up * 0.2f, 1.7f, Dest);
            Line(ground + Vector3.Up * 0.6f, ground + Vector3.Up * 0.6f + MathUtil.Forward(ai.DesiredHeading).ToGodot() * 10f, Steer);

            var k = ai.Target != null && ai.Contacts.TryGetValue(ai.Target, out var c) ? c : null;
            if (k != null)
            {
                Vector3 eye = unit.Volume(MechSection.Cockpit).Center.ToGodot();
                Line(eye, (k.Position + System.Numerics.Vector3.UnitY * k.AimHeight).ToGodot(), k.Visible ? Seen : Remembered);
                Cross(ai.AimPoint.ToGodot(), 0.8f, Lead);
                Wedge(k.Position.ToGodot() + Vector3.Up * 0.3f, k.RearBearing, 0.87f, 40f, RearArc); // ±50°: full rear hits
            }
            if (ai.Ward is { IsOut: false } ward)
            {
                // Escort: a line to the ward, a ring at the range threats count from, a red line to the threat.
                Vector3 w = ward.Position.ToGodot() + Vector3.Up * 0.5f;
                Line(ground + Vector3.Up * 0.5f, w, Escort);
                Ring(w, Logic.AI.AiPilot.ThreatRange, new Color(Escort, 0.35f), 48);
                if (ai.Threat is { IsOut: false } t) Line(w, t.Position.ToGodot() + Vector3.Up * 0.5f, Remembered);
            }
            // Jump plan: a cyan column above the mech while a tactical jump is lined up or under way.
            if (ai.Jump != Logic.AI.JumpPlan.None || ai.VaultArmed)
                Line(ground + Vector3.Up * 2f, ground + Vector3.Up * 30f * unit.Chassis.Scale, Lead);
            DrawCover(ai, unit, k, delta);
        }
        _mesh.SurfaceEnd();
    }

    private void DrawCover(Logic.AI.AiPilot ai, BattleUnit unit, Logic.AI.Contact? k, double delta)
    {
        var cover = Battle!.Map.Cover;
        if (_hidden.Length != cover.Count) _hidden = new bool[cover.Count];
        _coverTimer -= delta;
        if (_coverTimer <= 0)
        {
            _coverTimer = 0.5;
            for (int i = 0; i < cover.Count; i++) _hidden[i] = k != null && ai.Hidden(unit, cover[i].Position, k, Battle.Map);
        }
        for (int i = 0; i < cover.Count; i++)
        {
            var col = i == ai.CoverIndex ? Dest : _hidden[i] ? Seen : CoverIdle;
            Ring(cover[i].Position.ToGodot() + Vector3.Up * 0.15f, i == ai.CoverIndex ? 1.2f : 0.5f, col, 8);
        }
    }

    private void Ring(Vector3 c, float r, Color col, int n = 32)
    {
        for (int i = 0; i < n; i++)
        {
            float a0 = Mathf.Tau * i / n, a1 = Mathf.Tau * (i + 1) / n;
            Line(c + new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), c + new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r), col);
        }
    }

    /// <summary>A ground wedge of half-angle <paramref name="half"/> around <paramref name="heading"/>.</summary>
    private void Wedge(Vector3 c, float heading, float half, float r, Color col, int n = 10)
    {
        Vector3 At(float h) => c + MathUtil.Forward(h).ToGodot() * r;
        Line(c, At(heading - half), col);
        Line(c, At(heading + half), col);
        for (int i = 0; i < n; i++)
            Line(At(heading - half + 2f * half * i / n), At(heading - half + 2f * half * (i + 1) / n), col);
    }

    private void Line(Vector3 a, Vector3 b, Color c)
    {
        _mesh.SurfaceSetColor(c);
        _mesh.SurfaceAddVertex(a);
        _mesh.SurfaceSetColor(c);
        _mesh.SurfaceAddVertex(b);
    }

    private void Cross(Vector3 p, float r, Color c)
    {
        Line(p + new Vector3(-r, 0, 0), p + new Vector3(r, 0, 0), c);
        Line(p + new Vector3(0, 0, -r), p + new Vector3(0, 0, r), c);
        Line(p + new Vector3(0, -r, 0), p + new Vector3(0, r, 0), c);
    }
}
