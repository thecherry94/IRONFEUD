using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.Battle;

/// <summary>Per-unit tallies for the debug panel and, later, the BattleResult.</summary>
public struct UnitStats
{
    public int Shots, Hits;
    public float DamageDealt;

    /// <summary>Part of <see cref="DamageDealt"/> that struck a target from behind.</summary>
    public float RearDamage;

    /// <summary>Heat put into enemies (flamethrowers, incendiary rounds), and heat shutdowns this unit caused.</summary>
    public float HeatInflicted;
    public int ShutdownsCaused;
}

/// <summary>A mech in a battle: its simulation, its side and whoever is flying it (an AI, or the player via <see cref="Input"/>).</summary>
public sealed class BattleUnit
{
    public readonly int Id;
    public readonly string Name;
    public readonly int Team;
    public readonly MechSim Sim;

    /// <summary>Written by the pilot before each step: by <see cref="Ai"/> when set, otherwise from outside.</summary>
    public MechInput Input;
    public AiPilot? Ai;
    public UnitStats Stats;

    public BattleUnit(int id, string name, int team, MechSim sim)
    {
        Id = id;
        Name = name;
        Team = team;
        Sim = sim;
    }

    public bool IsOut => Sim.IsOut;
    public Vector3 Position => Sim.Locomotion.Body.Position;
    public MechChassis Chassis => Sim.Chassis;

    /// <summary>Footprint radius for mech-vs-mech and mech-vs-block collision.</summary>
    public float Radius => 3f * Chassis.Scale;

    public HitVolume Volume(MechSection s) => Sim.Pose.HitVolumes[(int)s];

    /// <summary>The enemy this unit's pilot wants a missile lock on; set by the pilot every tick (null = none).</summary>
    public BattleUnit? LockCandidate;

    /// <summary>Seconds the candidate has stayed spotted and in the aim cone; locked at the guided weapon's lock time.</summary>
    public float LockProgress { get; internal set; }

    /// <summary>The enemy guided missiles fired now will home on.</summary>
    public BattleUnit? Locked { get; internal set; }

    internal float PointDefenseBudget;

    /// <summary>Who last put heat into this unit, and when (credit for a shutdown).</summary>
    internal BattleUnit? HeatedBy;
    internal float HeatedAt = -99f;
    internal bool WasShutdown;

    /// <summary>Ground velocity (legs plus jets), for leading.</summary>
    public Vector3 Velocity
    {
        get
        {
            var body = Sim.Locomotion.Body;
            return MathUtil.Forward(body.Psi) * body.V + (body.Airborne ? Vector3.UnitY * body.Vy : Vector3.Zero);
        }
    }

    public override string ToString() => Name;
}

public sealed class Shell
{
    public int Id;
    public Projectile State;
    public BattleUnit Owner = null!;
}

/// <summary>
/// Lance tactics: Radar (lancemates share sightings), Spotters (lights take observation posts for guided-missile
/// teammates), Umbrella (pilots under missile threat close on a teammate's point defense).
/// </summary>
[Flags]
public enum TeamTactics { None = 0, Radar = 1, Spotters = 2, Umbrella = 4, All = Radar | Spotters | Umbrella }

/// <summary>
/// What a lance knows about an enemy's state: a sensor lock scans its internals (armour, structure, exposed
/// sections, heat). Shared across the team; stale after <see cref="BattleSim.IntelLifetime"/> without a lock.
/// </summary>
public sealed class ScanReport
{
    public float Time;

    /// <summary>Like <see cref="AiPilot.HealthFactor"/>: torso integrity weighted by working weapons.</summary>
    public float Health = 1f;
    public float Heat;
    public bool AnyExposed;
}

/// <summary>A missile in flight: boosts to cruise speed, homes on <see cref="Target"/> if guided (after a 0.5 s climb).</summary>
public sealed class Missile
{
    public int Id;
    public BattleUnit Owner = null!;
    public WeaponSpec Spec = null!;
    public BattleUnit? Target;
    public Vector3 Position, Velocity, Origin;
    public float Life;

    /// <summary>Share of the salvo's damage this round carries.</summary>
    public float Share;
}

