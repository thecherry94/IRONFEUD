using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Core;

namespace IronFeud.Logic.Mech;

/// <summary>A foot's ground contact: planar position (y = ground) and yaw.</summary>
public struct FootPlant
{
    public Vector3 Position;
    public float Yaw;

    public FootPlant(Vector3 position, float yaw)
    {
        Position = position;
        Yaw = yaw;
    }
}

public sealed class LegGait
{
    /// <summary>+1 = left leg, -1 = right leg. The left hip sits at local -X.</summary>
    public readonly int Side;
    public readonly float PhaseOffset;

    /// <summary>Stance fraction for the current step, latched at each landing so a speed change never skips a swing.</summary>
    public float Duty;

    /// <summary>Stride length multiplier for the current swing, chosen at lift-off to find a good foothold.</summary>
    public float StrideScale = 1f;

    public FootPlant Plant;
    public FootPlant From;
    public bool Swing;
    public float StepLength;

    public LegGait(int side, float phaseOffset)
    {
        Side = side;
        PhaseOffset = phaseOffset;
    }
}

/// <summary>Where the ankle should be this tick and how the foot is oriented.</summary>
public struct FootTarget
{
    public Vector3 Ankle;
    public float Yaw;

    /// <summary>Foot pitch, positive = toe down (heel lifted).</summary>
    public float Pitch;

    /// <summary>Predicted landing spot while swinging; equals the plant during stance.</summary>
    public FootPlant Landing;

    /// <summary>Ground normal the sole lines up with (blended from take-off to landing during a swing).</summary>
    public Vector3 Normal;
}

public struct Footfall
{
    public int Leg;

    /// <summary>Foot centre on the ground (slightly ahead of the ankle).</summary>
    public Vector3 Position;
    public float Yaw;

    /// <summary>Impact strength k, already scaled by the Impact multiplier.</summary>
    public float Strength;
    public float SpeedFraction;
}

/// <summary>
/// Gait phase and planted-foot bookkeeping. Feet in stance never move; swinging feet travel from their
/// last plant to a landing target predicted from the body's current velocity and turn rate.
/// </summary>
public sealed class GaitController
{
    public readonly LegGait[] Legs = { new(+1, 0f), new(-1, 0.5f) };

    /// <summary>Ground the feet are planted on; null = flat at y = 0.</summary>
    public IGround? Ground;

    /// <summary>Stride limit multiplier (shorter strides on slopes); set by the locomotion each tick.</summary>
    public float ReachScale = 1f;

    private float AMax(MechChassis t) => t.AMax * ReachScale;

    public float Phase;

    /// <summary>Cycle frequency, Hz.</summary>
    public float F;

    /// <summary>Gait amplitude 0..1; eases with activity and scales sway/bob.</summary>
    public float G;

    /// <summary>Stance fraction for the current speed: walk (≈0.62) blending to run (≈0.32, both feet off the ground between steps).</summary>
    public float Duty;

    /// <summary>0 = walking, 1 = fully running; drives the running posture (hip height, bounce, lean, foot lift).</summary>
    public float Run;

    public bool Active;

    /// <summary>In the air on jump jets: no gait, the legs follow <see cref="Tuck"/> instead.</summary>
    public bool Airborne { get; private set; }

    /// <summary>0..1 in the air: 1 = legs tucked up and trailing (rising), 0 = reaching down for the landing.</summary>
    public float Tuck;

    public void Reset(BodyState body, MechChassis t)
    {
        Airborne = false;
        Tuck = 0f;
        Phase = 0f;
        F = t.FMin;
        Duty = t.Duty;
        G = 0f;
        Active = false;
        foreach (var leg in Legs)
        {
            leg.Plant = Neutral(leg, body, t, Ground);
            leg.From = leg.Plant;
            leg.Swing = false;
            leg.StepLength = 0f;
            leg.Duty = t.Duty;
        }
    }

    public static Vector3 HipOffset(LegGait leg, MechChassis t) => new(-leg.Side * t.HipWidth, 0f, 0f);

    public static FootPlant Neutral(LegGait leg, BodyState body, MechChassis t, IGround? ground = null) =>
        new(ground.OnGround(body.Position + MathUtil.RotateY(HipOffset(leg, t), body.Psi)), body.Psi);

