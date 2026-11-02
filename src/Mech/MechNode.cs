using Godot;
using IronFeud.Core;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Mech;

/// <summary>
/// Renders one <see cref="BattleUnit"/>: builds the rig, poses it after every battle tick and shows a name tag.
/// The simulation lives in <see cref="BattleSim"/>; a pilot (player or AI) writes <see cref="PilotInput"/>.
/// The node's own transform tracks the body (ground position + leg heading).
/// </summary>
public partial class MechNode : Node3D
{
    [ExportGroup("Gait feel")]
    [Export(PropertyHint.Range, "0,2,0.01")] public float Sway { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Bob { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.3,1.6,0.01")] public float Lift { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Impact { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,2.5,0.01")] public float Jank { get; set; } = 1f;

    /// <summary>Set before the node enters the tree.</summary>
    public BattleUnit Unit = null!;
    public Color Accent = MechRig.DefaultAccent;

    public MechSim Sim => Unit.Sim;
    public MechLocomotion Locomotion => Sim.Locomotion;
    public MechWeapons Weapons => Sim.Weapons;
    public MechRig Rig { get; private set; } = null!;
    public Label3D Tag { get; private set; } = null!;

    public MechInput PilotInput
    {
        get => Unit.Input;
        set => Unit.Input = value;
    }

    private double _tagTimer;

    public override void _Ready()
    {
        Name = Unit.Name;
        ProcessPhysicsPriority = -10; // after pilots, before the battle tick
        Rig = MechRig.Build(this, Sim.Chassis, Accent);
        SyncSectionVisibility();

        Tag = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FixedSize = true,
            PixelSize = 0.0011f,
            FontSize = 26,
            OutlineSize = 8,
            Modulate = Accent.Lightened(0.35f),
            Position = new Vector3(0f, 13f * Sim.Chassis.Scale, 0f),
            RenderPriority = 10,
            OutlineRenderPriority = 9,
        };
        AddChild(Tag);
        UpdateTag();

        SyncFromSimulation();
        ResetPhysicsInterpolation();
        Rig.Pelvis.ResetPhysicsInterpolation();
    }

    public override void _PhysicsProcess(double delta)
    {
        var t = Sim.Chassis;
        t.Sway = Sway;
        t.Bob = Bob;
        t.Lift = Lift;
        t.Impact = Impact;
        t.Jank = Jank;
    }

    public override void _Process(double delta)
    {
        _tagTimer -= delta;
        if (_tagTimer > 0) return;
        _tagTimer = 0.15;
        UpdateTag();
    }

    /// <summary>Called by the battlefield after each battle tick.</summary>
    public void SyncFromSimulation()
    {
        var body = Locomotion.Body;
        GlobalTransform = new Transform3D(new Basis(Vector3.Up, body.Psi), body.Position.ToGodot());
        Rig.Apply(Sim.Pose);
    }

    public void OnDamaged(in DamageReport report)
    {
        if (report.SectionDestroyed) SyncSectionVisibility();
        UpdateTag();
    }

    private void SyncSectionVisibility()
    {
        Rig.Arms[0].Shoulder.Visible = !Sim.Damage.IsDestroyed(MechSection.ArmLeft);
        Rig.Arms[1].Shoulder.Visible = !Sim.Damage.IsDestroyed(MechSection.ArmRight);
    }

    private void UpdateTag()
    {
        var d = Sim.Damage;
        string who = Unit.Ai is { } ai ? $"{Unit.Name} · {ai.Personality.Name}" : Unit.Name;
        if (Sim.Chassis.Class != ChassisClass.Medium && Unit.Name != Sim.Chassis.Name) who += $" · {Sim.Chassis.Name}";
        string state = d.Status switch
        {
            MechStatus.Destroyed => "DESTROYED",
            MechStatus.PilotKilled => "PILOT KILLED",
            _ when Sim.Shutdown => "SHUTDOWN",
            _ when Sim.Staggered => "STAGGERED",
            _ => Unit.Ai is { } a ? AiPilot.Name(a.Action) + (a.TwistingAway ? " · twist-away" : "") : "",
        };
        int bars = Mathf.CeilToInt(d.Fraction(MechSection.Torso) * 10f);
        Tag.Text = $"{who}\n{new string('▮', bars)}{new string('▯', 10 - bars)}\n{state}";
        Tag.Modulate = d.IsOut ? new Color(0.6f, 0.6f, 0.6f, 0.7f) : Accent.Lightened(0.35f);
    }
}