/// <summary>
/// ShellFired / MissileFired: a round left a muzzle. Beam: a beam is lit (see <see cref="BattleEvent"/>). Impact: a
/// round hit something. PointDefense: a turret on <c>Shooter</c> shot down a missile at <c>Point</c>.
/// </summary>
public enum BattleEventKind { ShellFired, Beam, Impact, MissileFired, PointDefense }

/// <summary>
/// Something the renderer should show this tick. <see cref="Impact"/> is a shell or single beam hitting something;
/// <see cref="Target"/> and <see cref="Report"/> are set when it hit a mech. A burning beam sends a
/// <see cref="BattleEventKind.Beam"/> every tick instead (phase Start, then Hold) with <see cref="Hit"/> and
/// <see cref="Target"/> set, and no Impact: the renderer keeps it lit while the events keep coming.
/// </summary>
public struct BattleEvent
{
    public BattleEventKind Kind;
    public BattleUnit Shooter;
    public ShotFired Shot;
    public Vector3 From, Point;
    public bool Hit;
    public BattleUnit? Target;
    public DamageReport Report;
}

/// <summary>What a ray struck: the map (<see cref="Unit"/> null) or a mech section.</summary>
public struct BattleRayHit
{
    public Vector3 Point;
    public float Distance;
    public BattleUnit? Unit;
    public MechSection Section;
}

/// <summary>
/// A whole tactical battle at the fixed tick, no engine needed: AI pilots, mechs, shells, beams, damage and
/// collision against an <see cref="ArenaMap"/>. Godot renders it; tests and auto-resolve calibration run it headless.
/// Order per tick: collision → AI → mechs → shots fired this tick → shells in flight → outcome.
/// </summary>
public sealed class BattleSim
{
    public readonly ArenaMap Map;
    public readonly List<BattleUnit> Units = new();
    public readonly List<Shell> Shells = new();
    public readonly List<Missile> Missiles = new();

    /// <summary>Events of the last <see cref="Step"/>; cleared at the start of each step.</summary>
    public readonly List<BattleEvent> Events = new();

    /// <summary>Each team's focus target, picked once per second (squad coordinator).</summary>
    public readonly Dictionary<int, BattleUnit?> TeamFocus = new();

    public float Time { get; private set; }

    /// <summary>Which lance tactics each team's AI pilots use (all by default). Switched off only to measure what each changes.</summary>
    public readonly TeamTactics[] Tactics = { TeamTactics.All, TeamTactics.All };

    /// <summary>Sets both teams' lance tactics.</summary>
    public TeamTactics TeamTactics
    {
        set => Tactics[0] = Tactics[1] = value;
    }

    public TeamTactics TacticsOf(int team) => Tactics[Math.Clamp(team, 0, Tactics.Length - 1)];

    /// <summary>Per team: scans of enemies (see <see cref="ScanReport"/>).</summary>
    public readonly Dictionary<BattleUnit, ScanReport>[] Intel = { new(), new() };

    /// <summary>A sensor lock (any mech, on its pilot's candidate) takes this long; guided weapons use their own lock time.</summary>
    public const float SensorLockTime = 1.5f, SensorLockRange = 600f, IntelLifetime = 10f;

    /// <summary>The team's current scan of <paramref name="e"/>, or null if nobody has locked it in the last 10 s.</summary>
    public ScanReport? Scan(int team, BattleUnit e) =>
        Intel[Math.Clamp(team, 0, Intel.Length - 1)].TryGetValue(e, out var r) && Time - r.Time < IntelLifetime ? r : null;

    /// <summary>Which jump-jet tactics AI pilots use (all by default). Switched off only to measure what each changes.</summary>
    public JumpTactics JumpTactics = JumpTactics.All;

    /// <summary>Mediums escort a heavy or assault (<see cref="AiPilot.Ward"/>). Off only to measure what escorts change.</summary>
    public bool Escorts = true;

    /// <summary>Decide a winner once at most one team is left standing (off for the firing range).</summary>
    public bool TrackOutcome = true;
    public bool Over { get; private set; }

    /// <summary>Winning team, or -1 for a draw. Valid when <see cref="Over"/>.</summary>
    public int Winner { get; private set; } = -1;

