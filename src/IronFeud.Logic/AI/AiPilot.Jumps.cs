using System;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Logic.AI;

/// <summary>What an AI jump is for.</summary>
public enum JumpPlan { None, PopUp, Vault, Traverse, Dodge, Jink, Bound }

/// <summary>Jump tactics a battle allows its AI pilots (see <see cref="BattleSim.JumpTactics"/>).</summary>
[Flags]
public enum JumpTactics
{
    None = 0, PopUp = 1, Vault = 2, Traverse = 4, Dodge = 8, Jink = 16, Bound = 32,
    All = PopUp | Vault | Traverse | Dodge | Jink | Bound,
}

/// <summary>
/// Jump-jet tactics, all through <see cref="MechInput.Jump"/> like a player's Space bar:
/// <list type="bullet">
/// <item>Pop-up (mediums): hidden behind a crest from a target seen moments ago, with a gun ready, burn just long enough
/// for the muzzles to clear the crest, hold position, fire at the top and drop back into cover.</item>
/// <item>Vault (lights, alone on the target): head-on to a slow-turning heavy or assault, run straight at where it will
/// be and take off when the climb clears it there; overhead it can't track the light, which yaws round in the air to
/// land facing its back.</item>
/// <item>Traverse: a climb too steep to walk, within reach of the jets, is jumped instead of walked round.</item>
/// <item>Dodge: a heavy shell about to hit (visible tracer, 0.5-1.5 s out) is hopped over.</item>
/// <item>Jink (lights): under fire, a short hop now and then: it breaks the enemy's lead and a laser's burn.</item>
/// <item>Bound (lights): running flat out into rough ground (trenches, crater rims), hop it instead of slowing.</item>
/// </list>
/// Pop-ups are shoot-and-scoot: after landing the medium relocates, and pops at most every 5 s.
/// </summary>
public sealed partial class AiPilot
{
    /// <summary>The jump in progress or about to start (debug).</summary>
    public JumpPlan Jump { get; private set; }

    /// <summary>A light lined up to vault its target (running straight at it, waiting for the take-off distance).</summary>
    public bool VaultArmed { get; private set; }

    private float _burnLeft, _lastPop = -99f;
    private bool _launched;

    private const float PopMinCharge = 0.6f, VaultMinCharge = 0.85f, TraverseMinCharge = 0.6f, DodgeMinCharge = 0.3f;

    /// <summary>Net upward acceleration while burning.</summary>
    private static float Net(MechChassis c) => c.JumpThrust - MechLocomotion.Gravity;

    /// <summary>Height gained at the apex of a burn of <paramref name="burn"/> seconds from standing (burn, then coast).</summary>
    public static float ApexGain(MechChassis c, float burn)
    {
        float n = Net(c);
        return n * burn * burn * (0.5f + n / (2f * MechLocomotion.Gravity));
    }

    /// <summary>Seconds from take-off to the apex of a burn of <paramref name="burn"/> seconds.</summary>
    public static float ApexTime(MechChassis c, float burn) => burn + Net(c) * burn / MechLocomotion.Gravity;

    /// <summary>Height above the take-off point <paramref name="t"/> seconds after lighting a burn of <paramref name="burn"/> s.</summary>
    public static float HeightAt(MechChassis c, float burn, float t)
    {
        float n = Net(c);
        if (t <= burn) return 0.5f * n * t * t;
        float dt = t - burn;
        return 0.5f * n * burn * burn + n * burn * dt - 0.5f * MechLocomotion.Gravity * dt * dt;
    }

    /// <summary>Burn needed to rise <paramref name="height"/> metres.</summary>
    public static float BurnFor(MechChassis c, float height)
    {
        float n = Net(c);
        return MathF.Sqrt(MathF.Max(0f, height) / (n * (0.5f + n / (2f * MechLocomotion.Gravity))));
    }

    private JumpTactics _tactics = JumpTactics.All;

    private bool CanJump(BattleUnit me, float minCharge)
    {
        var loco = me.Sim.Locomotion;
        return me.Chassis.HasJumpJets && !loco.Body.Airborne && Jump == JumpPlan.None && loco.JumpCharge >= minCharge;
    }

    private void StartJump(BattleUnit me, JumpPlan kind, float burn, string why)
    {
        burn = MathF.Min(burn, me.Sim.Locomotion.JumpFuel);
        if (burn < 0.15f) return;
        Jump = kind;
        _burnLeft = burn;
        _launched = false;
        VaultArmed = false;
        Note($"{kind.ToString().ToLowerInvariant()}: {why}, burn {burn:0.0} s");
    }

