namespace IronFeud.Logic.Mech;

/// <summary>
/// Everything a pilot can command. The player controller and AI pilots write exactly this, nothing else.
/// Targets are clamped and rate-limited by <see cref="BodyKinematics"/>.
/// </summary>
public struct MechInput
{
    /// <summary>Throttle set-point, -1..1. Holding/ramping it is the pilot's job.</summary>
    public float Throttle;

    /// <summary>Leg turn command, -1..1, positive = turn left.</summary>
    public float Turn;

    /// <summary>Torso twist target in radians relative to the legs, positive = left.</summary>
    public float TwistTarget;

    /// <summary>Torso aim pitch target in radians, positive = up.</summary>
    public float AimTarget;

    /// <summary>Trigger bits, one per weapon mount. Held triggers fire whenever the weapon is ready.</summary>
    public uint FireMask;

    /// <summary>Distance along the torso aim ray where arm weapons converge, meters. 0 = default.</summary>
    public float Convergence;
}
