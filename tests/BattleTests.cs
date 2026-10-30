using System;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using Xunit.Abstractions;

namespace IronFeud.Tests;

public class ArenaMapTests
{
    [Fact]
    public void Arena_IsDeterministic_AndKeepsSpawnZonesOpen()
    {
        var a = ArenaMap.Arena(7);
        var b = ArenaMap.Arena(7);
        Assert.Equal(a.Blocks.Count, b.Blocks.Count);
        Assert.True(a.Blocks.Count >= 20, $"only {a.Blocks.Count} blocks");
        Assert.Equal(a.Blocks.Select(x => x.Min), b.Blocks.Select(x => x.Min));
        Assert.False(a.InsideAnyBlock(0f, 75f, 3f));
        Assert.False(a.InsideAnyBlock(0f, -75f, 3f));
        Assert.All(a.Cover, c => Assert.False(a.InsideAnyBlock(c.Position.X, c.Position.Z, 2.5f)));
        Assert.True(a.Cover.Count > a.Blocks.Count * 4);
    }

    [Fact]
    public void Rays_StopAtBlocksAndGround()
    {
        var map = new ArenaMap();
        map.Blocks.Add(new Block(0f, -20f, 4f, 4f, 10f));
        Assert.Equal(18f, map.RayDistance(new Vector3(0, 5, 0), -Vector3.UnitZ, 100f), 3);
        Assert.Equal(100f, map.RayDistance(new Vector3(0, 12, 0), -Vector3.UnitZ, 100f), 3);
        Assert.Equal(5f, map.RayDistance(new Vector3(30, 5, 0), -Vector3.UnitY, 100f), 3);
        Assert.False(map.LineClear(new Vector3(0, 5, 0), new Vector3(0, 5, -40)));
        Assert.True(map.LineClear(new Vector3(0, 12, 0), new Vector3(0, 12, -40)));
    }

    [Fact]
    public void PushOut_MovesACircleClearOfABlock()
    {
        var map = new ArenaMap();
        map.Blocks.Add(new Block(0f, 0f, 10f, 10f, 10f));
        Vector3 p = map.PushOut(new Vector3(6f, 0f, 1f), 3f);
        Assert.Equal(8f, p.X, 4);
        Assert.Equal(1f, p.Z, 4);
        Vector3 inside = map.PushOut(new Vector3(4f, 0f, 0f), 3f);
        Assert.Equal(8f, inside.X, 4);
    }

    [Fact]
    public void ClampDestination_StaysInsideTheRingAndOutOfBlocks()
    {
        var map = ArenaMap.Arena(3);
        var rng = new Logic.Core.DeterministicRng(9);
        for (int i = 0; i < 500; i++)
        {
            var p = map.ClampDestination(new Vector3(rng.Range(-150, 150), 0, rng.Range(-150, 150)));
            // The 6 m margin can be eaten by a block push near the edge; it must still be well inside the ring.
            Assert.True(p.Length() <= map.Radius - 3f, $"{p} outside the ring ({p.Length():0.0} m)");
            Assert.False(map.InsideAnyBlock(p.X, p.Z, 3.9f), $"{p} inside a block");
        }
    }
}

public class BattleTests
{
    private const float H = 1f / 120f;
    private readonly ITestOutputHelper _out;

    public BattleTests(ITestOutputHelper output) => _out = output;

    private static BattleSim Open() => new(new ArenaMap()) { TrackOutcome = false };