    public event Action<BattleUnit, DamageReport, Vector3, BattleUnit?>? Damaged;

    private readonly uint _seed;
    private int _nextShellId;
    private readonly HashSet<int> _burnScored = new();

    public BattleSim(ArenaMap map, uint seed = 1)
    {
        Map = map;
        _seed = seed;
        _rng = new DeterministicRng(seed * 977u + 13u);
    }

    private readonly DeterministicRng _rng;

    public BattleUnit AddUnit(string name, int team, MechChassis chassis, Vector3 position, float heading,
        Personality? ai = null, MechDamage? damage = null)
    {
        int id = Units.Count;
        uint seed = _seed * 7919u + (uint)id * 104729u + 1u;
        var sim = new MechSim(chassis, seed, damage);
        sim.Locomotion.Ground = Map;
        sim.Locomotion.Reset(position, heading);
        var unit = new BattleUnit(id, name, team, sim);
        if (ai != null) unit.Ai = new AiPilot(ai, seed ^ 0x5bd1e995u);
        Units.Add(unit);
        return unit;
    }

    public void Step(float h)
    {
        Events.Clear();
        Collide();

        if (MathF.Floor(Time) != MathF.Floor(Time + h) || TeamFocus.Count == 0) Coordinate();
        foreach (var u in Units)
            if (!u.IsOut) u.Ai?.Tick(this, u, h);
        UpdateLocks(h);

        foreach (var u in Units)
        {
            u.Sim.Step(u.Input, h);
            if (u.Sim.Shutdown && !u.WasShutdown && u.HeatedBy is { } heater && Time - u.HeatedAt < 5f) heater.Stats.ShutdownsCaused++;
            u.WasShutdown = u.Sim.Shutdown;
        }
        foreach (var u in Units)
            foreach (var s in u.Sim.Shots) Fire(u, s);

        StepShells(h);
        StepMissiles(h);
        PointDefend(h);
        Time += h;
        if (TrackOutcome) CheckOutcome();
    }

    /// <summary>Nearest hit along a unit ray against the map (ground included) and every mech except <paramref name="ignore"/>.</summary>
    public bool Raycast(Vector3 origin, Vector3 dir, float maxDistance, BattleUnit? ignore, out BattleRayHit hit)
    {
        float d = Map.RayDistance(origin, dir, maxDistance);
        bool found = d < maxDistance;
        hit = new BattleRayHit { Point = origin + dir * d, Distance = d };
        foreach (var u in Units)
        {
            if (u == ignore) continue;
            if (u.Sim.Raycast(origin, dir, hit.Distance, out var mh))
            {
                hit = new BattleRayHit { Point = mh.Point, Distance = mh.Distance, Unit = u, Section = mh.Section };
                found = true;
            }
        }
        return found;
    }

    /// <param name="travel">The shot's direction of travel: torso hits from behind strike the thin rear plate.</param>
    /// <param name="countHit">False for the later ticks of a burn that already scored: a burn is one hit.</param>
    /// <param name="spec">The weapon (its effectiveness against armour and structure).</param>
    public DamageReport ApplyHit(BattleUnit target, MechSection section, float amount, Vector3 point, BattleUnit? attacker,
        Vector3? travel = null, bool countHit = true, WeaponSpec? spec = null)
    {
        var report = target.Sim.ApplyHit(section, amount, travel, spec);
        if (report.Ignored) return report;
        // Heat weapons heat the target in proportion to the hit; a particle bolt scrambles its sensors.
        if (spec is { HeatToTarget: > 0f })
        {
            float heat = spec.HeatToTarget * amount / spec.Damage;
            target.Sim.Weapons.Heat += heat;
            if (attacker != null)
            {
                attacker.Stats.HeatInflicted += heat;
                target.HeatedBy = attacker;
                target.HeatedAt = Time;
            }
        }
        if (spec is { DisruptTime: > 0f }) target.Sim.Disrupt(spec.DisruptTime);
        if (attacker != null)
        {
            if (countHit) attacker.Stats.Hits++;
            attacker.Stats.DamageDealt += report.Amount;
            if (report.Facing == Facing.Rear) attacker.Stats.RearDamage += report.Amount;
        }
        target.Ai?.NotifyHit(attacker, report.Amount, report.Facing == Facing.Rear);
        Damaged?.Invoke(target, report, point, attacker);
        return report;
    }

