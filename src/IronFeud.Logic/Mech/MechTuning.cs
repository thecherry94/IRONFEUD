using System.Numerics;

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

    // Upper-body attachment points (MechRig builds the same hierarchy from these)
    public float TorsoBaseY = 0.55f;
    public float TorsoY = 0.6f;
    public Vector3 ShoulderOffset = new(2.4f, 1.9f, 0f);     // left arm mirrors X
    public Vector3 ElbowOffset = new(0.45f, -2.1f, 0f);      // left arm mirrors X
    public float AimOriginY = 1.2f;                            // torso-local height of the aim ray origin
    public Vector3 HeadOffset = new(0f, 2.75f, -1.0f);         // torso-local

    // Hit volumes (spheres). Torso/arms/legs from the prototype; the cockpit is a tiny sphere on the canopy glass.
    public float TorsoHitY = 1.2f;
    public float TorsoHitRadius = 2.4f;
    public Vector3 ArmHitOffset = new(0f, 0f, -1.7f);           // elbow-local (forearm)
    public float ArmHitRadius = 1.4f;
    public float LegsHitDrop = 2.4f;                           // below the pelvis
    public float LegsHitRadius = 2.0f;
    public Vector3 CockpitHitOffset = new(0f, 0.05f, -0.85f);  // head-local, just in front of the torso sphere
    public float CockpitHitRadius = 0.3f;

    // Damage effects
    public float CrippledSpeedFactor = 0.45f;
    public float CollapseDrop = 2.5f;
    public float CollapseTime = 1.4f;

    // Heat (unitless, weapons lock at >= 1)
    public float HeatStart = 0.1f;
    public float HeatFloor = 0.08f;
    public float HeatDissipation = 0.055f;
    public float HeatDissipationMovePenalty = 0.015f;

    // Feel multipliers (prototype sliders)
    public float Sway = 1f;
    public float Bob = 1f;
    public float Lift = 1f;
    public float Impact = 1f;
    public float Jank = 1f;
}