    /// <summary>Every tick, after <see cref="Control"/>: take off when lined up, hold the jets, end the jump on landing.</summary>
    private void FlyJump(BattleSim b, BattleUnit me, ref MechInput input, float h)
    {
        var loco = me.Sim.Locomotion;
        if (VaultArmed && Jump == JumpPlan.None && Target != null)
        {
            // Lined up: steer straight at where it will be every tick (the air gives a fifth of the control, too little
            // to fix a miss).
            float err = MathUtil.WrapAngle(MathUtil.Heading(VaultPoint(me) - me.Position) - loco.Body.Psi);
            input.Turn = MathUtil.Clamp(err * 3f, -1f, 1f);
            input.Throttle = 1f;
            TryVault(me);
        }
        if (_burnLeft > 0f)
        {
            input.Jump = true;
            _burnLeft -= h;
        }
        if (Jump == JumpPlan.Vault && loco.Body.Airborne && Target != null)
        {
            // Over and past it: swing round in the air to land facing its back.
            float err = MathUtil.WrapAngle(MathUtil.Heading(Target.Position - me.Position) - loco.Body.Psi);
            input.Turn = MathUtil.Clamp(err * 2f, -1f, 1f);
            input.Throttle = 0f;
        }
        if (Jump == JumpPlan.PopUp)
        {
            // Straight up and down: stay over the spot that hides us.
            input.Throttle = 0f;
            input.Turn = 0f;
        }
        if (loco.Body.Airborne) _launched = true;
        else if (Jump != JumpPlan.None && _burnLeft <= 0f)
        {
            // Landed (or never got off the ground). After a pop-up: move to a new spot, don't pop from here again.
            if (Jump == JumpPlan.PopUp && _launched)
            {
                _relocate = true;
                _lastPop = _now;
            }
            Jump = JumpPlan.None;
        }
    }

    /// <summary>
    /// Mid pop-up the target is checked every tick, not every think: the pilot knows where it is and the window at the
    /// top is about a second. Coming back into view after a deliberate pop counts as already reacted.
    /// </summary>
    private void RefreshPopTarget(BattleSim b, BattleUnit me)
    {
        if (Jump != JumpPlan.PopUp || Target == null || !Contacts.TryGetValue(Target, out var k)) return;
        if (!Sees(b.Map, me.Volume(MechSection.Cockpit).Center, Target, out float seen)) return;
        if (!k.Visible) k.FirstSeen = _now - Personality.Reaction;
        k.Visible = true;
        k.Position = Target.Position;
        k.LastSeen = _now;
        Observe(k, Target);
        k.AimHeight = seen;
    }

    /// <summary>Every think, once the action is chosen: start a pop-up or a dodge.</summary>
    private void PlanJump(BattleSim b, BattleUnit me, Contact? k, float dist, float age)
    {
        _tactics = b.JumpTactics;
        if (!me.Chassis.HasJumpJets || _tactics == JumpTactics.None) { VaultArmed = false; return; }
        if (Action is not (AiAction.Engage or AiAction.Flank) || !_tactics.HasFlag(JumpTactics.Vault)) VaultArmed = false;
        if (_tactics.HasFlag(JumpTactics.Dodge)) TryDodge(b, me);
        if (Role == AiRole.Skirmisher)
        {
            if (_tactics.HasFlag(JumpTactics.Jink)) TryJink(me);
            if (_tactics.HasFlag(JumpTactics.Bound)) TryBound(me);
        }
        if (_tactics.HasFlag(JumpTactics.PopUp) && Role == AiRole.Flex && k != null && !k.Visible && age < 8f && Action is AiAction.Engage or AiAction.Flank)
            TryPopUp(b, me, k, dist);
    }

    private void TryPopUp(BattleSim b, BattleUnit me, Contact k, float dist)
    {
        if (!CanJump(me, PopMinCharge) || !AnyReady(me.Sim.Weapons) || _now - _lastPop < 5f) return;
        var weapons = me.Sim.Weapons;
        float range = 0f;
        foreach (var m in weapons.Mounts) if (!m.Offline) range = MathF.Max(range, m.Spec.EffectiveRange);
        if (dist > range) return;
        float rise = PopHeight(b.Map, me, k);
        if (rise <= 0f) return;
        float burn = BurnFor(me.Chassis, rise + 1.5f);
        if (burn > 0.9f * me.Sim.Locomotion.JumpFuel) return;
        if (weapons.Heat + me.Chassis.JumpHeat * burn + 0.2f > Personality.HeatLimit) return;
        StartJump(me, JumpPlan.PopUp, burn, $"{rise:0} m over the crest at {Target?.Name}");
    }

