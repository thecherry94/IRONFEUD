using System;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Logic.AI;

public enum AiAction { Engage, Cool, Flank, Retreat, Search }

/// <summary>What a pilot remembers about an enemy. Position and velocity are ground-plane estimates.</summary>
public sealed class Contact
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float LastSeen;

    /// <summary>Start of the current stretch of visibility (reaction time counts from here).</summary>
    public float FirstSeen;
    public bool Visible;

    /// <summary>Torso centre and cockpit height above the ground, as last observed.</summary>
    public float AimHeight = 7f, EyeHeight = 8.3f;
}

/// <summary>
/// The arena prototype's utility AI (docs/combat-ai.md). It writes only <see cref="MechInput"/>, perceives through
/// line of sight, radar range and being shot, and aims with Gaussian error. Thinks every 0.35 s (staggered), steers
/// and aims every tick. All state the debug overlay needs is public.
/// </summary>
public sealed class AiPilot
{
    public const float ThinkInterval = 0.35f;
    // Distances are the prototype's ×2 (its mechs walked at 4 m/s; ours run at 11-36 m/s).
    public const float SensorRange = 600f, RadarRange = 260f, ContactMemory = 15f;

    /// <summary>Target speed that counts as "moving" for aim error (about a medium's top speed).</summary>
    public const float ReferenceSpeed = 20f;

    public const int LogLength = 9;
    public static readonly int ActionCount = Enum.GetValues<AiAction>().Length;
    private static readonly float[] SteerOffsets = { 0f, 0.3f, -0.3f, 0.6f, -0.6f, 0.9f, -0.9f, 1.3f, -1.3f, 1.8f, -1.8f, 2.4f, -2.4f };

    public Personality Personality;
    public AiAction Action { get; private set; } = AiAction.Search;

    /// <summary>Human-readable reason for the last action change.</summary>
    public string Why { get; private set; } = "";

    /// <summary>Utility of each action at the last think, commitment bonus included (index = (int)AiAction).</summary>
    public readonly float[] Scores = new float[ActionCount];
    public readonly Dictionary<BattleUnit, Contact> Contacts = new();
    public BattleUnit? Target { get; private set; }
    public Vector3 Destination { get; private set; }

    /// <summary>Leg heading chosen by the steering probes.</summary>
    public float DesiredHeading { get; private set; }

    /// <summary>Where the torso is pointed: lead point plus shell drop plus pilot error.</summary>
    public Vector3 AimPoint { get; private set; }

    /// <summary>Index into <see cref="ArenaMap.Cover"/>, or -1.</summary>
    public int CoverIndex { get; private set; } = -1;

    /// <summary>Current aim error standard deviation, radians.</summary>
    public float Sigma { get; private set; }
    public bool TwistingAway { get; private set; }
    public bool HoldingFire { get; private set; }

    /// <summary>Decision log, newest first.</summary>
    public IReadOnlyList<string> Log => _log;

    private readonly DeterministicRng _rng;
    private readonly List<string> _log = new();
    private float _now, _thinkTimer, _lastThink, _noLos, _orbitUntil, _errYaw, _errPitch, _dmgRecent, _coverUntil, _lastHurt;
    private int _orbit;
    private BattleUnit? _lastAttacker;
    private Vector3? _roam;
    private Vector3 _home;
    private bool _started;

    public AiPilot(Personality personality, uint seed)
    {
        Personality = personality;
        _rng = new DeterministicRng(seed);
        _thinkTimer = _rng.NextFloat() * ThinkInterval;
        _orbit = _rng.NextFloat() < 0.5f ? 1 : -1;
    }

    public static string Name(AiAction a) => a.ToString().ToUpperInvariant();

    /// <summary>Being shot: remember who did it (their position is revealed at the next think) and how much it hurt.</summary>
    public void NotifyHit(BattleUnit? attacker, float damage)
    {
        _dmgRecent += damage;
        _lastHurt = _now;
        if (attacker != null) _lastAttacker = attacker;
    }

