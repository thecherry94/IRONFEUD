using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>
/// Closed-loop torso pointing, the way a pilot does it: look at where the aim ray is now, nudge the twist/aim
/// targets by the error. Works through sway, springs and rate limits without modelling them.
/// </summary>
public static class AimSolver
{
    /// <summary>Updates twist/aim targets to bring the aim ray onto <paramref name="target"/>. Returns the angular error (rad).</summary>
    public static float Track(MechPose pose, BodyState body, MechTuning t, Vector3 target, ref float twistTarget, ref float aimTarget)
    {
        Vector3 want = Vector3.Normalize(target - pose.AimOrigin);
        Vector3 have = pose.AimDirection;
        float yawErr = MathUtil.WrapAngle(Yaw(want) - Yaw(have));
        float pitchErr = MathF.Asin(MathUtil.Clamp(want.Y, -1f, 1f)) - MathF.Asin(MathUtil.Clamp(have.Y, -1f, 1f));
        twistTarget = MathUtil.Clamp(body.Twist + yawErr, -t.TwistMax, t.TwistMax);
        aimTarget = MathUtil.Clamp(body.Aim + pitchErr, t.AimMin, t.AimMax);
        return MathF.Acos(MathUtil.Clamp(Vector3.Dot(want, have), -1f, 1f));
    }

    private static float Yaw(Vector3 d) => MathF.Atan2(-d.X, -d.Z);
}
