using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>Root motion of the mech: where the legs are taking the body and where the torso points.</summary>
public sealed class BodyState
{
    /// <summary>Ground position under the pelvis (y = ground height, currently 0).</summary>
    public Vector3 Position;

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
}

public static class BodyKinematics
{
    /// <param name="speedFactor">Forward top-speed multiplier (crippled legs).</param>
    public static void Step(BodyState s, in MechInput input, MechChassis t, float h, float speedFactor = 1f)
    {
        float throttle = MathUtil.Clamp(input.Throttle, -1f, 1f);
        float turn = MathUtil.Clamp(input.Turn, -1f, 1f);

        float vT = throttle >= 0f ? throttle * t.VMax * speedFactor : throttle * t.VRev;
        float dv = vT - s.V;
        float refSign = MathF.Sign(s.V != 0f ? s.V : vT);
        float a = MathF.Abs(vT) > MathF.Abs(s.V) && MathF.Sign(vT) == refSign ? t.Accel : t.Decel;
        float vPrev = s.V;
        s.V += MathUtil.Clamp(dv, -a * h, a * h);
        s.Acc = MathUtil.Lerp(s.Acc, (s.V - vPrev) / h, MathF.Min(1f, h * 6f));

        float omegaT = turn * t.OmegaMax * (1f - t.TurnSpeedPenalty * MathF.Abs(s.V) / t.VMax);
        s.Omega += MathUtil.Clamp(omegaT - s.Omega, -t.OmegaAccel * h, t.OmegaAccel * h);
        s.Psi += s.Omega * h;
        s.Position += MathUtil.Forward(s.Psi) * (s.V * h);

        float twistT = MathUtil.Clamp(input.TwistTarget, -t.TwistMax, t.TwistMax);
        float aimT = MathUtil.Clamp(input.AimTarget, t.AimMin, t.AimMax);
        s.Twist += MathUtil.Clamp(twistT - s.Twist, -t.TwistRate * h, t.TwistRate * h);
        s.Aim += MathUtil.Clamp(aimT - s.Aim, -t.AimRate * h, t.AimRate * h);
    }
}
