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
        float collapse = 0f, float collapseRoll = 0f, float aimSteadiness = 1f)
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
        // Lean into climbs (and back a little going downhill), a hint only: more reads as rocking.
        float slopeLean = MathUtil.Clamp(0.2f * body.Slope, -0.12f, 0.12f);
        float pelvisLean = -0.03f - 0.1f * run - slopeLean;
        pose.PelvisRotation = Rot.Yxz(pelvisLean - 0.3f * collapse, body.Psi - 0.07f * t.Sway * g * walkSway * c0, 0.025f * t.Sway * g * walkSway * cs - collapseRoll * collapse);

        // Foot targets first. If a planted leg would have to over-extend, the hips crouch (smoothed, at most 30 % of a
        // leg length) instead of snapping down. Swinging feet don't count: their targets jump around and they may
        // simply fall short in the air (the target clamp below).
        FootTarget target0 = Target(0, gait, body, t, collapse), target1 = Target(1, gait, body, t, collapse);
        float reach = 0.97f * t.LegLength, need = 0f;
        for (int i = 0; i < 2; i++)
        {
            if (gait.Legs[i].Swing) continue;
            var target = i == 0 ? target0 : target1;
            Vector3 root = pose.PelvisPosition + Vector3.Transform(new Vector3(-gait.Legs[i].Side * t.HipWidth, t.HipRootY, 0f), pose.PelvisRotation);
            float planar = MathUtil.PlanarDistance(root, target.Ankle);
            float maxRootY = target.Ankle.Y + MathF.Sqrt(MathF.Max(0f, reach * reach - planar * planar));
            need = MathF.Max(need, root.Y - maxRootY);
        }
        // Per call (one per 120 Hz tick): ease toward the needed crouch at ~2.4/s, never faster than 1.5 m/s, so it is
        // a posture over rough ground, not a bob per step (faster tracking only made the body pump).
        float crouchTarget = MathUtil.Clamp(need, 0f, 0.3f * t.LegLength);
        pose.Crouch += MathUtil.Clamp((crouchTarget - pose.Crouch) * 0.02f, -0.0125f * t.Scale, 0.0125f * t.Scale);
        pose.PelvisPosition.Y -= pose.Crouch;

        // Gyro-stabilised torso: it cancels 70 % of the pelvis lean so the cockpit and guns stay steady while the hips
        // and legs work the slope (not while collapsing).
        pose.TorsoRotation = Rot.Yxz(
            -(sec.TorsoPitch.X + 0.01f * jn) - 0.7f * pelvisLean,
            body.Twist + sec.TorsoYaw.X,
            -(sec.TorsoRoll.X + 0.015f * jn));
        pose.HeadRotation = Rot.Yxz(body.Aim * 0.85f - 0.006f * jn, 0f, -0.01f * jn);
        Quaternion torsoRot = pose.PelvisRotation * pose.TorsoRotation;
        Vector3 torsoBasePos = pose.PelvisPosition + Vector3.Transform(new Vector3(0f, t.TorsoBaseY, 0f), pose.PelvisRotation);
        Vector3 torsoPos = torsoBasePos + Vector3.Transform(new Vector3(0f, t.TorsoY, 0f), torsoRot);
        pose.TorsoWorldPosition = torsoPos;
        pose.TorsoWorldRotation = torsoRot;
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

        // Aim ray: the pilot's commanded line (legs + twist, aim pitch) plus what the stabilised weapon mounts don't
        // cancel of the torso's actual motion (sway, springs, kicks). Weapons converge on it.
        Vector3 torsoFwd = Vector3.Transform(-Vector3.UnitZ, torsoRot);
        float residual = 1f - t.AimStabilization * aimSteadiness;
        float aimYaw = body.Psi + body.Twist + residual * MathUtil.WrapAngle(MathF.Atan2(-torsoFwd.X, -torsoFwd.Z) - body.Psi - body.Twist);
        float aimPitch = body.Aim - residual * 0.25f * sec.TorsoPitch.X;
        pose.AimOrigin = torsoPos + Vector3.Transform(new Vector3(0f, t.AimOriginY, 0f), torsoRot);
        pose.AimDirection = MathUtil.Forward(aimYaw) * MathF.Cos(aimPitch) + Vector3.UnitY * MathF.Sin(aimPitch);

        Quaternion headRot = torsoRot * pose.HeadRotation;
        Vector3 headPos = torsoPos + Vector3.Transform(t.HeadOffset, torsoRot);
        pose.HitVolumes[(int)MechSection.Torso] = Sphere(MechSection.Torso, torsoPos + Vector3.Transform(t.TorsoHitOffset, torsoRot), t.TorsoHitRadius);
        pose.HitVolumes[(int)MechSection.ArmLeft] = Sphere(MechSection.ArmLeft, Elbow(pose, 0, t.ArmHitOffset), t.ArmHitRadius);
        pose.HitVolumes[(int)MechSection.ArmRight] = Sphere(MechSection.ArmRight, Elbow(pose, 1, t.ArmHitOffset), t.ArmHitRadius);
        pose.HitVolumes[(int)MechSection.Legs] = Sphere(MechSection.Legs, pose.PelvisPosition - new Vector3(0f, t.LegsHitDrop, 0f), t.LegsHitRadius);
        pose.HitVolumes[(int)MechSection.Cockpit] = Sphere(MechSection.Cockpit, headPos + Vector3.Transform(t.CockpitHitOffset, headRot), t.CockpitHitRadius);

        // Cover plates ride on the shoulders (left as defined, right mirrored) and belong to that arm.
        int plates = t.ShoulderCover.Length;
        if (pose.HitBoxes.Length != 2 * plates) pose.HitBoxes = new HitBox[2 * plates];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? 1f : -1f;
            Vector3 shoulderPos = torsoPos + Vector3.Transform(new Vector3(-side * t.ShoulderOffset.X, t.ShoulderOffset.Y, t.ShoulderOffset.Z), torsoRot);
            Quaternion shoulderRot = torsoRot * pose.Shoulders[i];
            for (int k = 0; k < plates; k++)
            {
                var c = t.ShoulderCover[k];
                Vector3 local = i == 0 ? c.Center : new Vector3(-c.Center.X, c.Center.Y, c.Center.Z);
                pose.HitBoxes[i * plates + k] = new HitBox
                {
                    Section = i == 0 ? MechSection.ArmLeft : MechSection.ArmRight,
                    Center = shoulderPos + Vector3.Transform(local, shoulderRot),
                    Rotation = shoulderRot,
                    HalfExtents = 0.5f * c.Size,
                };
            }
        }

        Quaternion pelvisInv = Quaternion.Inverse(pose.PelvisRotation);
        for (int i = 0; i < gait.Legs.Length; i++)
        {
            var leg = gait.Legs[i];
            var lp = pose.Legs[i];
            var target = i == 0 ? target0 : target1;

            Vector3 rootLocal = new(-leg.Side * t.HipWidth, t.HipRootY, 0f);
            Vector3 rootWorld = pose.PelvisPosition + Vector3.Transform(rootLocal, pose.PelvisRotation);
            Vector3 d = Sanitize(Vector3.Transform(target.Ankle - rootWorld, pelvisInv), t.LegLength, collapse);

            // Foot orientation is world-space: it keeps its planted yaw while the body turns over it.
            Quaternion footWorld = Rot.YawOnSlope(target.Yaw, target.Normal) * Rot.X(-target.Pitch);
            lp.Target = target;
            lp.HipWorld = rootWorld;
            if (t.Legs == LegLayout.Digitigrade)
            {
                var angles = LegIK.SolveDigitigrade(d, t.L1, t.L2, t.L3);
                lp.Angles = angles;
                lp.Hip = LegIK.HipRotation(angles);
                lp.Knee = LegIK.KneeRotation(angles);
                lp.Hock = LegIK.HockRotation(angles);
                Quaternion hockWorld = pose.PelvisRotation * lp.Hip * lp.Knee * lp.Hock;
                lp.Ankle = LimitAnkle(Quaternion.Normalize(Quaternion.Inverse(hockWorld) * footWorld));
                footWorld = hockWorld * lp.Ankle;
                var (knee, hock, toe) = LegIK.ForwardDigitigrade(angles, t.L1, t.L2, t.L3);
                lp.KneeWorld = rootWorld + Vector3.Transform(knee, pose.PelvisRotation);
                lp.HockWorld = rootWorld + Vector3.Transform(hock, pose.PelvisRotation);
                lp.AnkleWorld = rootWorld + Vector3.Transform(toe, pose.PelvisRotation);
            }
            else
            {
                var angles = LegIK.Solve(d, t.L1, t.L2);
                lp.Angles = angles;
                lp.Hip = LegIK.HipRotation(angles);
                lp.Knee = LegIK.KneeRotation(angles);
                lp.Hock = Quaternion.Identity;
                Quaternion kneeWorld = pose.PelvisRotation * lp.Hip * lp.Knee;
                lp.Ankle = LimitAnkle(Quaternion.Normalize(Quaternion.Inverse(kneeWorld) * footWorld));
                footWorld = kneeWorld * lp.Ankle;
                var (knee, ankle) = LegIK.Forward(angles, t.L1, t.L2);
                lp.KneeWorld = rootWorld + Vector3.Transform(knee, pose.PelvisRotation);
                lp.AnkleWorld = rootWorld + Vector3.Transform(ankle, pose.PelvisRotation);
                lp.HockWorld = lp.AnkleWorld;
            }
            lp.FootWorld = footWorld;
        }
    }

    /// <summary>
    /// The deliberate cheat: keep the IK's input well-posed instead of trusting it with anything. In the hip-root
    /// frame the foot stays at least 0.38 leg lengths below the hip (knee at most ~135°, enough to climb a parapet; relaxed to
    /// 0.25 as a wreck slumps onto its legs), within 20° of hip roll, and within 97 % of the leg's reach (pulled in
    /// horizontally). The foot may then hover a little or land short in extreme spots; the leg never twists.
    /// </summary>
    private static Vector3 Sanitize(Vector3 d, float legLength, float collapse)
    {
        float minDown = MathUtil.Lerp(0.38f, 0.25f, collapse) * legLength, reach = 0.97f * legLength;
        if (-d.Y < minDown) d.Y = -minDown;
        float side = MathF.Tan(0.35f) * -d.Y;
        d.X = MathUtil.Clamp(d.X, -side, side);
        float len = d.Length();
        if (len > reach)
        {
            float horiz = MathF.Sqrt(d.X * d.X + d.Z * d.Z);
            float want = MathF.Sqrt(MathF.Max(0f, reach * reach - d.Y * d.Y));
            if (horiz > 1e-4f && want > 0f) { d.X *= want / horiz; d.Z *= want / horiz; }
            else d *= reach / len;
        }
        return d;
    }

    /// <summary>Ankles bend at most ~45° from the shin's frame; past that the sole stops following the ground.</summary>
    private static Quaternion LimitAnkle(Quaternion q)
    {
        const float max = 0.8f;
        float angle = 2f * MathF.Acos(MathUtil.Clamp(MathF.Abs(q.W), 0f, 1f));
        return angle <= max ? q : Quaternion.Slerp(Quaternion.Identity, q.W < 0f ? Quaternion.Negate(q) : q, max / angle);
    }

    private static FootTarget Target(int leg, GaitController gait, BodyState body, MechChassis t, float collapse)
    {
        var target = gait.Evaluate(leg, body, t);
        if (collapse > 0f)
        {
            // Feet are all planted when collapsing; flatten them onto the ground.
            target.Pitch *= 1f - collapse;
            target.Ankle.Y = MathUtil.Lerp(target.Ankle.Y, gait.Legs[leg].Plant.Position.Y + t.AnkleHeight, collapse);
        }
        return target;
    }

    private static HitVolume Sphere(MechSection s, Vector3 c, float r) => new() { Section = s, Center = c, Radius = r };

    private static Vector3 Elbow(MechPose pose, int arm, Vector3 local) =>
        pose.ElbowWorldPositions[arm] + Vector3.Transform(local, pose.ElbowWorldRotations[arm]);
}