    public void Tick(BattleSim battle, BattleUnit me, float h)
    {
        _now = battle.Time;
        if (!_started)
        {
            _started = true;
            _home = me.Position;
            Destination = me.Position;
            DesiredHeading = me.Sim.Locomotion.Body.Psi;
            _lastThink = _now;
        }
        _thinkTimer -= h;
        if (_thinkTimer <= 0f)
        {
            _thinkTimer = ThinkInterval;
            Think(battle, me);
        }
        me.Input = Control(battle, me);
    }

    // ---------- think (every 0.35 s) ----------

    private void Think(BattleSim b, BattleUnit me)
    {
        var p = Personality;
        float dtThink = _now - _lastThink;
        _lastThink = _now;

        Perceive(b, me);
        SelectTarget(b, me);

        Contact? k = Target != null ? Contacts[Target] : null;
        bool vis = k?.Visible == true;
        _noLos = vis ? 0f : _noLos + dtThink;
        float hp = HealthFactor(me);
        float heat = me.Sim.Weapons.Heat;
        float dist = k != null ? MathUtil.PlanarDistance(me.Position, k.Position) : 999f;
        float age = k != null ? _now - k.LastSeen : 99f;

        Scores[(int)AiAction.Engage] = k != null ? (vis ? 0.55f : 0.25f) * p.Aggression + 0.25f * hp - 0.6f * MathF.Max(0f, heat - 0.55f) : 0f;
        Scores[(int)AiAction.Cool] = heat > 0.5f ? (heat - 0.5f) * 2.4f + (_dmgRecent > 12f ? 0.15f : 0f) : 0f;
        Scores[(int)AiAction.Flank] = k != null && !vis && age < 4f
            ? MathF.Max(0.1f, 0.3f + 0.07f * MathF.Min(_noLos, 6f) - 0.06f * MathF.Max(0f, _noLos - 9f)) : 0f;
        // Not in the prototype: the urge to retreat fades after 10 s without damage, otherwise two hurt pilots
        // hide from each other forever.
        float nerve = MathF.Exp(-MathF.Max(0f, _now - _lastHurt - 10f) / 12f);
        Scores[(int)AiAction.Retreat] = hp < 0.45f ? ((0.45f - hp) * 2.6f + 0.15f + (vis && dist < p.Range * 0.8f ? 0.15f : 0f)) * nerve : 0f;
        Scores[(int)AiAction.Search] = k == null ? 0.6f : age >= 4f ? 0.55f : 0f;
        Scores[(int)Action] += 0.12f; // commitment bonus: prevents flip-flopping

        var act = AiAction.Search;
        float top = -1f;
        for (int i = 0; i < ActionCount; i++)
            if (Scores[i] > top) { top = Scores[i]; act = (AiAction)i; }
        if (k == null && act is AiAction.Engage or AiAction.Flank) act = AiAction.Search;

        if (act != Action)
        {
            string why = act switch
            {
                AiAction.Engage => vis ? $"target visible at {dist:0} m" : "closing on last known position",
                AiAction.Cool => $"heat {heat * 100f:0}%",
                AiAction.Flank => $"no line of sight for {_noLos:0.0} s",
                AiAction.Retreat => $"structural integrity {hp * 100f:0}%",
                _ => k != null ? $"contact lost {age:0} s ago" : "no contacts",
            };
            Note($"{Name(Action)} → {Name(act)}: {why}");
            Why = why;
            Action = act;
            CoverIndex = -1;
        }

        if (_now > _orbitUntil)
        {
            _orbit = -_orbit;
            _orbitUntil = _now + 6f + _rng.NextFloat() * 6f;
        }

        Destination = PlanDestination(b, me, k);
        DesiredHeading = Steer(b, me, Destination);

        // Aim error grows with own speed, heat and target speed.
        float targetSpeed = k != null ? new Vector2(k.Velocity.X, k.Velocity.Z).Length() / ReferenceSpeed : 0f;
        Sigma = 0.006f + 0.014f * MathF.Abs(me.Sim.Locomotion.Body.V) / me.Chassis.VMax
                       + 0.03f * MathF.Max(0f, heat - 0.45f) + 0.008f * targetSpeed;
        _errYaw = _rng.NextGaussian() * Sigma;
        _errPitch = _rng.NextGaussian() * Sigma * 0.6f;
        _dmgRecent *= 0.6f;
    }