    private void Fire(BattleUnit u, in ShotFired shot)
    {
        if (shot.Phase != ShotPhase.Hold) u.Stats.Shots++;
        switch (shot.Spec.Kind)
        {
            case WeaponKind.Projectile:
                Shells.Add(new Shell { Id = _nextShellId++, State = Projectile.From(shot), Owner = u });
                Events.Add(new BattleEvent { Kind = BattleEventKind.ShellFired, Shooter = u, Shot = shot, From = shot.Origin, Point = shot.Origin });
                break;

            case WeaponKind.Missile:
            {
                var spec = shot.Spec;
                Vector3 dir = shot.Direction;
                if (spec.Guided)
                {
                    // Climb toward the target's bearing, then home (over cover).
                    var flat = new Vector3(dir.X, 0f, dir.Z);
                    flat = flat.LengthSquared() > 1e-6f ? Vector3.Normalize(flat) : MathUtil.Forward(u.Sim.Locomotion.Body.Psi);
                    dir = Vector3.Normalize(flat * MathF.Cos(spec.Loft) + Vector3.UnitY * MathF.Sin(spec.Loft));
                }
                dir = Jitter(dir, spec.Spread);
                Missiles.Add(new Missile
                {
                    Id = _nextShellId++, Owner = u, Spec = spec, Target = spec.Guided ? u.Locked : null,
                    Position = shot.Origin, Origin = shot.Origin, Velocity = dir * (0.5f * spec.MissileSpeed), Share = shot.DamageFraction,
                });
                Events.Add(new BattleEvent { Kind = BattleEventKind.MissileFired, Shooter = u, Shot = shot, From = shot.Origin, Point = shot.Origin });
                break;
            }

            case WeaponKind.Beam:
            {
                bool hit = Raycast(shot.Origin, shot.Direction, shot.Spec.Range, u, out var bh);
                Events.Add(new BattleEvent
                {
                    Kind = BattleEventKind.Beam, Shooter = u, Shot = shot, From = shot.Origin, Point = bh.Point, Hit = hit, Target = bh.Unit,
                });
                if (!hit) break;
                float damage = shot.Spec.DamageAt(bh.Distance) * shot.DamageFraction;
                if (shot.Phase == ShotPhase.Single) Impact(u, shot, bh, damage);
                else Burn(u, shot, bh, damage);
                break;
            }
        }
    }

    private void StepShells(float h)
    {
        for (int i = Shells.Count - 1; i >= 0; i--)
        {
            var s = Shells[i];
            Vector3 start = s.State.Step(h), end = s.State.Position;
            float len = Vector3.Distance(start, end);
            bool hit = Raycast(start, (end - start) / len, len, s.Owner, out var bh);
            if (!hit && !s.State.Expired) continue;
            Shells.RemoveAt(i);
            if (hit)
                Impact(s.Owner, new ShotFired { Spec = s.State.Spec, Origin = start, Direction = (end - start) / len }, bh,
                    s.State.Spec.DamageAt(Vector3.Distance(s.State.Origin, bh.Point)));
        }
    }

    private void Impact(BattleUnit shooter, in ShotFired shot, in BattleRayHit hit, float damage)
    {
        var e = new BattleEvent { Kind = BattleEventKind.Impact, Shooter = shooter, Shot = shot, From = shot.Origin, Point = hit.Point, Target = hit.Unit };
        if (hit.Unit != null) e.Report = ApplyHit(hit.Unit, hit.Section, damage, hit.Point, shooter, shot.Direction, spec: shot.Spec);
        Events.Add(e);
    }

