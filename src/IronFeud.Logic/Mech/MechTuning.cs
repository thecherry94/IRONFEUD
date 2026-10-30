namespace IronFeud.Logic.Mech;

/// <summary>
/// Locomotion constants (see docs/locomotion.md) plus the feel multipliers from the prototype's sliders.
/// Values are the prototype's; the Godot side may override fields for live tuning.
/// </summary>
public sealed class MechTuning
{
    // Body kinematics
    public float VMax = 4.2f;
    public float VRev = 1.8f;
    public float Accel = 1.5f;
    public float Decel = 2.8f;
    public float OmegaMax = 0.6f;
    public float OmegaAccel = 1.2f;
    public float TurnSpeedPenalty = 0.35f;
    public float TwistMax = 1.75f;
    public float TwistRate = 1.3f;
    public float AimMin = -0.35f;
    public float AimMax = 0.45f;
    public float AimRate = 0.8f;

    // Gait
    public float Duty = 0.62f;
    public float DMax = 6.2f;
    public float FMin = 0.36f;
    public float AMax = 1.95f;

    // Rig dimensions
    public float L1 = 3.2f;
    public float L2 = 3.3f;
    public float AnkleHeight = 0.62f;
    public float HipWidth = 1.6f;
    public float HipHeight = 5.75f;
    public float HipRootY = -0.15f;

    // Feel multipliers (prototype sliders)
    public float Sway = 1f;
    public float Bob = 1f;
    public float Lift = 1f;
    public float Impact = 1f;
    public float Jank = 1f;
}
