using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Mech;

/// <summary>
/// A whole mech at the fixed tick: locomotion, then weapons. Weapons fire from this tick's pose;
/// their recoil impulses land in the springs and show up from the next tick on.
/// Damage arrives between ticks via <see cref="ApplyHit"/>.
/// </summary>
public sealed class MechSim
{
    public readonly MechLocomotion Locomotion;
    public readonly MechWeapons Weapons;

    /// <summary>Persistent per-section damage; pass one in to continue from a previous battle.</summary>
    public readonly MechDamage Damage;

    /// <summary>Shots fired during the last <see cref="Step"/>.</summary>
    public readonly List<ShotFired> Shots = new();

    public MechSim(MechChassis chassis, uint seed = 1, MechDamage? damage = null)
    {
        Locomotion = new MechLocomotion(chassis, seed);
        Weapons = MechWeapons.ForChassis(chassis);
        Damage = damage ?? MechDamage.Create(chassis);
        Stability = chassis.StabilityMax;
        ApplyDamageEffects();
    }

    public MechChassis Chassis => Locomotion.Chassis;
    public MechPose Pose => Locomotion.Pose;
    public bool IsOut => Damage.IsOut;

    /// <summary>Heat at which the reactor scrams: only heat put in from outside (flamethrowers, incendiaries) or jets
    /// can push a mech this far, its own guns lock at 1.</summary>
    public const float ShutdownHeat = 1.2f, ShutdownTime = 3f;

    /// <summary>
    /// The restart flushes the coolant: heat drops to <see cref="RestartHeat"/> and for <see cref="CoolantFlushTime"/>
    /// seconds outside heat doesn't stick. A flamer shuts a mech down but can't hold it down: the 3 s shutdown bled only
    /// ~0.17 heat, the mech restarted above the line and scrammed again on the spot (half of all shutdowns in lance
    /// games, chains of up to 15 s).
    /// </summary>
    public const float RestartHeat = 0.6f, CoolantFlushTime = 4f;

    /// <summary>
    /// Running hot: from <see cref="HotHeat"/> up to the weapon lock at 1 the legs lose up to <see cref="HotSpeedLoss"/>
    /// of their top speed and the gyro steadies the guns less (up to <see cref="HotAimLoss"/> of its stabilisation gone,
    /// so the aim sways with the body). A visible, physical slope before the cliff (the genre's heat scales penalise
    /// movement and aim long before shutdown; hidden penalties like an extra heat charge per volley were resented).
    /// </summary>
    public const float HotHeat = 0.6f, HotSpeedLoss = 0.15f, HotAimLoss = 0.5f;

    /// <summary>0 below <see cref="HotHeat"/>, 1 at the weapon lock: how far into the running-hot band the mech is.</summary>
    public float Hot => Core.MathUtil.Smoothstep(HotHeat, 1f, Weapons.Heat);

    /// <summary>Seconds left of a heat shutdown: no legs, no guns, the torso slack; the heat sinks keep working.</summary>
    public float ShutdownLeft { get; private set; }
    public bool Shutdown => ShutdownLeft > 0f;

    /// <summary>Seconds left of the coolant flush after a restart (outside heat doesn't stick).</summary>
    public float FlushLeft { get; private set; }

    /// <summary>Heat put in from outside (flamethrowers, incendiary rounds); returns what stuck (none while flushing).</summary>
    public float AddOutsideHeat(float heat)
    {
        if (FlushLeft > 0f) return 0f;
        Weapons.Heat += heat;
        return heat;
    }

    /// <summary>
    /// Stability: hits drain it by their impact (<see cref="Weapons.WeaponSpec.Impact"/> times the damage that strikes,
    /// armour or not: the push of a heavy round), it recovers <see cref="StabilityRecoverRate"/> of the budget a second
    /// once <see cref="StabilityRecoverDelay"/> s pass without a hit. Drained, the gyro steadies the guns less (up to
    /// <see cref="StabilityAimLoss"/> of it gone); emptied, the mech staggers for <see cref="StaggerTime"/> s: guns
    /// off line, legs coasting, the torso rocked back. A stagger refills half the budget and can't recur for
    /// <see cref="StaggerGrace"/> s after (the flamer's chained shutdowns taught that). Heavy shells and massed missiles
    /// stagger, lasers barely do: a second axis besides damage on which weapons differ (the genre's stagger and
    /// stability meters).
    /// </summary>
    public float Stability { get; private set; }

