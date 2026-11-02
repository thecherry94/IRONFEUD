using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>
/// The AI walks round what its legs can't pass. Player report: "Alpha-2 got stuck on the corner of this building and
/// became a sitting duck". Causes: the probes passed over low and sunken blocks the collision walls, measured from the
/// terrain while the collision measured from the body (half a metre lower on slopes), ignored wrecks, and the detour
/// side flipped with the orbit side (back and forth along a wall). <see cref="StuckTime_InWatchLances"/> over 20
/// watch-mode lances per map, seconds pressing into obstacles: No Man's Land 325 -> 51, hills 1198 -> 195 (what is left
/// is slow climbing beside blocks); stalls over 3 s: at least four (up to 65 s) -> none.
/// </summary>
public class SteeringTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public SteeringTests(ITestOutputHelper output) => _out = output;

    private static MechDamage Tough(MechChassis c)
    {
        var d = MechDamage.Create(c);
        foreach (var s in new[] { MechSection.Torso, MechSection.Legs, MechSection.ArmLeft, MechSection.ArmRight, MechSection.Cockpit })
            d.Set(s, d.MaxArmor[(int)s], 1e6f);
        return d;
    }

    /// <summary>
    /// A medium heading for a target 500 m off, a 2 m wall (under a knee-height probe) across its way: it walks round,
    /// keeping to one side. Before: it walked into the wall; seeing it, it still ran back and forth along it for 30 s
    /// because its detour side followed its orbit side, which flips every 6-12 s.
    /// </summary>
    [Fact]
    public void Medium_WalksRoundALowWall()
    {
        var map = new ArenaMap { Radius = 600f };
        map.Blocks.Add(new Block(0f, 0f, 120f, 6f, 2f));
        var (b, m) = Scene(map, new Vector3(0f, 0f, 40f), 0f, new Vector3(0f, 0f, -500f));
        float worst = Run(b, m, () => m.Position.Z < -20f);
        _out.WriteLine($"low wall: reached z {m.Position.Z:0} at x {m.Position.X:0} by {b.Time:0} s, longest stall {worst:0} s");
        Assert.True(m.Position.Z < -20f, $"still at z {m.Position.Z:0} after {b.Time:0} s");
        Assert.True(worst < 3f, $"pressed into the wall for {worst:0} s");
    }

    /// <summary>
    /// Wrecks never move and shove like walls: the walk check sees one ahead in the corridor, not one beside or behind,
    /// nor a live mech (those give way). A light wedged between its lancemate's wreck and a block face stood there for
    /// the rest of a match.
    /// </summary>
    [Fact]
    public void WalkClear_SeesWrecks_ButNotLiveMechs()
    {
        var b = new BattleSim(new ArenaMap { Radius = 600f }, 4) { TrackOutcome = false };
        var m = b.AddUnit("M", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Balanced);
        var live = b.AddUnit("L", 1, MechChassis.Medium(), new Vector3(4f, 0f, -12f), 0f);
        Assert.True(b.WalkClear(m, 0f, 20f), "a live mech gives way");
        var w = b.AddUnit("W", 0, MechChassis.Medium(), new Vector3(4f, 0f, -12f), 0f);
        w.Sim.ApplyHit(MechSection.Cockpit, 1e6f);
        Assert.True(w.IsOut);
        Assert.False(b.WalkClear(m, 0f, 20f), "the wreck 4 m off the centre line is in the way");
        Assert.True(b.WalkClear(m, 0f, 8f), "beyond the probe");
        Assert.True(b.WalkClear(m, 1.2f, 20f), "turned away from it");
        Assert.True(b.WalkClear(m, MathF.PI, 20f), "behind");
    }

    /// <summary>No jets (it must walk), a tough target it knows about but can't reach with its guns from the start.</summary>
    private static (BattleSim, BattleUnit) Scene(ArenaMap map, Vector3 start, float heading, Vector3 targetAt)
    {
        var b = new BattleSim(map, 4) { TrackOutcome = false, JumpTactics = JumpTactics.None };
        var chassis = MechChassis.Medium();
        var m = b.AddUnit("M", 0, chassis, start, heading, Personality.Balanced, Tough(chassis));
        var heavy = MechChassis.Heavy();
        var target = b.AddUnit("H", 1, heavy, targetAt, heading + MathF.PI, damage: Tough(heavy));
        m.Ai!.Contacts[target] = new Contact { Position = target.Position, LastSeen = 0f, FirstSeen = -5f };
        return (b, m);
    }

    /// <summary>Steps up to 30 s or until <paramref name="done"/>; returns the longest stretch (s) of throttle on without moving.</summary>
    private static float Run(BattleSim b, BattleUnit m, Func<bool> done)
    {
        float pressing = 0f, worst = 0f;
        Vector3 before = m.Position;
        for (int i = 0; i < 120 * 30 && !done(); i++)
        {
            b.Step(H);
            if (i % 120 != 119) continue;
            bool stalled = MathUtil.PlanarDistance(before, m.Position) < 2f && m.Input.Throttle > 0.25f;
            pressing = stalled ? pressing + 1f : 0f;
            worst = MathF.Max(worst, pressing);
            before = m.Position;
        }
        return worst;
    }

    /// <summary>
    /// Report: watch-mode lances (20 per map), seconds spent pressing into a block or wreck (throttle on, within a
    /// stride of a block, covering under a tenth of top speed per second over 2 s, not shut down), and every stall longer than 3 s.
    /// IRONFEUD_BALANCE=1 dotnet test tests/ --filter StuckTime --logger "console;verbosity=detailed"
    /// </summary>
    [BalanceFact]
    public void StuckTime_InWatchLances()
    {
        int longStalls = 0;
        foreach (bool hills in new[] { false, true })
        {
            float stuck = 0f, alive = 0f;
            for (uint seed = 1; seed <= 20; seed++)
            {
                var b = WatchLance(seed, hills);
                var pressing = b.Units.ToDictionary(u => u, _ => 0f);
                var trail = b.Units.ToDictionary(u => u, _ => new Queue<Vector3>());
                for (int i = 0; i < 300 * 120 && !b.Over; i++)
                {
                    b.Step(H);
                    foreach (var u in b.Units)
                    {
                        if (u.IsOut) continue;
                        alive += H;
                        var q = trail[u];
                        q.Enqueue(u.Position);
                        if (q.Count > 240) q.Dequeue();
                        bool stalled = q.Count == 240 && MathUtil.PlanarDistance(q.Peek(), u.Position) < 0.2f * u.Chassis.VMax
                                       && !u.Sim.Locomotion.Body.Airborne && !u.Sim.Shutdown && MathF.Abs(u.Input.Throttle) > 0.25f
                                       && b.Map.InsideAnyBlock(u.Position.X, u.Position.Z, u.Radius + 1.5f);
                        if (stalled) { pressing[u] += H; stuck += H; continue; }
                        if (pressing[u] > 3f)
                        {
                            longStalls++;
                            _out.WriteLine($"  {(hills ? "hills" : "nml")} seed {seed}: {u.Name} stalled {pressing[u]:0.0} s at ({u.Position.X:0}, {u.Position.Z:0}) by {b.Time:0} s");
                        }
                        pressing[u] = 0f;
                    }
                }
                // Stalled until the end, or until destroyed there.
                foreach (var (u, t) in pressing)
                    if (t > 3f)
                    {
                        longStalls++;
                        _out.WriteLine($"  {(hills ? "hills" : "nml")} seed {seed}: {u.Name} stalled {t:0.0} s at ({u.Position.X:0}, {u.Position.Z:0}) to the end");
                    }
            }
            _out.WriteLine($"{(hills ? "hills" : "nml  ")}: {stuck:0} s pressing into obstacles of {alive:0} s alive");
        }
        Assert.Equal(0, longStalls);
    }

    /// <summary>The watch-mode lance (Main.SpawnTeams) on No Man's Land or the hills arena.</summary>
    private static BattleSim WatchLance(uint seed, bool hills)
    {
        var b = new BattleSim(hills ? ArenaMap.Arena(seed) : ArenaMap.NoMansLand(seed), seed * 31u + 1u) { TrackOutcome = false };
        foreach (var (team, sign) in new[] { (0, 1f), (1, -1f) })
        {
            void Add(ChassisClass c, float x, float z, Personality p, MechModel model) =>
                b.AddUnit($"{team}-{c}", team, MechChassis.Create(c, model), new Vector3(x * sign, 0f, z * sign), team == 0 ? 0f : MathF.PI, p);
            const float z = ArenaMap.ArenaSpawnZ, z2 = z + 17f;
            Add(ChassisClass.Light, -45f, z, Personality.Balanced, MechModel.Sicklefoot);
            Add(ChassisClass.Medium, -15f, z, Personality.Balanced, MechModel.Bascinet);
            Add(ChassisClass.Heavy, 15f, z2, Personality.Balanced, MechModel.Sicklefoot);
            Add(ChassisClass.Assault, 45f, z2, Personality.Sniper, MechModel.Bascinet);
        }
        return b;
    }
}
