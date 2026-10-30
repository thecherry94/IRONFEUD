using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Mech;

/// <summary>
/// Thin Godot adapter around <see cref="MechLocomotion"/>. A pilot (player or AI) writes <see cref="PilotInput"/>
/// before this node's physics tick; the node steps the simulation and poses the rig.
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

    [ExportGroup("")]
    [Export] public uint Seed { get; set; } = 1;

    public MechInput PilotInput;
    public MechLocomotion Locomotion { get; private set; } = null!;
    public MechRig Rig { get; private set; } = null!;
    public FootfallFx Fx { get; private set; } = null!;

    public override void _Ready()
    {
        var tuning = new MechTuning();
        Locomotion = new MechLocomotion(tuning, Seed);
        var start = GlobalPosition;
        Locomotion.Reset(new System.Numerics.Vector3(start.X, 0f, start.Z), Rotation.Y);

        Rig = MechRig.Build(this, tuning);
        Fx = new FootfallFx { Name = "FootfallFx" };
        AddChild(Fx);

        SyncFromSimulation();
        ResetPhysicsInterpolation();
        Rig.Pelvis.ResetPhysicsInterpolation();
    }

    public override void _PhysicsProcess(double delta)
    {
        var t = Locomotion.Tuning;
        t.Sway = Sway;
        t.Bob = Bob;
        t.Lift = Lift;
        t.Impact = Impact;
        t.Jank = Jank;

        Locomotion.Step(PilotInput, (float)delta);
        SyncFromSimulation();
        foreach (var f in Locomotion.Footfalls) Fx.Spawn(f);
    }

    private void SyncFromSimulation()
    {
        var body = Locomotion.Body;
        GlobalTransform = new Transform3D(new Basis(Vector3.Up, body.Psi), body.Position.ToGodot());
        Rig.Apply(Locomotion.Pose);
    }
}