    public const float StaggerTime = 1.1f, StaggerGrace = 3f, StabilityRecoverDelay = 1.5f, StabilityRecoverRate = 0.25f, StabilityAimLoss = 0.6f;

    /// <summary>0 at full stability, 1 when emptied.</summary>
    public float Instability => 1f - Stability / Chassis.StabilityMax;

    public float StaggerLeft { get; private set; }
    public bool Staggered => StaggerLeft > 0f;

    /// <summary>Staggers so far (for reports).</summary>
    public int Staggers { get; private set; }

    private float _sinceImpact = 99f, _staggerGraceLeft;

    private void Drain(float impact, Vector3? travel)
    {
        if (impact <= 0f || IsOut) return;
        Stability = MathF.Max(0f, Stability - impact);
        _sinceImpact = 0f;
        if (Stability > 0f || _staggerGraceLeft > 0f || Shutdown) return;
        StaggerLeft = StaggerTime;
        Staggers++;
        Stability = 0.5f * Chassis.StabilityMax;
        _staggerGraceLeft = StaggerTime + StaggerGrace;
        // Rocked away from the blow: back when hit from the front, forward from behind.
        bool fromFront = travel is not { } d || Vector3.Dot(d, TorsoFacing) < 0f;
        var sec = Locomotion.Secondary;
        sec.TorsoPitch.V += fromFront ? -1.4f : 1.4f;
        sec.TorsoRoll.V += sec.RandomSigned() * 1.2f;
        sec.AddTrauma(0.7f);
    }

    /// <summary>Seconds left of sensor scrambling from a particle-bolt hit.</summary>
    public float DisruptLeft { get; private set; }

    public void Disrupt(float seconds) => DisruptLeft = MathF.Max(DisruptLeft, seconds);

    public void Step(in MechInput input, float h)
    {
        Shots.Clear();
        DisruptLeft = MathF.Max(0f, DisruptLeft - h);
        FlushLeft = MathF.Max(0f, FlushLeft - h);
        if (!Shutdown && !IsOut && Weapons.Heat >= ShutdownHeat) ShutdownLeft = ShutdownTime;
        _sinceImpact += h;
        _staggerGraceLeft = MathF.Max(0f, _staggerGraceLeft - h);
        StaggerLeft = MathF.Max(0f, StaggerLeft - h);
        if (_sinceImpact > StabilityRecoverDelay)
            Stability = MathF.Min(Chassis.StabilityMax, Stability + StabilityRecoverRate * Chassis.StabilityMax * h);
        float hot = Hot;
        Locomotion.HeatSpeedFactor = 1f - HotSpeedLoss * hot;
        Locomotion.AimSteadiness = (1f - HotAimLoss * hot) * (1f - StabilityAimLoss * Instability);
        var command = input;
        if (Staggered && !Shutdown)
            command = new MechInput { TwistTarget = Locomotion.Body.Twist, AimTarget = Locomotion.Body.Aim };
        if (Shutdown)
        {
            ShutdownLeft = MathF.Max(0f, ShutdownLeft - h);
            command = new MechInput { TwistTarget = Locomotion.Body.Twist, AimTarget = Locomotion.Body.Aim };
            if (ShutdownLeft <= 0f)
            {
                Weapons.Heat = MathF.Min(Weapons.Heat, RestartHeat);
                FlushLeft = CoolantFlushTime;
            }
        }
        Locomotion.Step(command, h);
        if (Locomotion.JetsBurning) Weapons.Heat += Chassis.JumpHeat * h;
        if (!IsOut) Weapons.Step(command, Locomotion, h, Shots);
    }

    /// <summary>Heat sink capacity each destroyed side torso takes with it.</summary>
    public const float LostSideTorsoSinks = 0.15f;