    /// <summary>Line of sight from the cockpit to the enemy torso (300 m), radar inside 130 m, and being shot.</summary>
    private void Perceive(BattleSim b, BattleUnit me)
    {
        Vector3 eye = me.Volume(MechSection.Cockpit).Center;
        foreach (var e in b.Units)
        {
            if (e.Team == me.Team) continue;
            if (e.IsOut)
            {
                if (Contacts.Remove(e) && e == Target) Target = null;
                continue;
            }
            float d = MathUtil.PlanarDistance(me.Position, e.Position);
            float seenHeight = 0f;
            bool vis = d < SensorRange && Sees(b.Map, eye, e, out seenHeight);
            Contacts.TryGetValue(e, out var k);
            if (vis)
            {
                if (k == null)
                {
                    k = new Contact { Position = e.Position, LastSeen = _now, FirstSeen = _now };
                    Contacts[e] = k;
                    Note($"contact: {e.Name} at {d:0} m");
                }
                else
                {
                    float dt = MathF.Max(0.05f, _now - k.LastSeen);
                    k.Velocity = Vector3.Lerp(k.Velocity, (e.Position - k.Position) / dt, 0.5f);
                    k.Position = e.Position;
                    k.LastSeen = _now;
                    if (!k.Visible) k.FirstSeen = _now;
                }
                k.Visible = true;
                Observe(k, e);
                k.AimHeight = seenHeight;
            }
            else if (d < RadarRange)
            {
                if (k == null)
                {
                    k = new Contact { Position = e.Position, LastSeen = _now, FirstSeen = _now };
                    Contacts[e] = k;
                    Note($"radar contact: {e.Name} at {d:0} m");
                }
                else
                {
                    k.Position = e.Position;
                    k.LastSeen = _now;
                }
                k.Visible = false;
                Observe(k, e);
            }
            else if (k != null) k.Visible = false;
        }

        if (_lastAttacker is { IsOut: false } at && at.Team != me.Team && !(Contacts.TryGetValue(at, out var ka) && ka.Visible))
        {
            if (ka == null)
            {
                ka = new Contact { FirstSeen = _now };
                Contacts[at] = ka;
                Observe(ka, at);
                Note($"taking fire from {at.Name}");
            }
            ka.Position = at.Position;
            ka.LastSeen = _now;
        }
        _lastAttacker = null;
    }

    /// <summary>
    /// Line of sight to the parts a pilot would recognise: torso centre, top of the torso, cockpit. A mech hull-down
    /// behind a crest is still seen (and shot at) by whatever shows; <paramref name="aimHeight"/> is the lowest
    /// visible part's height above the ground.
    /// </summary>
    private static bool Sees(ArenaMap map, Vector3 eye, BattleUnit e, out float aimHeight)
    {
        var torso = e.Volume(MechSection.Torso);
        float ground = e.Position.Y;
        aimHeight = torso.Center.Y - ground;
        if (map.LineClear(eye, torso.Center)) return true;
        Vector3 top = torso.Center + Vector3.UnitY * (0.7f * torso.Radius);
        aimHeight = top.Y - ground;
        if (map.LineClear(eye, top)) return true;
        Vector3 cockpit = e.Volume(MechSection.Cockpit).Center;
        aimHeight = cockpit.Y - ground;
        return map.LineClear(eye, cockpit);
    }

    private static void Observe(Contact k, BattleUnit e)
    {
        float ground = e.Position.Y;
        k.AimHeight = e.Volume(MechSection.Torso).Center.Y - ground;
        k.EyeHeight = e.Volume(MechSection.Cockpit).Center.Y - ground;
    }

    private void SelectTarget(BattleSim b, BattleUnit me)
    {
        b.TeamFocus.TryGetValue(me.Team, out var focus);
        BattleUnit? best = null;
        float bestScore = float.NegativeInfinity;
        foreach (var (e, k) in Contacts)
        {
            float age = _now - k.LastSeen;
            if (age > ContactMemory) continue;
            float s = (k.Visible ? 0.6f : 0f) - MathUtil.PlanarDistance(me.Position, k.Position) / 500f + (focus == e ? 0.5f : 0f)
                      + (1f - e.Sim.Damage.Fraction(MechSection.Torso)) * 0.4f - age * 0.03f + (e == Target ? 0.15f : 0f);
            if (s > bestScore) { bestScore = s; best = e; }
        }
        if (best == Target) return;
        Target = best;
        if (best != null) Note($"target → {best.Name}{(focus == best ? " (team focus)" : "")}");
    }