    [Fact]
    public void Laser_HitsAMechInFront_AndCreditsTheShooter()
    {
        var b = Open();
        var shooter = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f);
        var target = b.AddUnit("B", 1, MechChassis.Medium(), new Vector3(0, 0, -60), MathF.PI);
        float twist = 0, aim = 0;
        for (int i = 0; i < 120 * 3; i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = i == 240 ? 0b10u : 0u, Convergence = 60f };
            b.Step(H);
        }
        Assert.Equal(1, shooter.Stats.Shots);
        Assert.Equal(1, shooter.Stats.Hits);
        Assert.Equal(target.Chassis.TorsoHp - 9f, target.Sim.Damage[MechSection.Torso], 3);
    }

    [Fact]
    public void Blocks_StopShells()
    {
        var b = new BattleSim(new ArenaMap()) { TrackOutcome = false };
        b.Map.Blocks.Add(new Block(0f, -30f, 20f, 4f, 30f));
        var shooter = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f);
        var target = b.AddUnit("B", 1, MechChassis.Medium(), new Vector3(0, 0, -60), MathF.PI);
        shooter.Input = new MechInput { FireMask = 0b01u, Convergence = 60f };
        for (int i = 0; i < 120 * 2; i++) b.Step(H);
        Assert.True(shooter.Stats.Shots >= 2);
        Assert.Equal(0, shooter.Stats.Hits);
        Assert.Equal(target.Chassis.TorsoHp, target.Sim.Damage[MechSection.Torso]);
    }

    [Fact]
    public void Mechs_CannotWalkThroughEachOtherOrBlocks()
    {
        var b = Open();
        b.Map.Blocks.Add(new Block(-20f, -40f, 10f, 10f, 10f));
        var a = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f);
        var c = b.AddUnit("B", 0, MechChassis.Medium(), new Vector3(0, 0, -20), 0f);
        var w = b.AddUnit("W", 0, MechChassis.Light(), new Vector3(-20, 0, -20), 0f);
        a.Input = new MechInput { Throttle = 1f };
        w.Input = new MechInput { Throttle = 1f };
        float minGap = float.MaxValue;
        for (int i = 0; i < 120 * 10; i++)
        {
            b.Step(H);
            minGap = MathF.Min(minGap, Logic.Core.MathUtil.PlanarDistance(a.Position, c.Position));
            Assert.False(b.Map.InsideAnyBlock(w.Position.X, w.Position.Z, 0f), "walked into a block");
        }
        Assert.True(minGap > 5.5f, $"mechs overlapped to {minGap:0.00} m");
    }

    private static BattleSim Duel(uint seed, Personality p0, Personality p1, ChassisClass c0 = ChassisClass.Medium, ChassisClass c1 = ChassisClass.Medium)
    {
        var b = new BattleSim(ArenaMap.Arena(seed), seed);
        b.AddUnit("Alpha-1", 0, MechChassis.Create(c0), new Vector3(0, 0, 75), 0f, p0);
        b.AddUnit("Bravo-1", 1, MechChassis.Create(c1), new Vector3(0, 0, -75), MathF.PI, p1);
        return b;
    }

    private static float RunUntilOver(BattleSim b, float maxSeconds)
    {
        int n = (int)(maxSeconds / H);
        for (int i = 0; i < n && !b.Over; i++) b.Step(H);
        return b.Time;
    }

    [Fact]
    public void AiDuels_EndWithAWinner()
    {
        // Several seeds and matchups; prints a summary for tuning (dotnet test --logger "console;verbosity=detailed").
        var matchups = new (Personality, Personality, ChassisClass, ChassisClass)[]
        {
            (Personality.Balanced, Personality.Brawler, ChassisClass.Medium, ChassisClass.Medium),
            (Personality.Sniper, Personality.Brawler, ChassisClass.Medium, ChassisClass.Medium),
            (Personality.Balanced, Personality.Balanced, ChassisClass.Light, ChassisClass.Medium),
        };
        int pilotKills = 0, total = 0;
        foreach (var (p0, p1, c0, c1) in matchups)
        for (uint seed = 1; seed <= 4; seed++)
        {
            var b = Duel(seed, p0, p1, c0, c1);
            float t = RunUntilOver(b, 400f);
            Assert.True(b.Over, $"{p0.Name}/{c0} vs {p1.Name}/{c1} seed {seed}: no winner after {t:0} s");
            total++;
            if (b.Units.Any(u => u.Sim.Damage.Status == MechStatus.PilotKilled)) pilotKills++;
            _out.WriteLine($"{p0.Name}/{c0} vs {p1.Name}/{c1} seed {seed}: winner {b.Winner} after {t:0} s · " +
                           string.Join(" · ", b.Units.Select(u => $"{u.Name} {u.Sim.Damage.Status} {u.Stats.Hits}/{u.Stats.Shots} hits {u.Stats.DamageDealt:0} dmg")));
        }
        _out.WriteLine($"{pilotKills}/{total} matches ended by a cockpit kill");
    }

    [Fact]
    public void AiBattle_IsDeterministic()
    {
        string Fingerprint()
        {
            var b = Duel(11, Personality.Sniper, Personality.Balanced, ChassisClass.Light);
            for (int i = 0; i < 120 * 40; i++) b.Step(H);
            return string.Join("|", b.Units.Select(u => $"{u.Position} {u.Sim.Damage[MechSection.Torso]} {u.Stats.Shots}"));
        }
        Assert.Equal(Fingerprint(), Fingerprint());
    }

    [Fact]
    public void Ai_NeverFiresPastItsHeatLimit()
    {
        var b = Duel(5, Personality.Sniper, Personality.Brawler);
        float maxHeat = 0f;
        for (int i = 0; i < 120 * 90 && !b.Over; i++)
        {
            b.Step(H);
            foreach (var u in b.Units) maxHeat = MathF.Max(maxHeat, u.Sim.Weapons.Heat);
        }
        // Brawler limit 0.92 (+0.05 retreating); no pilot ever locks its weapons.
        Assert.True(maxHeat < 1f, $"heat reached {maxHeat:0.00}");
    }

    [Fact]
    public void Ai_WithoutLineOfSightOrRadar_DoesNotKnowTheEnemy()
    {
        var b = new BattleSim(new ArenaMap()) { TrackOutcome = false };
        b.Map.Blocks.Add(new Block(0f, -100f, 60f, 6f, 40f));
        var me = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Balanced);
        var enemy = b.AddUnit("B", 1, MechChassis.Medium(), new Vector3(0, 0, -200), 0f);
        for (int i = 0; i < 120 * 2; i++) b.Step(H);
        Assert.Empty(me.Ai!.Contacts);
        Assert.Equal(AiAction.Search, me.Ai.Action);

        // Being shot reveals the attacker.
        b.ApplyHit(me, MechSection.Torso, 1f, me.Position, enemy);
        for (int i = 0; i < 120; i++) b.Step(H);
        Assert.True(me.Ai.Contacts.ContainsKey(enemy));
        Assert.Equal(enemy, me.Ai.Target);
    }
}