    /// <summary>Nearest hit volume or cover plate along a ray (unit direction); a destroyed arm's plates are gone.</summary>
    public bool Raycast(Vector3 origin, Vector3 dir, float maxDistance, out RayHit hit)
    {
        uint gone = 0u;
        foreach (var s in new[] { MechSection.ArmLeft, MechSection.ArmRight })
            if (Damage.IsDestroyed(s)) gone |= 1u << (int)s;
        return HitTest.Raycast(Pose.HitVolumes, Pose.HitBoxes, gone, origin, dir, maxDistance, out hit);
    }

    /// <summary>Torso forward on the ground plane: the way its front armour faces (follows the torso twist).</summary>
    public Vector3 TorsoFacing
    {
        get
        {
            var d = Pose.AimDirection;
            d.Y = 0f;
            return d.LengthSquared() > 1e-8f ? Vector3.Normalize(d) : -Vector3.UnitZ;
        }
    }

    /// <param name="travel">Direction the shot was travelling, if known: torso hits from behind strike the rear plate.</param>
    /// <param name="spec">The weapon, for its effectiveness against armour and structure (null = 1 and 1).</param>
    public DamageReport ApplyHit(MechSection struck, float amount, Vector3? travel = null, Weapons.WeaponSpec? spec = null)
    {
        var facing = travel is { } dir ? HitFacing.Classify(TorsoFacing, dir) : Facing.Front;
        var report = Damage.Apply(struck, amount, facing, spec?.VsArmor ?? 1f, spec?.VsStructure ?? 1f);
        if (report.Ignored) return report;
        Locomotion.Secondary.ApplyHit(report.Amount);
        Drain(amount * (spec?.Impact ?? 1f), travel);
        if (report.ToStructure > 0f || report.SectionDestroyed) ApplyDamageEffects();
        return report;
    }

    /// <summary>
    /// How well the head's sensors work, 0..1: its structure once the plate is gone (aim and detection degrade with
    /// it; the AI reads this).
    /// </summary>
    public float SensorQuality =>
        (Damage.Exposed(MechSection.Cockpit) ? 0.4f + 0.6f * Damage.StructureFraction(MechSection.Cockpit) : 1f) * (DisruptLeft > 0f ? 0.5f : 1f);

    /// <summary>
    /// Maps section state onto the simulation. Destroyed: a section's weapons go offline (a side torso takes its arm
    /// and the rack sitting in it), one leg caps speed at the crippled limit, the centre torso, the cockpit or both
    /// legs = out (collapse). Exposed structure degrades a section in proportion to what is left of it: a leg limps (top
    /// speed down toward the crippled cap, the worse leg counts), an arm's weapon cycles slower (up to twice as long),
    /// centre torso heat sinks lose capacity (dissipation down to 40 %) and each lost side torso takes another 15 %,
    /// the head's sensors blur (<see cref="SensorQuality"/>).
    /// </summary>
    private void ApplyDamageEffects()
    {
        foreach (var m in Weapons.Mounts)
        {
            m.Offline = Damage.IsDestroyed(m.Section);
            if (!m.OnTorso) m.CycleFactor = Damage.Exposed(m.Section) ? 2f - Damage.StructureFraction(m.Section) : 1f;
        }
        float legs = 1f;
        foreach (var leg in new[] { MechSection.LegLeft, MechSection.LegRight })
        {
            float f = Damage.IsDestroyed(leg) ? Chassis.CrippledSpeedFactor
                : Damage.Exposed(leg) ? Core.MathUtil.Lerp(Chassis.CrippledSpeedFactor, 1f, Damage.StructureFraction(leg))
                : 1f;
            legs = MathF.Min(legs, f);
        }
        Locomotion.SpeedFactor = legs;
        float sinks = Damage.Exposed(MechSection.Torso) ? 0.4f + 0.6f * Damage.StructureFraction(MechSection.Torso) : 1f;
        foreach (var side in new[] { MechSection.TorsoLeft, MechSection.TorsoRight })
            if (Damage.IsDestroyed(side)) sinks -= LostSideTorsoSinks;
        Weapons.DissipationFactor = MathF.Max(0.1f, sinks);
        if (IsOut) Locomotion.Collapse(Locomotion.Secondary.RandomSigned() * 0.6f);
    }
}