    /// <summary>Smallest rise (m) that gives the lowest working muzzle a clear line to the target; 0 = none in reach.</summary>
    private float PopHeight(ArenaMap map, BattleUnit me, Contact k)
    {
        float gun = GunHeight(me);
        Vector3 at = k.Position + Vector3.UnitY * k.AimHeight;
        float reach = ApexGain(me.Chassis, 0.9f * me.Sim.Locomotion.JumpFuel);
        for (float rise = 2f; rise <= reach; rise += 2f)
            if (map.LineClear(me.Position + Vector3.UnitY * (gun + rise), at)) return rise;
        return 0f;
    }

    /// <summary>Height of the lowest working muzzle above the ground under the mech.</summary>
    private static float GunHeight(BattleUnit me)
    {
        var weapons = me.Sim.Weapons;
        float ground = me.Position.Y, gun = float.MaxValue;
        for (int w = 0; w < weapons.Mounts.Count; w++)
            if (!weapons.Mounts[w].Offline) gun = MathF.Min(gun, weapons.MuzzlePosition(w, me.Sim.Pose).Y - ground);
        return gun == float.MaxValue ? me.Volume(MechSection.Torso).Center.Y - ground : gun;
    }

    /// <summary>
    /// A light head-on to a slow-turning heavy or assault arms a vault: it runs straight at it (see
    /// <see cref="PlanDestination"/>) and <see cref="TryVault"/> lights the jets at the right distance.
    /// </summary>
    private bool WantsVault(BattleSim b, BattleUnit me, Contact k, float toRear)
    {
        var map = b.Map;
        if (Role != AiRole.Skirmisher || Target == null || !k.Visible || !_tactics.HasFlag(JumpTactics.Vault)) return false;
        if (Target.Chassis.Class is not (ChassisClass.Heavy or ChassisClass.Assault)) return false;
        float d = MathUtil.PlanarDistance(me.Position, k.Position), s = me.Chassis.Scale;
        // The run-up must be clear of blocks (the straight-at-it steering overrides obstacle avoidance; slopes are
        // fine, the jets carry over them).
        Vector3 fwd = MathUtil.Forward(MathUtil.Heading(k.Position - me.Position));
        float len = MathF.Max(0f, d - 15f);
        bool runUp = map.RayDistance(me.Position + Vector3.UnitY * (3f * s), fwd, len, ground: false) >= len;
        // The lone flanker's move: with a teammate on the target, plain flanking reaches its back without the
        // head-on run (measured: vaults then cost lights and kills).
        foreach (var u in b.Units)
            if (u != me && u.Team == me.Team && !u.IsOut && MathUtil.PlanarDistance(u.Position, Target.Position) < 250f) return VaultArmed = false;
        bool ok = runUp && (VaultArmed || CanJump(me, VaultMinCharge)) && MathF.Abs(toRear) > 1.8f && d is > 25f and < 160f
                  && me.Sim.Weapons.Heat < Personality.HeatLimit - 0.15f && ClearHeight(me) <= ApexGain(me.Chassis, me.Sim.Locomotion.JumpFuel);
        if (ok && !VaultArmed) Note($"lining up a vault over {Target.Name}");
        VaultArmed = ok;
        return ok;
    }

    /// <summary>Rise the light needs to pass over its target: the top of its torso plus 3 m, from our ground.</summary>
    private float ClearHeight(BattleUnit me)
    {
        var torso = Target!.Volume(MechSection.Torso);
        return torso.Center.Y + torso.Radius + 3f - me.Position.Y;
    }

    /// <summary>Where the target will be when the light gets there at its current speed (it moves 20-30 m in a vault's flight).</summary>
    private Vector3 VaultPoint(BattleUnit me)
    {
        var vel = Contacts.TryGetValue(Target!, out var k) ? k.Velocity : Vector3.Zero;
        float t = MathUtil.PlanarDistance(me.Position, Target!.Position) / MathF.Max(8f, me.Sim.Locomotion.Body.V);
        // At most 1 s ahead: a big mech that sees a light coming slows and pivots, and a longer straight-line guess
        // overshoots (30 m misses).
        return Target.Position + new Vector3(vel.X, 0f, vel.Z) * MathF.Min(t, 1f);
    }