    private Vector3 PlanDestination(BattleSim b, BattleUnit me, Contact? k)
    {
        var map = b.Map;
        float range = Personality.Range;
        switch (Action)
        {
            case AiAction.Engage when k != null:
            {
                // Orbit point at preferred range, ±0.35 rad around the target.
                float a = MathUtil.Heading(me.Position - k.Position) + _orbit * 0.35f;
                var orbit = map.ClampDestination(k.Position + MathUtil.Forward(a) * range);
                return map.Terrain != null ? FiringPosition(map, me, k, a) : orbit;
            }
            case AiAction.Cool or AiAction.Retreat:
            {
                if (CoverIndex < 0 || _now > _coverUntil || k != null && !Hidden(me, map.Cover[CoverIndex].Position, k, map))
                {
                    CoverIndex = PickCover(b, me, k, Action == AiAction.Retreat);
                    _coverUntil = _now + 3f;
                }
                if (CoverIndex >= 0) return map.Cover[CoverIndex].Position;
                return map.ClampDestination(k != null ? me.Position - (k.Position - me.Position) : me.Position);
            }
            case AiAction.Flank when k != null:
            {
                float baseAngle = MathUtil.Heading(me.Position - k.Position);
                Vector3 best = me.Position;
                float bestScore = float.NegativeInfinity;
                float eyeHeight = me.Volume(MechSection.Cockpit).Center.Y - me.Position.Y;
                foreach (int side in new[] { 1, -1 })
                {
                    Vector3 pt = map.ClampDestination(k.Position + MathUtil.Forward(baseAngle + side * 1.4f) * (range * 0.85f));
                    bool clear = map.LineClear(pt + Vector3.UnitY * eyeHeight, k.Position + Vector3.UnitY * k.AimHeight);
                    float s = (clear ? 1f : 0f) - MathUtil.PlanarDistance(me.Position, pt) / 240f + (side == _orbit ? 0.1f : 0f);
                    if (s > bestScore) { bestScore = s; best = pt; }
                }
                return best;
            }
            default:
            {
                if (k != null) return map.ClampDestination(k.Position);
                if (_roam is not { } roam || MathUtil.PlanarDistance(me.Position, roam) < 20f)
                {
                    // Sweep toward the enemy half (away from where this mech started).
                    float enemySide = _home.Z > 0f ? -1f : 1f, r = map.Radius > 0f ? map.Radius : 100f;
                    roam = map.ClampDestination(new Vector3(_rng.Range(-0.63f, 0.63f) * r, 0f, enemySide * _rng.Range(-0.1f, 0.63f) * r));
                    _roam = roam;
                    Note($"sweeping toward ({roam.X:0}, {roam.Z:0})");
                }
                return roam;
            }
        }
    }

    /// <summary>Firing-position rings as fractions of preferred range; fine enough to find the band just behind a crest.</summary>
    private static readonly float[] FiringRanges = { 0.75f, 0.85f, 0.92f, 1f, 1.08f, 1.15f, 1.25f };