    /// <summary>A direction scattered uniformly within a cone of half-angle <paramref name="spread"/>.</summary>
    private Vector3 Jitter(Vector3 dir, float spread)
    {
        if (spread <= 0f) return dir;
        Vector3 side = Vector3.Normalize(Vector3.Cross(dir, MathF.Abs(dir.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
        Vector3 up = Vector3.Cross(side, dir);
        float a = _rng.NextFloat() * MathF.Tau, r = spread * MathF.Sqrt(_rng.NextFloat());
        return Vector3.Normalize(dir + side * (MathF.Cos(a) * r) + up * (MathF.Sin(a) * r));
    }

    /// <summary>
    /// Missiles: boost to cruise speed over 0.5 s; guided ones home on their target (leading it) after a 0.5 s climb
    /// at the spec's turn rate. A hit inside the minimum range is a dud; otherwise damage falls off with the
    /// straight-line distance from the launcher like any weapon.
    /// </summary>
    private void StepMissiles(float h)
    {
        for (int i = Missiles.Count - 1; i >= 0; i--)
        {
            var m = Missiles[i];
            m.Life += h;
            float speed = MathF.Min(m.Spec.MissileSpeed, m.Velocity.Length() + m.Spec.MissileSpeed * h / 0.5f);
            Vector3 dir = Vector3.Normalize(m.Velocity);
            if (m.Target is { IsOut: false } t && m.Life > 0.5f)
            {
                // Top attack: stay high over the target's area (aim above it by half the remaining ground distance,
                // 20-120 m) and dive steeply only at the end. A direct line came in at ~13° and buried itself in the
                // crest in front of hull-down targets. The dive (last 90 m) turns at twice the cruise rate.
                Vector3 aim = t.Volume(MechSection.Torso).Center;
                aim += t.Velocity * (Vector3.Distance(aim, m.Position) / speed);
                float ground = MathUtil.PlanarDistance(aim, m.Position);
                bool diving = ground < 90f;
                if (!diving) aim.Y += MathUtil.Clamp(0.5f * ground, 20f, 120f);
                dir = TurnToward(dir, Vector3.Normalize(aim - m.Position), m.Spec.TurnRate * (diving ? 2f : 1f) * h);
            }
            m.Velocity = dir * speed;
            Vector3 start = m.Position;
            m.Position += m.Velocity * h;
            float len = Vector3.Distance(start, m.Position);
            bool hit = Raycast(start, dir, len, m.Owner, out var bh);
            if (hit)
            {
                float flown = Vector3.Distance(m.Origin, bh.Point);
                float damage = flown < m.Spec.MinRange ? 0f : m.Spec.DamageAt(flown) * m.Share;
                var shot = new ShotFired { Spec = m.Spec, Origin = start, Direction = dir, Share = m.Share };
                var e = new BattleEvent { Kind = BattleEventKind.Impact, Shooter = m.Owner, Shot = shot, From = start, Point = bh.Point, Target = bh.Unit };
                if (bh.Unit != null && damage > 0f) e.Report = ApplyHit(bh.Unit, bh.Section, damage, bh.Point, m.Owner, dir, spec: m.Spec);
                Events.Add(e);
            }
            if (hit || m.Life > m.Spec.MaxLifetime) Missiles.RemoveAt(i);
        }
    }

    /// <summary>Is the missile closing on this mech or on a teammate within point-defense cover of it?</summary>
    private bool ClosingOnTeam(Missile m, BattleUnit defender, Vector3 at)
    {
        if (Vector3.Dot(m.Velocity, at - m.Position) > 0f) return true;
        foreach (var a in Units)
        {
            if (a == defender || a.Team != defender.Team || a.IsOut) continue;
            Vector3 c = a.Volume(MechSection.Torso).Center;
            if (Vector3.Distance(c, at) < PointDefense.Cover && Vector3.Dot(m.Velocity, c - m.Position) > 0f) return true;
        }
        return false;
    }

    private static Vector3 TurnToward(Vector3 dir, Vector3 want, float maxAngle)
    {
        float cos = Math.Clamp(Vector3.Dot(dir, want), -1f, 1f), angle = MathF.Acos(cos);
        if (angle <= maxAngle) return want;
        Vector3 axis = Vector3.Cross(dir, want);
        if (axis.LengthSquared() < 1e-10f) axis = Vector3.UnitY;
        return Vector3.Normalize(Vector3.Transform(dir, Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), maxAngle)));
    }

    /// <summary>
    /// Point-defense turrets: each mech's turrets shoot down enemy missiles within range that are closing on it or on
    /// a teammate within cover distance (nearest first), at a steady rate per turret (budget capped at 1.5 shots per
    /// turret so a quiet turret can't erase the front of a salvo at once).
    /// </summary>
    private void PointDefend(float h)
    {
        if (Missiles.Count == 0) return;
        foreach (var u in Units)
        {
            int turrets = u.Chassis.PointDefense;
            if (u.IsOut || turrets <= 0) continue;
            u.PointDefenseBudget = MathF.Min(1.5f * turrets, u.PointDefenseBudget + PointDefense.Rate * turrets * h);
            Vector3 c = u.Volume(MechSection.Torso).Center;
            while (u.PointDefenseBudget >= 1f)
            {
                int best = -1;
                float bestD = PointDefense.Range;
                for (int i = 0; i < Missiles.Count; i++)
                {
                    var m = Missiles[i];
                    if (m.Owner.Team == u.Team) continue;
                    float d = Vector3.Distance(m.Position, c);
                    if (d < bestD && ClosingOnTeam(m, u, c)) { bestD = d; best = i; }
                }
                if (best < 0) break;
                Events.Add(new BattleEvent { Kind = BattleEventKind.PointDefense, Shooter = u, From = c, Point = Missiles[best].Position });
                Missiles.RemoveAt(best);
                u.PointDefenseBudget -= 1f;
                u.Sim.Weapons.Heat += PointDefense.HeatPerKill;
            }
        }
    }

    /// <summary>
    /// Sensor locks: every mech locks its pilot's candidate once it has stayed spotted (any teammate can see it, the
    /// locker included) within 20° of the torso aim and in range for the lock time (a guided weapon's own, else 1.5 s);
    /// losing either bleeds the lock off twice as fast. A lock scans the target's internals into the team's intel
    /// every tick it holds, and is what guided missiles home on.
    /// </summary>
    private void UpdateLocks(float h)
    {
        foreach (var u in Units)
        {
            WeaponSpec? guided = null;
            if (!u.IsOut)
                foreach (var m in u.Sim.Weapons.Mounts)
                    if (m.Spec.Guided && !m.Offline) guided = m.Spec;
            var c = u.LockCandidate;
            if (u.IsOut || c == null || c.IsOut || c.Team == u.Team)
            {
                u.LockProgress = 0f;
                u.Locked = null;
                continue;
            }
            if (u.Locked != null && u.Locked != c) { u.Locked = null; u.LockProgress = 0f; }
            var pose = u.Sim.Pose;
            Vector3 to = c.Volume(MechSection.Torso).Center - pose.AimOrigin;
            float d = to.Length(), lockTime = guided?.LockTime ?? SensorLockTime, range = guided?.Range ?? SensorLockRange;
            bool inCone = d < range && Vector3.Dot(pose.AimDirection, to / MathF.Max(1e-3f, d)) > MathF.Cos(0.35f);
            u.LockProgress = inCone && Spotted(u.Team, c)
                ? MathF.Min(lockTime, u.LockProgress + h)
                : MathF.Max(0f, u.LockProgress - 2f * h);
            u.Locked = u.LockProgress >= lockTime ? c : null;
            if (u.Locked == null) continue;
            var intel = Intel[Math.Clamp(u.Team, 0, Intel.Length - 1)];
            if (!intel.TryGetValue(c, out var scan)) intel[c] = scan = new ScanReport();
            scan.Time = Time;
            scan.Health = AiPilot.HealthFactor(c);
            scan.Heat = c.Sim.Weapons.Heat;
            scan.AnyExposed = false;
            foreach (var s in Enum.GetValues<MechSection>())
                if (c.Sim.Damage.Exposed(s) && !c.Sim.Damage.IsDestroyed(s)) scan.AnyExposed = true;
        }
    }

    /// <summary>Can any mech of <paramref name="team"/> see <paramref name="e"/> (cockpit to torso, within 600 m)?</summary>
    public bool Spotted(int team, BattleUnit e)
    {
        Vector3 target = e.Volume(MechSection.Torso).Center;
        foreach (var u in Units)
        {
            if (u.Team != team || u.IsOut) continue;
            Vector3 eye = u.Volume(MechSection.Cockpit).Center;
            if (Vector3.Distance(eye, target) < 600f && Map.LineClear(eye, target)) return true;
        }
        return false;
    }

    /// <summary>One tick of a burning beam on whatever it touches now. Counts as a hit once per burn.</summary>
    private void Burn(BattleUnit shooter, in ShotFired shot, in BattleRayHit hit, float damage)
    {
        int key = shooter.Id * 8 + shot.Weapon;
        if (shot.Phase == ShotPhase.Start) _burnScored.Remove(key);
        if (hit.Unit == null) return;
        ApplyHit(hit.Unit, hit.Section, damage, hit.Point, shooter, shot.Direction, countHit: _burnScored.Add(key), spec: shot.Spec);
    }

    /// <summary>Mechs push each other apart (wrecks don't move) and are pushed out of blocks.</summary>
    private void Collide()
    {
        for (int i = 0; i < Units.Count; i++)
        for (int j = i + 1; j < Units.Count; j++)
        {
            BattleUnit a = Units[i], b = Units[j];
            Vector3 d = b.Position - a.Position;
            d.Y = 0f;
            float dist = d.Length(), min = a.Radius + b.Radius;
            if (dist >= min || dist < 1e-3f || a.IsOut && b.IsOut || PassesOver(a, b) || PassesOver(b, a)) continue;
            Vector3 n = d / dist;
            float push = min - dist;
            float wa = a.IsOut ? 0f : b.IsOut ? 1f : 0.5f;
            a.Sim.Locomotion.Body.Position -= n * (push * wa);
            b.Sim.Locomotion.Body.Position += n * (push * (1f - wa));
        }
        foreach (var u in Units)
            if (!u.IsOut) u.Sim.Locomotion.Body.Position = Map.PushOut(u.Position, u.Radius * 0.8f, u.Position.Y);
    }

    /// <summary>A mech on jets whose feet are above the other's torso centre passes over it (a vault), no shove.</summary>
    private static bool PassesOver(BattleUnit air, BattleUnit other) =>
        air.Sim.Locomotion.Body.Airborne && air.Position.Y > other.Volume(MechSection.Torso).Center.Y;

    /// <summary>Squad coordinator: each team focuses the enemy that is most damaged and most seen by its pilots.</summary>
    private void Coordinate()
    {
        foreach (var u in Units) TeamFocus.TryAdd(u.Team, null);
        foreach (int team in new List<int>(TeamFocus.Keys))
        {
            BattleUnit? best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var e in Units)
            {
                if (e.Team == team || e.IsOut) continue;
                // What the team knows (a scan), not the truth: unscanned enemies count as healthy.
                float s = (1f - (Scan(team, e)?.Health ?? 1f)) * 1.5f;
                foreach (var a in Units)
                    if (a.Team == team && !a.IsOut && a.Ai?.Contacts.GetValueOrDefault(e) is { } k)
                        s += k.Visible ? 1f : 0.3f;
                if (s > bestScore) { bestScore = s; best = e; }
            }
            TeamFocus[team] = best;
        }
        if (Escorts) AssignEscorts();
        AssignSpotters();
        AnswerHelpCalls();
    }

