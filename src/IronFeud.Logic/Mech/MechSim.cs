using System.Collections.Generic;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Mech;

/// <summary>
/// A whole mech at the fixed tick: locomotion, then weapons. Weapons fire from this tick's pose;
/// their recoil impulses land in the springs and show up from the next tick on.
/// </summary>
public sealed class MechSim
{
    public readonly MechLocomotion Locomotion;
    public readonly MechWeapons Weapons;

    /// <summary>Shots fired during the last <see cref="Step"/>.</summary>
    public readonly List<ShotFired> Shots = new();

    public MechSim(MechTuning tuning, uint seed = 1)
    {
        Locomotion = new MechLocomotion(tuning, seed);
        Weapons = MechWeapons.Default(tuning);
    }

    public MechTuning Tuning => Locomotion.Tuning;
    public MechPose Pose => Locomotion.Pose;

    public void Step(in MechInput input, float h)
    {
        Shots.Clear();
        Locomotion.Step(input, h);
        Weapons.Step(input, Locomotion, h, Shots);
    }
}