    /// <summary>
    /// Not in the prototype (flat ground): on terrain, ENGAGE picks where to stand. Candidates fan around the target
    /// at 0.75-1.25× preferred range (7 rings × 7 bearings, ±0.6 rad around the orbit bearing) plus the current spot. Each is scored on:
    /// the guns (lowest working muzzle) can reach the target, the legs (and ideally the torso) are hidden from the target's
    /// cockpit (hull-down behind a crest), range error, travel, and a little for high ground when hull-down.
    /// </summary>
    private Vector3 FiringPosition(ArenaMap map, BattleUnit me, Contact k, float bearing)
    {
        // Muzzle height: the lowest weapon that still works must clear the crest (arms hang below the torso).
        float ground = me.Position.Y, gunHeight = float.MaxValue;
        var weapons = me.Sim.Weapons;
        for (int w = 0; w < weapons.Mounts.Count; w++)
            if (!weapons.Mounts[w].Offline) gunHeight = MathF.Min(gunHeight, weapons.MuzzlePosition(w, me.Sim.Pose).Y - ground);
        if (gunHeight == float.MaxValue) gunHeight = me.Volume(MechSection.Torso).Center.Y - ground;
        float legHeight = 0.45f * me.Chassis.HipHeight;
        Vector3 targetEye = k.Position + Vector3.UnitY * k.EyeHeight;
        Vector3 targetAim = k.Position + Vector3.UnitY * k.AimHeight;
        float range = Personality.Range;

        Vector3 best = me.Position;
        float bestScore = float.NegativeInfinity;
        for (int i = -1; i < 49; i++)
        {
            Vector3 c;
            if (i < 0) c = me.Position;
            else
            {
                float a = bearing + (i % 7 - 3) * 0.2f, r = range * FiringRanges[i / 7];
                c = map.ClampDestination(k.Position + MathUtil.Forward(a) * r);
            }
            Vector3 up = Vector3.UnitY;
            bool shoot = map.LineClear(c + up * gunHeight, targetAim);
            bool legsHidden = !map.LineClear(targetEye, c + up * legHeight);
            bool torsoHidden = legsHidden && !map.LineClear(targetEye, c + up * (0.8f * gunHeight));
            // High ground only counts from behind a crest; on an exposed hilltop it just makes a better target.
            float score = (shoot ? 1f : -0.5f) + (legsHidden ? 0.6f : 0f) + (shoot && torsoHidden ? 0.25f : 0f)
                          - MathF.Abs(MathUtil.PlanarDistance(c, k.Position) - range) / 450f
                          - MathUtil.PlanarDistance(c, me.Position) / 300f
                          + (legsHidden ? MathUtil.Clamp(c.Y - k.Position.Y, -10f, 10f) * 0.015f : 0f)
                          + (MathUtil.PlanarDistance(c, Destination) < 15f ? 0.1f : 0f); // don't dither between equals
            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best;
    }

    /// <summary>Is a spot hidden from the contact's cockpit (checked against this mech's torso height)?</summary>
    public bool Hidden(BattleUnit me, Vector3 spot, Contact k, ArenaMap map)
    {
        float hide = 0.85f * (me.Volume(MechSection.Torso).Center.Y - me.Position.Y);
        return !map.LineClear(k.Position + Vector3.UnitY * k.EyeHeight, spot + Vector3.UnitY * hide);
    }

    private int PickCover(BattleSim b, BattleUnit me, Contact? k, bool retreat)
    {
        var cover = b.Map.Cover;
        int best = -1;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < cover.Count; i++)
        {
            var c = cover[i];
            float d = MathUtil.PlanarDistance(c.Position, me.Position);
            if (d > 200f) continue;
            bool hidden = k == null || Hidden(me, c.Position, k, b.Map);
            float dt = k != null ? MathUtil.PlanarDistance(c.Position, k.Position) : 100f;
            float s = (hidden ? 1.2f : 0f) - d / 120f
                      + (retreat ? dt / 300f : -MathF.Abs(dt - Personality.Range) / 450f)
                      + (c.BlockHeight > 15f ? 0.15f : 0f);
            if (k != null && dt < 50f) s -= 1f;
            foreach (var a in b.Units)
            {
                if (a == me || a.Team != me.Team || a.IsOut) continue;
                if (retreat) s += 0.35f * MathF.Exp(-MathUtil.PlanarDistance(c.Position, a.Position) / 80f);
                if (a.Ai?.CoverIndex == i) s -= 0.8f;
            }
            if (s > bestScore) { bestScore = s; best = i; }
        }
        return best;
    }

    /// <summary>
    /// Probe headings around the destination bearing; take the first with three clear parallel rays.
    /// The probes look 1.2 s ahead at current speed (prototype: a fixed 16 m).
    /// </summary>
    private float Steer(BattleSim b, BattleUnit me, Vector3 dest)
    {
        Vector3 to = dest - me.Position;
        float desired = MathUtil.Heading(to);
        float lookAhead = MathF.Max(16f, 1.2f * MathF.Abs(me.Sim.Locomotion.Body.V));
        float probe = MathF.Min(lookAhead, MathUtil.PlanarDistance(dest, me.Position));
        if (probe < 3f) return desired;
        float s = me.Chassis.Scale;
        foreach (float off in SteerOffsets)
        {
            float h = desired + off * _orbit;
            if (b.Map.PathClear(me.Position, h, 2.4f * s, probe, 3f * s)) return h;
        }
        return desired + MathF.PI / 2f;
    }