    /// <summary>
    /// Lit with a full burn, will the light be high enough over the target when it gets there (at its current speed)?
    /// Then go: the window runs from where it would still be climbing past the target's height to where it would be
    /// coming down through it (75-125 m at a light's 36 m/s). Closer than that, the chance is gone.
    /// </summary>
    private void TryVault(BattleUnit me)
    {
        if (Target == null || Target.IsOut) { VaultArmed = false; return; }
        var c = me.Chassis;
        var body = me.Sim.Locomotion.Body;
        Vector3 over = VaultPoint(me);
        float d = MathUtil.PlanarDistance(me.Position, over), v = MathF.Max(8f, body.V);
        float burn = 0.95f * me.Sim.Locomotion.JumpFuel, clear = ClearHeight(me);
        float err = MathUtil.WrapAngle(MathUtil.Heading(over - me.Position) - body.Psi);
        bool high = HeightAt(c, burn, d / v) >= clear;
        if (high && MathF.Abs(err) < 0.08f) StartJump(me, JumpPlan.Vault, burn, $"over {Target.Name} from {d:0} m");
        else if (d < v * ApexTime(c, burn) && !high && HeightAt(c, burn, d / v) < clear && d / v < ApexTime(c, burn))
            VaultArmed = false; // inside the window's near edge: can't climb over it in time any more
    }

    /// <summary>
    /// Terrain ahead too steep to walk (the path check's grade rule) but low enough for the jets: jump it. Returns
    /// true if a traverse jump started (keep the heading then).
    /// </summary>
    private bool TryTraverse(BattleSim b, BattleUnit me, float heading, float length)
    {
        if (!_tactics.HasFlag(JumpTactics.Traverse) || !CanJump(me, TraverseMinCharge) || b.Map.Terrain == null) return false;
        const float step = 8f;
        Vector3 fwd = MathUtil.Forward(heading), p = me.Position;
        float start = b.Map.Height(p.X, p.Z), prev = start, top = start;
        bool steep = false;
        for (float d = step; d <= length; d += step)
        {
            Vector3 q = p + fwd * d;
            float h = b.Map.Height(q.X, q.Z);
            steep |= (h - prev) / step > ArenaMap.MaxClimbGrade;
            top = MathF.Max(top, h);
            prev = h;
        }
        if (!steep) return false;
        float rise = top - start + 3f;
        if (rise > ApexGain(me.Chassis, 0.9f * me.Sim.Locomotion.JumpFuel)) return false;
        StartJump(me, JumpPlan.Traverse, BurnFor(me.Chassis, rise), $"{rise - 3f:0} m climb ahead");
        return true;
    }

    /// <summary>Under fire (hit in the last second): a short hop half the time, keeping speed and course.</summary>
    private void TryJink(BattleUnit me)
    {
        if (_now - _lastHurt > 1f || !CanJump(me, 0.5f) || _rng.NextFloat() > 0.5f) return;
        if (me.Sim.Weapons.Heat > Personality.HeatLimit - 0.1f) return;
        StartJump(me, JumpPlan.Jink, 0.3f + 0.2f * _rng.NextFloat(), "under fire");
    }

    /// <summary>Running fast into ground rough enough to slow the legs (trench walls, crater rims): bound over it.</summary>
    private void TryBound(BattleUnit me)
    {
        var loco = me.Sim.Locomotion;
        if (loco.TerrainSpeedFactor > 0.7f || MathF.Abs(loco.Body.V) < 0.6f * me.Chassis.VMax || !CanJump(me, 0.5f)) return;
        if (me.Sim.Weapons.Heat > Personality.HeatLimit - 0.1f) return;
        StartJump(me, JumpPlan.Bound, 0.45f, $"rough ground ahead ({100f * loco.TerrainSpeedFactor:0}% speed)");
    }

    /// <summary>A heavy enemy shell on course for our torso, 0.5-1.5 s out: a short hop takes the torso out of its path.</summary>
    private void TryDodge(BattleSim b, BattleUnit me)
    {
        if (!CanJump(me, DodgeMinCharge)) return;
        var torso = me.Volume(MechSection.Torso);
        foreach (var s in b.Shells)
        {
            if (s.Owner.Team == me.Team || s.State.Spec.Damage < HeavyShot) continue;
            Vector3 v = s.State.Velocity, r = torso.Center - s.State.Position;
            float t = Vector3.Dot(r, v) / MathF.Max(1e-3f, v.LengthSquared());
            if (t is < 0.5f or > 1.5f) continue;
            if ((r - v * t).Length() > torso.Radius + 0.5f) continue;
            StartJump(me, JumpPlan.Dodge, 0.6f, $"{s.State.Spec.Name} inbound, {t:0.0} s");
            return;
        }
    }
}
