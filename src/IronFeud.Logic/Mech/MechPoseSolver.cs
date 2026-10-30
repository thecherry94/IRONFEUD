using System;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>
/// Turns simulation state into joint rotations. This is the single place where semantic angles
/// (pitch + = down, roll + = lean right) become Godot Euler angles: X and Z flip sign, Y does not.
/// </summary>
public static class MechPoseSolver
{
    public static void Solve(MechPose pose, BodyState body, GaitController gait, SecondaryMotion sec, MechTuning t, float time)
    {
        float p = gait.Phase, g = gait.G;
        float jn = sec.Jank(time, t);
        float c0 = MathF.Cos(MathUtil.Tau * p);
        float cs = MathF.Cos(MathUtil.Tau * (p - 0.31f));
        pose.Jank = jn;

        // Pelvis: lateral sway toward the stance leg (+ = left), bob lowest just after footfall, counter-yaw.
        float swayLeft = 0.32f * t.Sway * g * cs;
        Vector3 sway = MathUtil.RotateY(new Vector3(-swayLeft, 0f, 0f), body.Psi);
        float bob = -0.17f * t.Bob * g * MathF.Cos(MathUtil.Tau * (2f * p - 0.1f));
        pose.PelvisPosition = body.Position + sway + new Vector3(0f, t.HipHeight + bob + sec.BounceY.X + 0.09f * jn, 0f);
        pose.PelvisRotation = Rot.Yxz(-0.03f, body.Psi - 0.07f * t.Sway * g * c0, 0.025f * t.Sway * g * cs);

        pose.TorsoRotation = Rot.Yxz(
            -(sec.TorsoPitch.X + 0.01f * jn),
            body.Twist + sec.TorsoYaw.X,
            -(sec.TorsoRoll.X + 0.015f * jn));
        pose.HeadRotation = Rot.Yxz(body.Aim * 0.85f - 0.006f * jn, 0f, -0.01f * jn);
        Quaternion torsoRot = pose.PelvisRotation * pose.TorsoRotation;
        Vector3 torsoBasePos = pose.PelvisPosition + Vector3.Transform(new Vector3(0f, t.TorsoBaseY, 0f), pose.PelvisRotation);
        Vector3 torsoPos = torsoBasePos + Vector3.Transform(new Vector3(0f, t.TorsoY, 0f), torsoRot);
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? 1f : -1f;
            pose.Shoulders[i] = Rot.X(body.Aim - sec.Arms[i].X);
            float elbowJank = i == 0 ? 0.02f * jn : -0.02f * jn;
            pose.Elbows[i] = Rot.X(-(sec.Elbows[i].X * 0.25f + elbowJank));

            var shoulderOffset = new Vector3(-side * t.ShoulderOffset.X, t.ShoulderOffset.Y, t.ShoulderOffset.Z);
            var elbowOffset = new Vector3(-side * t.ElbowOffset.X, t.ElbowOffset.Y, t.ElbowOffset.Z);
            Vector3 shoulderPos = torsoPos + Vector3.Transform(shoulderOffset, torsoRot);
            Quaternion shoulderRot = torsoRot * pose.Shoulders[i];
            pose.ElbowWorldPositions[i] = shoulderPos + Vector3.Transform(elbowOffset, shoulderRot);
            pose.ElbowWorldRotations[i] = shoulderRot * pose.Elbows[i];
        }

        // Aim ray: torso heading (twist + sway), pitch = aim minus a little of the torso's pitch wobble.
        Vector3 torsoFwd = Vector3.Transform(-Vector3.UnitZ, torsoRot);
        float aimYaw = MathF.Atan2(-torsoFwd.X, -torsoFwd.Z);
        float aimPitch = body.Aim - 0.25f * sec.TorsoPitch.X;
        pose.AimOrigin = torsoPos + Vector3.Transform(new Vector3(0f, t.AimOriginY, 0f), torsoRot);
        pose.AimDirection = MathUtil.Forward(aimYaw) * MathF.Cos(aimPitch) + Vector3.UnitY * MathF.Sin(aimPitch);

        Quaternion pelvisInv = Quaternion.Inverse(pose.PelvisRotation);
        for (int i = 0; i < gait.Legs.Length; i++)
        {
            var leg = gait.Legs[i];
            var lp = pose.Legs[i];
            var target = gait.Evaluate(i, body, t);

            Vector3 rootLocal = new(-leg.Side * t.HipWidth, t.HipRootY, 0f);
            Vector3 rootWorld = pose.PelvisPosition + Vector3.Transform(rootLocal, pose.PelvisRotation);
            Vector3 d = Vector3.Transform(target.Ankle - rootWorld, pelvisInv);

            var angles = LegIK.Solve(d, t.L1, t.L2);
            lp.Hip = LegIK.HipRotation(angles);
            lp.Knee = LegIK.KneeRotation(angles);

            // Foot orientation is world-space: it keeps its planted yaw while the body turns over it.
            Quaternion kneeWorld = pose.PelvisRotation * lp.Hip * lp.Knee;
            Quaternion footWorld = Rot.Y(target.Yaw) * Rot.X(-target.Pitch);
            lp.Ankle = Quaternion.Normalize(Quaternion.Inverse(kneeWorld) * footWorld);

            var (knee, ankle) = LegIK.Forward(angles, t.L1, t.L2);
            lp.Target = target;
            lp.HipWorld = rootWorld;
            lp.KneeWorld = rootWorld + Vector3.Transform(knee, pose.PelvisRotation);
            lp.AnkleWorld = rootWorld + Vector3.Transform(ankle, pose.PelvisRotation);
            lp.FootWorld = footWorld;
        }
    }
}