    /// <summary>Torso health weighted by how many weapons still work (0..1).</summary>
    public static float HealthFactor(BattleUnit me)
    {
        var mounts = me.Sim.Weapons.Mounts;
        int online = 0;
        foreach (var m in mounts) if (!m.Offline) online++;
        float armed = mounts.Count > 0 ? (float)online / mounts.Count : 0f;
        return me.Sim.Damage.Fraction(MechSection.Torso) * (0.7f + 0.3f * armed);
    }

    // ---------- control (every tick) ----------

    private MechInput Control(BattleSim b, BattleUnit me)
    {
        var p = Personality;
        var sim = me.Sim;
        var body = sim.Locomotion.Body;
        var c = me.Chassis;
        var input = new MechInput();

        // Legs: follow the planned heading; back up when the goal is behind during a fight.
        float dist = MathUtil.PlanarDistance(me.Position, Destination);
        float err = MathUtil.WrapAngle(DesiredHeading - body.Psi);
        Contact? k = Target != null && Contacts.TryGetValue(Target, out var kk) ? kk : null;
        if (dist < 5f) { }
        else if (Action is AiAction.Engage or AiAction.Retreat && k != null && MathF.Abs(err) > 2.1f && dist < 90f)
        {
            input.Turn = MathUtil.Clamp(MathUtil.WrapAngle(DesiredHeading + MathF.PI - body.Psi) * 2f, -1f, 1f);
            input.Throttle = -0.9f;
        }
        else
        {
            // Arrive without overshooting: no faster than the chassis can brake to a stop over the remaining
            // distance (prototype: dist / 20 m, fine at 4 m/s). Never slower than ~1.3 m/s.
            float brake = MathF.Sqrt(2f * c.Decel * MathF.Max(0f, dist - 4f)) / c.VMax;
            input.Turn = MathUtil.Clamp(err * 2.2f, -1f, 1f);
            input.Throttle = MathUtil.Clamp(brake, MathF.Min(0.3f, 1.3f / c.VMax), 1f) * MathF.Max(0.1f, MathF.Cos(err));
        }

        bool wasHolding = HoldingFire;
        TwistingAway = HoldingFire = false;
        if (k == null)
        {
            // Scan with the torso.
            input.TwistTarget = MathF.Sin(_now * 0.5f + me.Team) * 0.9f;
            return input;
        }

        // Torso: aim at the lead point (shell flight time) plus pilot error.
        var weapons = sim.Weapons;
        var shell = PrimaryProjectile(weapons);
        float age = _now - k.LastSeen;
        float d = MathUtil.PlanarDistance(me.Position, k.Position);
        float tof = shell != null ? d / shell.MuzzleVelocity : 0f;
        Vector3 lead = k.Position + k.Velocity * (age + tof);
        float bearing = MathUtil.Heading(lead - me.Position);
        float needTwist = MathUtil.WrapAngle(bearing + _errYaw - body.Psi);
        if (MathF.Abs(needTwist) > c.TwistMax * 0.9f && dist < 8f) input.Turn = MathUtil.Clamp(needTwist * 2f, -1f, 1f);
        // Not in the prototype (its mechs stood at their orbit point to fire): a fast mech running across the target's
        // bearing runs out of torso twist, so with a weapon ready and the target in sight it swings its legs round.
        else if (k.Visible && MathF.Abs(needTwist) > c.TwistMax * 0.85f && AnyReady(weapons)) input.Turn = MathUtil.Clamp(needTwist * 2f, -1f, 1f);

        // Spread damage: twist the weaker arm away while every weapon recycles.
        float extraTwist = 0f;
        if (p.TwistAway && k.Visible && Action != AiAction.Flank && AllRecycling(weapons, 0.3f))
        {
            TwistingAway = true;
            var dmg = sim.Damage;
            extraTwist = (dmg[MechSection.ArmRight] < dmg[MechSection.ArmLeft] ? -1f : 1f) * 0.8f;
        }

        // Open loop, as in the prototype: twist toward the bearing and pitch toward the drop-compensated lead point.
        // Torso sway and recoil are not compensated; the alignment check below waits for the aim ray to swing through.
        Vector3 aimBase = lead + Vector3.UnitY * k.AimHeight;
        float drop = shell != null ? 0.5f * shell.Gravity * tof * tof : 0f;
        AimPoint = aimBase + Vector3.UnitY * (drop + _errPitch * d) - MathUtil.Right(bearing) * (_errYaw * d);
        input.TwistTarget = MathUtil.Clamp(needTwist + extraTwist, -c.TwistMax, c.TwistMax);
        input.AimTarget = MathF.Atan2(aimBase.Y + drop - sim.Pose.AimOrigin.Y, MathF.Max(1f, d)) + _errPitch;
        input.Convergence = MathUtil.Clamp(d, 20f, 700f);

        if (!k.Visible || _now - k.FirstSeen < p.Reaction || TwistingAway) return input;

        // Fire discipline: alignment, range, heat budget, clear line of fire.
        float heatCap = Action == AiAction.Retreat ? p.HeatLimit + 0.05f : p.HeatLimit;
        float heatAfter = weapons.Heat;
        for (int i = 0; i < weapons.Mounts.Count; i++)
        {
            if (!weapons.CanFire(i)) continue;
            var spec = weapons.Mounts[i].Spec;
            bool projectile = spec.Kind == WeaponKind.Projectile;
            if (d >= (projectile ? spec.Range : spec.Range * 0.9f)) continue;
            if (heatAfter + spec.Heat >= heatCap) continue;
            Vector3 point = aimBase;
            if (projectile)
            {
                float t = d / spec.MuzzleVelocity;
                point.Y += 0.5f * spec.Gravity * t * t;
            }
            if (!Aligned(sim.Pose, point, 0.02f)) continue;
            switch (LineOfFire(b, me, weapons.MuzzlePosition(i, sim.Pose), point))
            {
                case FireLine.Clear:
                    input.FireMask |= 1u << i;
                    heatAfter += spec.Heat;
                    break;
                case FireLine.Ally:
                    HoldingFire = true;
                    break;
            }
        }
        if (HoldingFire && !wasHolding) Note("holding fire: ally in line");
        return input;
    }