    /// <summary>Each mech with a guided weapon gets the nearest free ranged light of its team as its spotter (one each).</summary>
    private void AssignSpotters()
    {
        foreach (var u in Units)
            if (u.Ai?.SpotFor is { } s && (s.IsOut || u.IsOut)) u.Ai.SpotFor = null;
        foreach (var heavy in Units)
        {
            if (heavy.IsOut || !heavy.Sim.Weapons.Mounts.Exists(m => m.Spec.Guided && !m.Offline)) continue;
            if (!TacticsOf(heavy.Team).HasFlag(TeamTactics.Spotters))
            {
                foreach (var u in Units) if (u.Ai?.SpotFor == heavy) u.Ai.SpotFor = null;
                continue;
            }
            if (Units.Exists(u => u.Ai?.SpotFor == heavy)) continue;
            BattleUnit? best = null;
            float bestD = float.MaxValue;
            foreach (var u in Units)
            {
                if (u.Team != heavy.Team || u.IsOut || u.Ai == null || u.Ai.SpotFor != null || u.Chassis.Class != ChassisClass.Light) continue;
                // A flamethrower light is a brawler, not an observer (spotting kept it 300+ m out, it never lit anything).
                if (u.Sim.Weapons.Mounts.Exists(m => m.Spec.Flame && !m.Offline)) continue;
                float d = PlanarDistance(u.Position, heavy.Position);
                if (d < bestD) { bestD = d; best = u; }
            }
            if (best != null) best.Ai!.SpotFor = heavy;
        }
    }