    /// <summary>Where the foot will land if the body keeps its current v and ω for <paramref name="remaining"/> seconds.</summary>
    public FootPlant Landing(LegGait leg, BodyState body, MechChassis t, float remaining)
    {
        float psiL = body.Psi + body.Omega * remaining;
        Vector3 p = body.Position + MathUtil.Forward(body.Psi) * (body.V * remaining);
        float halfStride = MathUtil.Clamp(body.V / F * Duty / 2f, -AMax(t), AMax(t)) * leg.StrideScale;
        Vector3 local = HipOffset(leg, t) + new Vector3(0f, 0f, -halfStride);
        return new FootPlant(Ground.OnGround(p + MathUtil.RotateY(local, psiL)), psiL);
    }

    public void Step(BodyState body, MechChassis t, float h, List<Footfall>? footfalls)
    {
        float spd = MathF.Abs(body.V);
        bool unsettled = false;
        foreach (var leg in Legs)
        {
            if (leg.Swing) { unsettled = true; continue; }
            var n = Neutral(leg, body, t, Ground);
            if (MathUtil.PlanarDistance(leg.Plant.Position, n.Position) > 0.45f * t.Scale ||
                MathF.Abs(MathUtil.WrapAngle(leg.Plant.Yaw - body.Psi)) > 0.2f)
                unsettled = true;
        }

        Active = spd > 0.12f || MathF.Abs(body.Omega) > 0.04f || unsettled;
        Run = t.RunBlend(spd);
        Duty = t.DutyAt(spd);
        // Stride is limited by leg reach (stance travel v·duty/f ≤ 2·AMax) and, while walking, by DMax. The DMax cap
        // fades out with the run, otherwise a fast mech can only speed up by shuffling ever faster.
        // A planted leg keeps the duty it latched at landing (longer while accelerating), so size the cadence for the
        // longest stance in progress.
        float stanceDuty = Duty;
        foreach (var leg in Legs)
            if (!leg.Swing) stanceDuty = MathF.Max(stanceDuty, leg.Duty);
        F = MathF.Max(t.FMin, MathF.Max((1f - Run) * spd / t.DMax, spd * stanceDuty / (2f * AMax(t))));
        float gT = Active ? MathUtil.Clamp01(0.3f + spd / t.VMax + 0.5f * MathF.Abs(body.Omega) / t.OmegaMax) : 0f;
        G += (gT - G) * MathF.Min(1f, h * 2.5f);
        if (!Active) return;

        float prev = Phase;
        Phase = MathUtil.Frac(Phase + F * h);
        for (int i = 0; i < Legs.Length; i++)
        {
            var leg = Legs[i];
            float pa = MathUtil.Frac(prev + leg.PhaseOffset);
            float pb = MathUtil.Frac(Phase + leg.PhaseOffset);
            if (pa < leg.Duty && pb >= leg.Duty)
            {
                leg.Swing = true;
                leg.From = leg.Plant;
                leg.StrideScale = ChooseFoothold(leg, body, t, (1f - leg.Duty) / F);
            }
            if (pb < pa)
            {
                leg.Swing = false;
                leg.Plant = Landing(leg, body, t, 0f);
                leg.StepLength = MathUtil.PlanarDistance(leg.Plant.Position, leg.From.Position);
                leg.Duty = Duty;
                // Fast cadences would saturate the springs; above a walking cadence (0.7 Hz) each impact is softened.
                float k = t.Impact * (0.45f + 0.55f * MathF.Min(1f, leg.StepLength / (3f * t.Scale))) * MathF.Min(1f, 0.7f / F);
                footfalls?.Add(new Footfall
                {
                    Leg = i,
                    Position = leg.Plant.Position + MathUtil.Forward(leg.Plant.Yaw) * (0.3f * t.Scale),
                    Yaw = leg.Plant.Yaw,
                    Strength = k,
                    SpeedFraction = spd / t.VMax,
                });
            }
        }
    }

    /// <summary>Feet leave the ground: every leg counts as swinging (so the hips don't crouch for them).</summary>
    public void TakeOff()
    {
        Airborne = true;
        foreach (var leg in Legs)
        {
            leg.From = leg.Plant;
            leg.Swing = true;
        }
    }

    /// <summary>
    /// One airborne tick: the legs tuck and trail while rising, and reach down for the ground once touchdown is
    /// less than ~0.6 s away (<paramref name="timeToGround"/>, infinity while rising).
    /// </summary>
    public void Air(float timeToGround, float h)
    {
        float tuckT = MathUtil.Smoothstep(0.15f, 0.6f, timeToGround);
        Tuck += (tuckT - Tuck) * MathF.Min(1f, h * 5f);
        G = MathF.Max(0f, G - h * 3f);
        Active = false;
    }

