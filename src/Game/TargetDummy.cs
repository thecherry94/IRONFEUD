using Godot;
using IronFeud.Logic.Mech;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>Brainless pilot for the firing range: stands still, or walks a steady circle to practise leading.</summary>
public partial class TargetDummy : Node
{
    public MechNode? Mech;
    public bool Circle;

    public override void _Ready() => ProcessPhysicsPriority = -100;

    public override void _PhysicsProcess(double delta)
    {
        if (Mech == null || !IsInstanceValid(Mech)) return;
        Mech.PilotInput = Circle ? new MechInput { Throttle = 0.5f, Turn = 0.35f } : default;
    }
}
