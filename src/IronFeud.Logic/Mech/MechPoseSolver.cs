using System;
using System.Numerics;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Logic.Mech;

/// <summary>
/// Turns simulation state into joint rotations. This is the single place where semantic angles
/// (pitch + = down, roll + = lean right) become Godot Euler angles: X and Z flip sign, Y does not.
/// </summary>
public static class MechPoseSolver
{
    /// <param name="collapse">0..1 slump of a mech that is out (pelvis drops and pitches, arms hang).</param>
    public static void Solve(MechPose pose, BodyState body, GaitController gait, SecondaryMotion sec, MechChassis t, float time,
        float collapse = 0f, float collapseRoll = 0f)
    {
        float p = gait.Phase, g = gait.G;
        float jn = sec.Jank(time, t);
        float c0 = MathF.Cos(MathUtil.Tau * p);
        float cs = MathF.Cos(MathUtil.Tau * (p - 0.31f));
        pose.Jank = jn;

        // Pelvis: lateral sway toward the stance leg (+ = left), bob lowest just after footfall, counter-yaw.
        // Side-to-side sway is a walking thing; at a running cadence it reads as a waddle, so it fades with the run.
        float s = t.Scale, run = gait.Run, walkSway = 1f - 0.75f * run;
        float swayLeft = 0.32f * s * t.Sway * g * walkSway * cs;
        Vector3 sway = MathUtil.RotateY(new Vector3(-swayLeft, 0f, 0f), body.Psi);
        // Running: the bounce grows and its low point moves to mid-stance (leg compression), high in the flight phase;
        // the hips ride higher (straighter legs) and the body leans into the run.
        float bob = -0.17f * s * t.Bob * g * (1f + 0.8f * run) * MathF.Cos(MathUtil.Tau * (2f * p - MathUtil.Lerp(0.1f, gait.Duty, run)));
        float hipRaise = 0.25f * s * run;
        float drop = t.CollapseDrop * collapse;
        pose.PelvisPosition = body.Position + sway + new Vector3(0f, t.HipHeight + hipRaise + bob + sec.BounceY.X + 0.09f * s * jn - drop, 0f);
        pose.PelvisRotation = Rot.Yxz(-0.03f - 0.1f * run - 0.3f * collapse, body.Psi - 0.07f * t.Sway * g * walkSway * c0, 0.025f * t.Sway * g * walkSway * cs - collapseRoll * collapse);

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
            pose.Shoulders[i] = Rot.X(body.Aim - sec.Arms[i].X - 0.6f * collapse);
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

        Quaternion headRot = torsoRot * pose.HeadRotation;
        Vector3 headPos = torsoPos + Vector3.Transform(t.HeadOffset, torsoRot);
        pose.HitVolumes[(int)MechSection.Torso] = Sphere(MechSection.Torso, torsoPos + Vector3.Transform(t.TorsoHitOffset, torsoRot), t.TorsoHitRadius);
        pose.HitVolumes[(int)MechSection.ArmLeft] = Sphere(MechSection.ArmLeft, Elbow(pose, 0, t.ArmHitOffset), t.ArmHitRadius);
        pose.HitVolumes[(int)MechSection.ArmRight] = Sphere(MechSection.ArmRight, Elbow(pose, 1, t.ArmHitOffset), t.ArmHitRadius);
        pose.HitVolumes[(int)MechSection.Legs] = Sphere(MechSection.Legs, pose.PelvisPosition - new Vector3(0f, t.LegsHitDrop, 0f), t.LegsHitRadius);
        pose.HitVolumes[(int)MechSection.Cockpit] = Sphere(MechSection.Cockpit, headPos + Vector3.Transform(t.CockpitHitOffset, headRot), t.CockpitHitRadius);

        Quaternion pelvisInv = Quaternion.Inverse(pose.PelvisRotation);
        for (int i = 0; i < gait.Legs.Length; i++)
        {
            var leg = gait.Legs[i];
            var lp = pose.Legs[i];
            var target = gait.Evaluate(i, body, t);
            if (collapse > 0f)
            {
                // Feet are all planted when collapsing; flatten them onto the ground.
                target.Pitch *= 1f - collapse;
                target.Ankle.Y = MathUtil.Lerp(target.Ankle.Y, body.Position.Y + t.AnkleHeight, collapse);
            }

            Vector3 rootLocal = new(-leg.Side * t.HipWidth, t.HipRootY, 0f);
            Vector3 rootWorld = pose.PelvisPosition + Vector3.Transform(rootLocal, pose.PelvisRotation);
            Vector3 d = Vector3.Transform(target.Ankle - rootWorld, pelvisInv);

            // Foot orientation is world-space: it keeps its planted yaw while the body turns over it.
            Quaternion footWorld = Rot.Y(target.Yaw) * Rot.X(-target.Pitch);
            lp.Target = target;
            lp.HipWorld = rootWorld;
            if (t.Legs == LegLayout.Digitigrade)
            {
                var angles = LegIK.SolveDigitigrade(d, t.L1, t.L2, t.L3);
                lp.Hip = LegIK.HipRotation(angles);
                lp.Knee = LegIK.KneeRotation(angles);
                lp.Hock = LegIK.HockRotation(angles);
                Quaternion hockWorld = pose.PelvisRotation * lp.Hip * lp.Knee * lp.Hock;
                lp.Ankle = Quaternion.Normalize(Quaternion.Inverse(hockWorld) * footWorld);
                var (knee, hock, toe) = LegIK.ForwardDigitigrade(angles, t.L1, t.L2, t.L3);
                lp.KneeWorld = rootWorld + Vector3.Transform(knee, pose.PelvisRotation);
                lp.HockWorld = rootWorld + Vector3.Transform(hock, pose.PelvisRotation);
                lp.AnkleWorld = rootWorld + Vector3.Transform(toe, pose.PelvisRotation);
            }
            else
            {
                var angles = LegIK.Solve(d, t.L1, t.L2);
                lp.Hip = LegIK.HipRotation(angles);
                lp.Knee = LegIK.KneeRotation(angles);
                lp.Hock = Quaternion.Identity;
                Quaternion kneeWorld = pose.PelvisRotation * lp.Hip * lp.Knee;
                lp.Ankle = Quaternion.Normalize(Quaternion.Inverse(kneeWorld) * footWorld);
                var (knee, ankle) = LegIK.Forward(angles, t.L1, t.L2);
                lp.KneeWorld = rootWorld + Vector3.Transform(knee, pose.PelvisRotation);
                lp.AnkleWorld = rootWorld + Vector3.Transform(ankle, pose.PelvisRotation);
                lp.HockWorld = lp.AnkleWorld;
            }
            lp.FootWorld = footWorld;
        }
    }

    private static HitVolume Sphere(MechSection s, Vector3 c, float r) => new() { Section = s, Center = c, Radius = r };

    private static Vector3 Elbow(MechPose pose, int arm, Vector3 local) =>
        pose.ElbowWorldPositions[arm] + Vector3.Transform(local, pose.ElbowWorldRotations[arm]);
}