    private static WeaponSpec? PrimaryProjectile(MechWeapons w)
    {
        foreach (var m in w.Mounts)
            if (!m.Offline && m.Spec.Kind == WeaponKind.Projectile) return m.Spec;
        return null;
    }

    private static bool AnyReady(MechWeapons w)
    {
        for (int i = 0; i < w.Mounts.Count; i++)
            if (w.CanFire(i)) return true;
        return false;
    }

    private static bool AllRecycling(MechWeapons w, float threshold)
    {
        bool any = false;
        foreach (var m in w.Mounts)
        {
            if (m.Offline) continue;
            if (m.Cooldown <= threshold) return false;
            any = true;
        }
        return any;
    }

    /// <summary>Aim-ray angle to the point is inside the tolerance (which widens up close).</summary>
    private static bool Aligned(MechPose pose, Vector3 point, float tolerance)
    {
        Vector3 v = point - pose.AimOrigin;
        float d = v.Length();
        return MathF.Acos(MathUtil.Clamp(Vector3.Dot(pose.AimDirection, v / d), -1f, 1f)) < tolerance + 1.6f / d;
    }

    private enum FireLine { Clear, Blocked, Ally }

    private static FireLine LineOfFire(BattleSim b, BattleUnit me, Vector3 from, Vector3 to)
    {
        if (!b.Map.LineClear(from, to)) return FireLine.Blocked;
        Vector3 d = to - from;
        float len = d.Length();
        d /= len;
        foreach (var a in b.Units)
        {
            if (a == me || a.Team != me.Team || a.IsOut) continue;
            if (HitTest.RaySphere(from, d, a.Volume(MechSection.Torso).Center, 3.5f * a.Chassis.Scale, out float t) && t < len)
                return FireLine.Ally;
        }
        return FireLine.Clear;
    }

    private void Note(string text)
    {
        _log.Insert(0, $"{_now,5:0.0}s {text}");
        if (_log.Count > LogLength) _log.RemoveAt(_log.Count - 1);
    }
}
