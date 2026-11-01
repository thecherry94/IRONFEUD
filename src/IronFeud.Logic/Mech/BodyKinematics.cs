using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>Root motion of the mech: where the legs are taking the body and where the torso points.</summary>
public sealed class BodyState
{
    /// <summary>
    /// Ground position under the pelvis. y follows the ground under the body and the planted feet; in the air it is
    /// where the ground would be for the pelvis height (pelvis = y + hip height), integrated ballistically.
    /// </summary>
    public Vector3 Position;

    /// <summary>On jump jets or falling: no feet on the ground.</summary>
    public bool Airborne;

    /// <summary>Vertical speed while airborne, m/s (+ = up).</summary>
    public float Vy;

    /// <summary>Leg heading, radians about +Y (Godot Rotation.Y).</summary>
    public float Psi;

    /// <summary>Signed forward speed, m/s.</summary>
    public float V;

    /// <summary>Low-passed longitudinal acceleration, m/s².</summary>
    public float Acc;

    /// <summary>Turn rate, rad/s, positive = left.</summary>
    public float Omega;

    public float Twist;
    public float Aim;

    /// <summary>Smoothed ground slope along the heading, radians (+ = climbing). Set by MechLocomotion.</summary>
    public float Slope;
}

public static class BodyKinematics
{
    /// <param name="speedFactor">Forward top-speed multiplier (crippled legs).</param>
    /// <param name="control">Leg authority over speed: 1 on the ground; in the air only the jets push (a fraction of it).</param>
    /// <param name="turnControl">Authority over the turn rate, given in the air (jets: no speed penalty); default: the square
    /// root of <paramref name="control"/> with the legs' speed penalty.</param>
    public static void Step(BodyState s, in MechInput input, MechChassis t, float h, float speedFactor = 1f, float control = 1f,
        float turnControl = -1f)
    {
        float throttle = MathUtil.Clamp(input.Throttle, -1f, 1f);
        float turn = MathUtil.Clamp(input.Turn, -1f, 1f);

        float vT = throttle >= 0f ? throttle * t.VMax * speedFactor : throttle * t.VRev;
        float dv = vT - s.V;
        float refSign = MathF.Sign(s.V != 0f ? s.V : vT);
        float a = (MathF.Abs(vT) > MathF.Abs(s.V) && MathF.Sign(vT) == refSign ? t.Accel : t.Decel) * control;
        float vPrev = s.V;
        s.V += MathUtil.Clamp(dv, -a * h, a * h);
        s.Acc = MathUtil.Lerp(s.Acc, (s.V - vPrev) / h, MathF.Min(1f, h * 6f));

        // On the ground the legs carve the turn (slower at speed); in the air the jets yaw the body, no speed penalty.
        float omegaT = turnControl >= 0f
            ? turn * t.OmegaMax * turnControl
            : turn * t.OmegaMax * (1f - t.TurnSpeedPenalty * MathF.Abs(s.V) / t.VMax) * MathF.Sqrt(control);
        s.Omega += MathUtil.Clamp(omegaT - s.Omega, -t.OmegaAccel * h, t.OmegaAccel * h);
        s.Psi += s.Omega * h;
        s.Position += MathUtil.Forward(s.Psi) * (s.V * h);

        float twistT = MathUtil.Clamp(input.TwistTarget, -t.TwistMax, t.TwistMax);
        float aimT = MathUtil.Clamp(input.AimTarget, t.AimMin, t.AimMax);
        s.Twist += MathUtil.Clamp(twistT - s.Twist, -t.TwistRate * h, t.TwistRate * h);
        s.Aim += MathUtil.Clamp(aimT - s.Aim, -t.AimRate * h, t.AimRate * h);
    }
}