    /// <summary>Has an enemy a missile lock on <paramref name="u"/> (the lock warning a pilot gets)?</summary>
    public bool LockedOn(BattleUnit u)
    {
        foreach (var e in Units) if (e.Team != u.Team && !e.IsOut && e.Locked == u) return true;
        return false;
    }

    /// <summary>Guided missiles in flight homing on <paramref name="u"/>.</summary>
    public int IncomingGuided(BattleUnit u)
    {
        int n = 0;
        foreach (var m in Missiles) if (m.Target == u) n++;
        return n;
    }

    /// <summary>
    /// A pilot in great danger calls for support (<see cref="AiPilot.HelpWanted"/>): the free teammate that can get
    /// there soonest (distance over top speed, at most 20 s away; assaults are too slow to answer) hunts the pest from
    /// beside the caller.
    /// </summary>
    private void AnswerHelpCalls()
    {
        foreach (var caller in Units)
        {
            if (caller.IsOut || caller.Ai?.HelpWanted is not { IsOut: false } pest) continue;
            if (Units.Any(u => u.Ai?.Answering == caller && !u.IsOut)) continue;
            BattleUnit? best = null;
            float soonest = 20f;
            foreach (var u in Units)
            {
                if (u == caller || u.Team != caller.Team || u.IsOut || u.Ai == null || u.Ai.Answering != null || u.Ai.HelpWanted != null) continue;
                if (u.Chassis.Class == ChassisClass.Assault) continue;
                float eta = PlanarDistance(u.Position, caller.Position) / u.Chassis.VMax;
                if (eta < soonest) { soonest = eta; best = u; }
            }
            best?.Ai!.Answer(caller, pest, Time);
        }
    }

