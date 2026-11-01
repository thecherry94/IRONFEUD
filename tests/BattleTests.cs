using System;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using Vector2 = System.Numerics.Vector2;
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
        Assert.True(a.Blocks.Count >= 60, $"only {a.Blocks.Count} blocks");
        Assert.Equal(a.Blocks.Select(x => x.Min), b.Blocks.Select(x => x.Min));
        Assert.False(a.InsideAnyBlock(0f, ArenaMap.ArenaSpawnZ, 3f));
        Assert.False(a.InsideAnyBlock(0f, -ArenaMap.ArenaSpawnZ, 3f));
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

    /// <summary>Shooter tracks a target 60 m ahead and pulls the left trigger once at t = 2 s.</summary>
    private (BattleUnit Shooter, BattleUnit Target) TrackAndFire(MechChassis shooterChassis, float seconds, Action<BattleSim, int>? during = null)
    {
        var b = Open();
        var shooter = b.AddUnit("A", 0, shooterChassis, Vector3.Zero, 0f);
        var target = b.AddUnit("B", 1, MechChassis.Medium(), new Vector3(0, 0, -60), MathF.PI);
        float twist = 0, aim = 0;
        for (int i = 0; i < (int)(120 * seconds); i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = i == 240 ? 0b10u : 0u, Convergence = 60f };
            during?.Invoke(b, i);
            b.Step(H);
        }
        return (shooter, target);
    }

    [Fact]
    public void LaserBurn_HeldOnTarget_DeliversItsFullDamage_AsOneHit()
    {
        var (shooter, target) = TrackAndFire(MechChassis.Medium(), 4f);
        Assert.Equal(1, shooter.Stats.Shots);
        Assert.Equal(1, shooter.Stats.Hits);
        Assert.Equal(target.Chassis.TorsoHp - WeaponSpec.Laser.Damage, target.Sim.Damage[MechSection.Torso], 2);
    }

    [Fact]
    public void LaserBurn_IsSpreadOverItsBurnTime()
    {
        float atHalf = 0f;
        var (_, target) = TrackAndFire(MechChassis.Medium(), 4f, (b, i) =>
        {
            if (i == 240 + (int)(120 * WeaponSpec.Laser.BurnTime / 2)) atHalf = b.Units[1].Chassis.TorsoHp - b.Units[1].Sim.Damage[MechSection.Torso];
        });
        Assert.InRange(atHalf / WeaponSpec.Laser.Damage, 0.4f, 0.6f);
    }

    [Fact]
    public void LaserBurn_StopsWhenItsArmIsDestroyed()
    {
        var (shooter, target) = TrackAndFire(MechChassis.Medium(), 4f, (b, i) =>
        {
            if (i == 240 + 60) b.ApplyHit(b.Units[0], MechSection.ArmLeft, 1000f, Vector3.Zero, null);
        });
        float dealt = target.Chassis.TorsoHp - target.Sim.Damage[MechSection.Torso];
        Assert.InRange(dealt / WeaponSpec.Laser.Damage, 0.3f, 0.5f);
        Assert.True(shooter.Sim.Weapons.Mounts[1].Offline);
    }

    [Fact]
    public void PulseLaser_FiresABurst_ThatAddsUpToItsDamage()
    {
        var light = MechChassis.Light(); // pulse laser on the left arm
        Assert.Equal(WeaponSpec.PulseLaser, light.LeftArmWeapon);
        var (shooter, target) = TrackAndFire(light, 3f);
        Assert.Equal(WeaponSpec.PulseLaser.Pulses, shooter.Stats.Shots);
        Assert.Equal(WeaponSpec.PulseLaser.Pulses, shooter.Stats.Hits);
        Assert.Equal(target.Chassis.TorsoHp - WeaponSpec.PulseLaser.Damage, target.Sim.Damage[MechSection.Torso], 2);
    }

    [Fact]
    public void Blocks_StopShells()
    {
        var b = new BattleSim(new ArenaMap()) { TrackOutcome = false };
        b.Map.Blocks.Add(new Block(0f, -30f, 20f, 4f, 30f));
        var shooter = b.AddUnit("A", 0, MechChassis.Prototype(), Vector3.Zero, 0f); // quick-firing cannon
        var target = b.AddUnit("B", 1, MechChassis.Prototype(), new Vector3(0, 0, -60), MathF.PI);
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
        b.AddUnit("Alpha-1", 0, MechChassis.Create(c0), new Vector3(0, 0, ArenaMap.ArenaSpawnZ), 0f, p0);
        b.AddUnit("Bravo-1", 1, MechChassis.Create(c1), new Vector3(0, 0, -ArenaMap.ArenaSpawnZ), MathF.PI, p1);
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
    public void AiDuels_InNoMansLand_EndWithAWinner_AndFightFromCover()
    {
        var matchups = new (Personality, Personality, ChassisClass, ChassisClass, MechModel)[]
        {
            (Personality.Balanced, Personality.Brawler, ChassisClass.Medium, ChassisClass.Medium, MechModel.Hauberk),
            (Personality.Sniper, Personality.Brawler, ChassisClass.Medium, ChassisClass.Medium, MechModel.Sicklefoot),
            (Personality.Balanced, Personality.Balanced, ChassisClass.Light, ChassisClass.Heavy, MechModel.Sicklefoot),
        };
        float engageAll = 0f, hullDownAll = 0f;
        foreach (var (p0, p1, c0, c1, model) in matchups)
        for (uint seed = 1; seed <= 3; seed++)
        {
            var b = new BattleSim(ArenaMap.NoMansLand(seed), seed);
            b.AddUnit("Alpha-1", 0, MechChassis.Create(c0, model), new Vector3(0, 0, ArenaMap.ArenaSpawnZ), 0f, p0);
            b.AddUnit("Bravo-1", 1, MechChassis.Create(c1), new Vector3(0, 0, -ArenaMap.ArenaSpawnZ), MathF.PI, p1);
            float engage = 0f, hullDown = 0f;
            for (int i = 0; i < 400 * 120 && !b.Over; i++)
            {
                b.Step(H);
                if (i % 12 != 0) continue; // sample at 10 Hz
                foreach (var u in b.Units)
                {
                    if (u.IsOut || u.Ai!.Action != AiAction.Engage) continue;
                    var enemy = b.Units[1 - u.Id];
                    engage += 0.1f;
                    var legs = u.Position + Vector3.UnitY * (0.45f * u.Chassis.HipHeight);
                    if (!b.Map.LineClear(enemy.Volume(MechSection.Cockpit).Center, legs)) hullDown += 0.1f;
                }
            }
            Assert.True(b.Over, $"{p0.Name}/{c0} {model} vs {p1.Name}/{c1} seed {seed}: no winner after {b.Time:0} s");
            engageAll += engage;
            hullDownAll += hullDown;
            _out.WriteLine($"{p0.Name}/{c0} {model} vs {p1.Name}/{c1} seed {seed}: winner {b.Winner} after {b.Time:0} s, " +
                           $"hull-down {100f * hullDown / MathF.Max(1f, engage):0}% of {engage:0} s engaging · " +
                           string.Join(" · ", b.Units.Select(u => $"{u.Name} {u.Sim.Damage.Status} {u.Stats.Hits}/{u.Stats.Shots}")));
        }
        _out.WriteLine($"overall hull-down {100f * hullDownAll / engageAll:0}% of engaging time");
        Assert.True(hullDownAll > 0.3f * engageAll, "AIs should mostly fight from behind cover in no man's land");
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
        b.Map.Blocks.Add(new Block(0f, -150f, 80f, 6f, 40f));
        var me = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Balanced);
        var enemy = b.AddUnit("B", 1, MechChassis.Medium(), new Vector3(0, 0, -AiPilot.RadarRange - 100f), 0f);
        for (int i = 0; i < 120 * 2; i++) b.Step(H);
        Assert.Empty(me.Ai!.Contacts);
        Assert.Equal(AiAction.Search, me.Ai.Action);

        // Being shot reveals the attacker.
        b.ApplyHit(me, MechSection.Torso, 1f, me.Position, enemy);
        for (int i = 0; i < 120; i++) b.Step(H);
        Assert.True(me.Ai.Contacts.ContainsKey(enemy));
        Assert.Equal(enemy, me.Ai.Target);
    }

    /// <summary>A 7 m ridge across z = -90 between an AI at the origin and a sitting target at z = -260.</summary>
    private static (BattleSim, BattleUnit Ai, BattleUnit Target) RidgeDuel()
    {
        var f = HeightField.Create(300f, 2f).AddRidge(new Vector2(-120f, -90f), new Vector2(120f, -90f), 18f, 7f, 0.8f);
        f.UpdateBounds();
        var b = new BattleSim(new ArenaMap { Terrain = f }) { TrackOutcome = false };
        var ai = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Balanced);
        var tough = MechDamage.Create(MechChassis.Medium());
        tough.Hp[(int)MechSection.Torso] = tough.MaxHp[(int)MechSection.Torso] = 1e6f; // a target that lasts the whole test
        var target = b.AddUnit("T", 1, MechChassis.Medium(), new Vector3(0, 0, -260), MathF.PI, damage: tough);
        return (b, ai, target);
    }

    [Fact]
    public void Ai_FightsHullDown_FromBehindACrest()
    {
        var (b, ai, target) = RidgeDuel();
        int engaging = 0, hullDown = 0;
        for (int i = 0; i < 120 * 60; i++)
        {
            b.Step(H);
            if (i < 120 * 20 || ai.Ai!.Action != AiAction.Engage) continue;
            engaging++;
            Vector3 eye = target.Volume(MechSection.Cockpit).Center;
            Vector3 legs = ai.Position + Vector3.UnitY * (0.45f * ai.Chassis.HipHeight);
            if (!b.Map.LineClear(eye, legs)) hullDown++;
        }
        _out.WriteLine($"engaging {engaging / 120f:0} s, hull-down {hullDown / 120f:0} s, at z = {ai.Position.Z:0}, shots {ai.Stats.Shots}, hits {ai.Stats.Hits}");
        Assert.True(engaging > 120 * 10, "should have spent the fight engaging");
        Assert.True(hullDown > engaging / 2, "should engage hull-down behind the ridge most of the time");
        Assert.True(ai.Stats.Hits >= 3, "and still be able to hit the target over the crest");
    }

    [Fact]
    public void Ai_SeesAndAimsAtTheExposedTopOfAHullDownMech()
    {
        var f = HeightField.Create(300f, 2f).AddRidge(new Vector2(-60f, -150f), new Vector2(60f, -150f), 8f, 9f, 0.8f);
        f.UpdateBounds();
        var b = new BattleSim(new ArenaMap { Terrain = f }) { TrackOutcome = false };
        var ai = b.AddUnit("A", 0, MechChassis.Medium(), Vector3.Zero, 0f, Personality.Sniper);
        var target = b.AddUnit("T", 1, MechChassis.Medium(), new Vector3(0, 0, -170), MathF.PI);
        for (int i = 0; i < 60; i++) b.Step(H);
        var torso = target.Volume(MechSection.Torso).Center;
        Assert.False(b.Map.LineClear(ai.Volume(MechSection.Cockpit).Center, torso), "setup: torso centre should be behind the crest");
        var k = Assert.Contains(target, (System.Collections.Generic.IDictionary<BattleUnit, Contact>)ai.Ai!.Contacts);
        Assert.True(k.Visible);
        Assert.True(k.AimHeight > torso.Y - target.Position.Y + 0.5f, $"aims at {k.AimHeight:0.0} m, torso centre at {torso.Y - target.Position.Y:0.0} m");
    }

    /// <summary>Shots an AI lands on a light 160 m away, standing or running a tight circle (4 seeds x 40 s).</summary>
    /// <param name="coolant">Unlimited heat sinking, so only fire discipline (not the heat budget) limits the guns.</param>
    private static (int Shots, int Hits, int Shells) ShootAtLight(bool running, ChassisClass shooter = ChassisClass.Medium, bool coolant = false)
    {
        int shots = 0, hits = 0, shellCount = 0;
        for (uint seed = 1; seed <= 4; seed++)
        {
            var (s, h, sh) = ShootAtLight(running, shooter, seed, coolant);
            shots += s; hits += h; shellCount += sh;
        }
        return (shots, hits, shellCount);
    }

    private static (int Shots, int Hits, int Shells) ShootAtLight(bool running, ChassisClass shooter, uint seed, bool coolant)
    {
        var b = new BattleSim(new ArenaMap(), seed) { TrackOutcome = false };
        var chassis = MechChassis.Create(shooter);
        if (coolant) chassis.HeatDissipation = 10f;
        var ai = b.AddUnit("A", 0, chassis, Vector3.Zero, 0f, Personality.Balanced);
        var light = MechChassis.Light();
        var tough = MechDamage.Create(light);
        tough.Hp[(int)MechSection.Torso] = tough.MaxHp[(int)MechSection.Torso] = 1e6f; // keep it alive to count hits
        var target = b.AddUnit("L", 1, light, new Vector3(0, 0, -160), 0f, damage: tough);
        target.Input = running ? new MechInput { Throttle = 1f, Turn = 1f } : default;
        int shells = 0;
        for (int i = 0; i < 120 * 40; i++)
        {
            b.Step(H);
            foreach (var e in b.Events) if (e.Kind == BattleEventKind.ShellFired) shells++;
        }
        return (ai.Stats.Shots, ai.Stats.Hits, shells);
    }

    [Fact]
    public void Evasion_ARunningLightIsMuchHarderToHit()
    {
        var still = ShootAtLight(false);
        var running = ShootAtLight(true);
        float pStill = (float)still.Hits / still.Shots, pRunning = (float)running.Hits / Math.Max(1, running.Shots);
        _out.WriteLine($"standing light: {still.Hits}/{still.Shots} hit · running light: {running.Hits}/{running.Shots} hit");
        Assert.True(pRunning < 0.5f * pStill, $"running {pRunning:P0} vs standing {pStill:P0}");
    }

    [Fact]
    public void HeavyGuns_WaitForCleanShots()
    {
        // Heat sinking unlimited: since lasers burn, the heavy laser's heat alone throttles the siege cannon against a
        // standing light (14 vs 13 shells), which hid what this checks: holding fire on a crossing target.
        var still = ShootAtLight(false, ChassisClass.Assault, coolant: true);
        var running = ShootAtLight(true, ChassisClass.Assault, coolant: true);
        _out.WriteLine($"assault vs standing light: {still.Hits}/{still.Shots} hits, {still.Shells} siege shells · vs running light: {running.Hits}/{running.Shots}, {running.Shells} shells");
        Assert.True(running.Shells < 0.7f * still.Shells, "the siege cannon should hold fire on a fast-crossing light");
    }
}