    /// <summary>
    /// Touchdown: both feet plant where they are (on the ground under them) and each lands a footfall scaled by the
    /// impact speed, which drives the squat, dust, sound and shake like any step, only much harder.
    /// </summary>
    public void Land(BodyState body, MechChassis t, float impactSpeed, List<Footfall>? footfalls)
    {
        Airborne = false;
        float k = t.Impact * MathUtil.Clamp(impactSpeed / 8f, 0.6f, 1.8f);
        for (int i = 0; i < Legs.Length; i++)
        {
            var leg = Legs[i];
            var target = AirTarget(leg, body, t); // where the foot is in the air, not the old plant
            leg.Plant = new FootPlant(Ground.OnGround(target.Ankle), body.Psi);
            leg.From = leg.Plant;
            leg.Swing = false;
            leg.StepLength = 0f;
            leg.Duty = t.DutyAt(MathF.Abs(body.V));
            footfalls?.Add(new Footfall
            {
                Leg = i,
                Position = leg.Plant.Position + MathUtil.Forward(leg.Plant.Yaw) * (0.3f * t.Scale),
                Yaw = leg.Plant.Yaw,
                Strength = k,
                SpeedFraction = MathF.Abs(body.V) / t.VMax,
            });
        }
        // Restart the cycle, and give each leg the state its phase calls for: at a running duty the second leg is
        // already in its swing, so it springs straight into the next stride instead of staying planted (and left
        // metres behind) for a whole cycle.
        Phase = 0f;
        Tuck = 0f;
        Duty = t.DutyAt(MathF.Abs(body.V));
        F = MathF.Max(t.FMin, MathF.Abs(body.V) * Duty / (2f * AMax(t)));
        foreach (var leg in Legs)
        {
            if (MathUtil.Frac(leg.PhaseOffset) < leg.Duty) continue;
            leg.Swing = true;
            leg.StrideScale = ChooseFoothold(leg, body, t, (1f - leg.Duty) / F);
        }
    }

    /// <summary>
    /// Feet in flight, relative to the body: tucked (raised 0.22 leg lengths, toes down) and trailing while rising,
    /// stretched down under the hips, one a little ahead, as the ground comes up.
    /// </summary>
    private FootTarget AirTarget(LegGait leg, BodyState body, MechChassis t)
    {
        float L = t.LegLength, stagger = leg.Side * 0.08f * L;
        float back = MathUtil.Lerp(-0.04f * L, 0.22f * L, Tuck) + stagger;
        Vector3 local = HipOffset(leg, t) + new Vector3(0f, 0f, back);
        Vector3 ankle = body.Position + MathUtil.RotateY(local, body.Psi) + new Vector3(0f, t.AnkleHeight + 0.22f * L * Tuck, 0f);
        ankle.Y = MathF.Max(ankle.Y, Ground.HeightAt(ankle.X, ankle.Z) + t.AnkleHeight); // skim a crest, don't dig into it
        var plant = new FootPlant(Ground.OnGround(ankle), body.Psi);
        return new FootTarget { Ankle = ankle, Yaw = body.Psi, Pitch = 0.4f * Tuck, Landing = plant, Normal = Vector3.UnitY };
    }

    private static readonly float[] StrideOptions = { 1f, 0.85f, 1.15f, 0.7f, 0.55f, 1.3f };

    /// <summary>
    /// Foothold selection (a shortcut real foot-placement planners use too): of a few stride lengths, take the landing
    /// whose ground is closest to the height under the body at touchdown and not on a steep face, preferring the
    /// nominal stride. Keeps feet out of trench bottoms and off crest tops the body is merely passing.
    /// </summary>
    private float ChooseFoothold(LegGait leg, BodyState body, MechChassis t, float swingTime)
    {
        if (Ground == null) return 1f;
        Vector3 bodyAt = body.Position + MathUtil.Forward(body.Psi) * (body.V * swingTime);
        float reference = Ground.Height(bodyAt.X, bodyAt.Z);
        float best = 1f, bestCost = float.MaxValue;
        foreach (float s in StrideOptions)
        {
            leg.StrideScale = s;
            var p = Landing(leg, body, t, swingTime).Position;
            float rise = MathF.Abs(p.Y - reference) / t.LegLength;
            float cost = rise + (rise > 0.3f ? 1f : 0f) // a step of more than 0.3 leg lengths is a last resort
                         + 2f * (1f - Ground.NormalAt(p.X, p.Z, t.Scale).Y)
                         + 0.15f * MathF.Abs(1f - s);
            if (cost < bestCost) { bestCost = cost; best = s; }
        }
        return best;
    }

