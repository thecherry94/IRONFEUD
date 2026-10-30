using System;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Mech;

/// <summary>
/// Thin Godot adapter around <see cref="MechSim"/>. A pilot (player or AI) writes <see cref="PilotInput"/>
/// before this node's physics tick; the node steps the simulation, poses the rig and reports footfalls and shots.
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

    /// <summary>Carried-over damage (e.g. from a previous battle); set before the node enters the tree.</summary>
    public MechDamage? InitialDamage;

    public MechInput PilotInput;
    public MechSim Sim { get; private set; } = null!;
    public MechLocomotion Locomotion => Sim.Locomotion;
    public MechWeapons Weapons => Sim.Weapons;
    public MechRig Rig { get; private set; } = null!;

    public event Action<Footfall>? Footfall;
    public event Action<ShotFired>? ShotFired;

    /// <summary>A hit was applied: report, world point, attacker (if known).</summary>
    public event Action<DamageReport, Vector3, MechNode?>? Damaged;

    public override void _Ready()
    {
        var tuning = new MechTuning();
        Sim = new MechSim(tuning, Seed, InitialDamage);
        var start = GlobalPosition;
        Locomotion.Reset(new System.Numerics.Vector3(start.X, 0f, start.Z), Rotation.Y);

        Rig = MechRig.Build(this, tuning);
        SyncSectionVisibility();

        SyncFromSimulation();
        ResetPhysicsInterpolation();
        Rig.Pelvis.ResetPhysicsInterpolation();
    }

    public override void _PhysicsProcess(double delta)
    {
        var t = Sim.Tuning;
        t.Sway = Sway;
        t.Bob = Bob;
        t.Lift = Lift;
        t.Impact = Impact;
        t.Jank = Jank;

        Sim.Step(PilotInput, (float)delta);
        SyncFromSimulation();
        foreach (var f in Locomotion.Footfalls) Footfall?.Invoke(f);
        foreach (var s in Sim.Shots) ShotFired?.Invoke(s);
    }

    public DamageReport ApplyHit(MechSection section, float amount, Vector3 at, MechNode? attacker)
    {
        var report = Sim.ApplyHit(section, amount);
        if (report.Ignored) return report;
        if (report.SectionDestroyed) SyncSectionVisibility();
        Damaged?.Invoke(report, at, attacker);
        return report;
    }

    private void SyncSectionVisibility()
    {
        Rig.Arms[0].Shoulder.Visible = !Sim.Damage.IsDestroyed(MechSection.ArmLeft);
        Rig.Arms[1].Shoulder.Visible = !Sim.Damage.IsDestroyed(MechSection.ArmRight);
    }

    private void SyncFromSimulation()
    {
        var body = Locomotion.Body;
        GlobalTransform = new Transform3D(new Basis(Vector3.Up, body.Psi), body.Position.ToGodot());
        Rig.Apply(Sim.Pose);
    }
}
