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

    public override string ToString() => Name;
}

public sealed class Shell
{
    public int Id;
    public Projectile State;
    public BattleUnit Owner = null!;
}

public enum BattleEventKind { ShellFired, Beam, Impact }

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

    /// <summary>Events of the last <see cref="Step"/>; cleared at the start of each step.</summary>
    public readonly List<BattleEvent> Events = new();

    /// <summary>Each team's focus target, picked once per second (squad coordinator).</summary>
    public readonly Dictionary<int, BattleUnit?> TeamFocus = new();

    public float Time { get; private set; }

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
    }

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

        foreach (var u in Units) u.Sim.Step(u.Input, h);
        foreach (var u in Units)
            foreach (var s in u.Sim.Shots) Fire(u, s);

        StepShells(h);
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

    /// <param name="travel">The shot's direction of travel: hits from behind do more damage to the torso.</param>
    /// <param name="countHit">False for the later ticks of a burn that already scored: a burn is one hit.</param>
    public DamageReport ApplyHit(BattleUnit target, MechSection section, float amount, Vector3 point, BattleUnit? attacker,
        Vector3? travel = null, bool countHit = true)
    {
        var report = target.Sim.ApplyHit(section, amount, travel);
        if (report.Ignored) return report;
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
                Impact(s.Owner, new ShotFired { Spec = s.State.Spec, Origin = start, Direction = (end - start) / len }, bh, s.State.Spec.Damage);
        }
    }

    private void Impact(BattleUnit shooter, in ShotFired shot, in BattleRayHit hit, float damage)
    {
        var e = new BattleEvent { Kind = BattleEventKind.Impact, Shooter = shooter, Shot = shot, From = shot.Origin, Point = hit.Point, Target = hit.Unit };
        if (hit.Unit != null) e.Report = ApplyHit(hit.Unit, hit.Section, damage, hit.Point, shooter, shot.Direction);
        Events.Add(e);
    }

    /// <summary>One tick of a burning beam on whatever it touches now. Counts as a hit once per burn.</summary>
    private void Burn(BattleUnit shooter, in ShotFired shot, in BattleRayHit hit, float damage)
    {
        int key = shooter.Id * 8 + shot.Weapon;
        if (shot.Phase == ShotPhase.Start) _burnScored.Remove(key);
        if (hit.Unit == null) return;
        ApplyHit(hit.Unit, hit.Section, damage, hit.Point, shooter, shot.Direction, countHit: _burnScored.Add(key));
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
                float s = (1f - e.Sim.Damage.Fraction(MechSection.Torso)) * 1.5f;
                foreach (var a in Units)
                    if (a.Team == team && !a.IsOut && a.Ai?.Contacts.GetValueOrDefault(e) is { } k)
                        s += k.Visible ? 1f : 0.3f;
                if (s > bestScore) { bestScore = s; best = e; }
            }
            TeamFocus[team] = best;
        }
        if (Escorts) AssignEscorts();
        AnswerHelpCalls();
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