    /// <summary>The sole follows the ground up to 30° of tilt; on steeper walls it stays at 30° (the toes dig in).</summary>
    private static Vector3 SoleNormal(Vector3 n)
    {
        const float maxTilt = 0.52f;
        if (n.Y >= MathF.Cos(maxTilt)) return n;
        var flat = new Vector3(n.X, 0f, n.Z);
        float len = flat.Length();
        return len < 1e-6f ? Vector3.UnitY : flat / len * MathF.Sin(maxTilt) + Vector3.UnitY * MathF.Cos(maxTilt);
    }

    /// <summary>Ankle target and foot orientation for the current phase (heel-strike / toe-off, swing arc).</summary>
    public FootTarget Evaluate(int legIndex, BodyState body, MechChassis t)
    {
        var leg = Legs[legIndex];
        if (Airborne) return AirTarget(leg, body, t);
        float lp = MathUtil.Frac(Phase + leg.PhaseOffset);
        float lift = 0f, outward = 0f, pitch, yaw;
        float s0 = t.Scale;
        float scale = MathUtil.Clamp01(leg.StepLength / (3f * s0));
        float normalSpan = 1.5f * s0;
        Vector3 cur, normal;
        FootPlant landing;

        if (!leg.Swing)
        {
            cur = leg.Plant.Position;
            yaw = leg.Plant.Yaw;
            landing = leg.Plant;
            normal = SoleNormal(Ground.NormalAt(cur.X, cur.Z, normalSpan));
            pitch = 0.38f * MathUtil.Smoothstep(leg.Duty - 0.16f, leg.Duty, lp) - 0.14f * (1f - MathUtil.Smoothstep(0f, 0.07f, lp));
        }
        else
        {
            float s = MathUtil.Clamp01((lp - leg.Duty) / (1f - leg.Duty));
            landing = Landing(leg, body, t, (1f - s) * (1f - leg.Duty) / F);
            float e = MathUtil.Smootherstep(s);
            cur = Vector3.Lerp(leg.From.Position, landing.Position, e);
            cur.Y = MathF.Max(cur.Y, Ground.HeightAt(cur.X, cur.Z)); // clear a crest between the two footholds
            yaw = MathUtil.LerpAngle(leg.From.Yaw, landing.Yaw, e);
            normal = SoleNormal(Vector3.Normalize(Vector3.Lerp(
                Ground.NormalAt(leg.From.Position.X, leg.From.Position.Z, normalSpan),
                Ground.NormalAt(landing.Position.X, landing.Position.Z, normalSpan), e)));
            float dist = MathUtil.PlanarDistance(landing.Position, leg.From.Position);
            scale = MathUtil.Clamp01(dist / (3f * s0));
            // Walking: fast lift, hang, hard drop. Running: a much higher, earlier arc (the foot is pulled up under
            // the hip as it recovers) instead of skimming the ground.
            float walkLift = 0.95f * s0 * MathUtil.Clamp(dist / (1.6f * s0), 0.35f, 1f);
            float runLift = 0.28f * t.LegLength;
            lift = t.Lift * MathUtil.Lerp(walkLift, runLift, Run) * MathF.Sin(MathF.PI * MathF.Pow(s, MathUtil.Lerp(0.75f, 0.55f, Run)));
            pitch = MathUtil.Lerp(0.38f, -0.14f, MathUtil.Smoothstep(0f, 0.75f, s));
            outward = 0.22f * s0 * MathF.Sin(MathF.PI * s) * MathUtil.Clamp01(dist / (2f * s0));
        }

        pitch *= scale * (body.V < -0.1f ? 0.3f : 1f);
        // Raise the ankle so the pivot (toe when toe-down, heel when toe-up) stays on the ground.
        float rise = pitch > 0f ? t.ToeLength * MathF.Sin(pitch) : t.HeelLength * MathF.Sin(-pitch);
        Vector3 outOffset = MathUtil.RotateY(new Vector3(-leg.Side * outward, 0f, 0f), yaw);

        return new FootTarget
        {
            Ankle = cur + outOffset + new Vector3(0f, t.AnkleHeight + rise + lift, 0f),
            Yaw = yaw,
            Pitch = pitch,
            Landing = landing,
            Normal = normal,
        };
    }
}