    /// <summary>
    /// Each medium escorts one heavy or assault of its team: assaults first, one escort per ward, nearest first.
    /// A ward is kept while it stands; a medium with no ward left fights on its own.
    /// </summary>
    private void AssignEscorts()
    {
        var claimed = new HashSet<BattleUnit>();
        foreach (var u in Units)
            if (u.Ai?.Ward is { IsOut: false } w) claimed.Add(w);
        foreach (var u in Units)
        {
            if (u.IsOut || u.Ai == null || u.Chassis.Class != ChassisClass.Medium) continue;
            if (u.Ai.Ward is { IsOut: false }) continue;
            BattleUnit? best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var w in Units)
            {
                if (w.Team != u.Team || w.IsOut || claimed.Contains(w) || w.Chassis.Class is not (ChassisClass.Heavy or ChassisClass.Assault)) continue;
                float s = (w.Chassis.Class == ChassisClass.Assault ? 100f : 0f) - PlanarDistance(u.Position, w.Position);
                if (s > bestScore) { bestScore = s; best = w; }
            }
            u.Ai.Ward = best;
            if (best != null) claimed.Add(best);
        }
    }

    private void CheckOutcome()
    {
        if (Over) return;
        var teams = new HashSet<int>();
        var alive = new HashSet<int>();
        foreach (var u in Units)
        {
            teams.Add(u.Team);
            if (!u.IsOut) alive.Add(u.Team);
        }
        if (teams.Count < 2 || alive.Count > 1) return;
        Over = true;
        Winner = alive.Count == 1 ? new List<int>(alive)[0] : -1;
    }

    internal static Vector3 Planar(Vector3 v) => new(v.X, 0f, v.Z);
    internal static float PlanarDistance(Vector3 a, Vector3 b) => MathUtil.PlanarDistance(a, b);
}
