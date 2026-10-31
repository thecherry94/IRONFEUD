using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Scripted gunner for unattended captures (<c>--range-demo</c>): works through a list of (target, section, weapons)
/// tasks, tracking the section with <see cref="AimSolver"/> and firing once aligned. Compensates shell drop.
/// Writes only MechInput and runs after the player controller, overriding it.
/// </summary>
public partial class RangeDemoPilot : Node
{
    public MechNode? Mech;
    public readonly List<(MechNode Target, MechSection Section, uint Fire)> Tasks = new();

    private float _twist, _aim, _time;
    private int _task;

    public override void _Ready() => ProcessPhysicsPriority = -90;

    public override void _PhysicsProcess(double delta)
    {
        if (Mech == null) return;
        _time += (float)delta;
        while (_task < Tasks.Count && Done(Tasks[_task])) _task++;
        if (_task >= Tasks.Count || _time < 1f)
        {
            Mech.PilotInput = new MechInput { TwistTarget = _twist, AimTarget = _aim };
            return;
        }

        var (target, section, fire) = Tasks[_task];
        var sim = Mech.Sim;
        var volume = target.Sim.Pose.HitVolumes[(int)section];
        var point = volume.Center;
        float dist = System.Numerics.Vector3.Distance(sim.Pose.AimOrigin, point);
        if ((fire & 1u) != 0)
        {
            var gun = sim.Weapons.Mounts[0].Spec;
            float tof = dist / gun.MuzzleVelocity;
            point.Y += 0.5f * gun.Gravity * tof * tof;
        }
        float err = AimSolver.Track(sim.Pose, sim.Locomotion.Body, sim.Chassis, point, ref _twist, ref _aim);
        bool aligned = err < 0.5f * volume.Radius / dist;
        Mech.PilotInput = new MechInput
        {
            TwistTarget = _twist,
            AimTarget = _aim,
            FireMask = aligned ? fire : 0u,
            Convergence = dist,
        };
    }

    private static bool Done((MechNode Target, MechSection Section, uint Fire) t) =>
        !IsInstanceValid(t.Target) || t.Target.Sim.IsOut || t.Target.Sim.Damage.IsDestroyed(t.Section);
}
