using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using Xunit.Abstractions;

namespace IronFeud.Tests;

/// <summary>Runs only with IRONFEUD_BALANCE=1 (a few minutes of headless battles).</summary>
public sealed class BalanceFactAttribute : FactAttribute
{
    public BalanceFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IRONFEUD_BALANCE") != "1") Skip = "set IRONFEUD_BALANCE=1 to run balance reports";
    }
}

/// <summary>
/// Balance reports: AI-vs-AI medium duels over many seeds with sides swapped (spawn side alone showed no bias over 40
/// seeds per map). Prints win counts and hit rates; asserts nothing wins more than 70 % of a matchup.
/// IRONFEUD_BALANCE=1 dotnet test tests/ --filter BalanceTests --logger "console;verbosity=detailed"
/// </summary>
public class BalanceTests
{
    private readonly ITestOutputHelper _out;
    public BalanceTests(ITestOutputHelper o) => _out = o;

    private (int A, int B) Duels(string map, (Personality P, MechModel M) a, (Personality P, MechModel M) b, uint seeds = 20)
    {
        int wa = 0, wb = 0;
        float ha = 0, sa = 0, hb = 0, sb = 0;
        for (uint seed = 1; seed <= seeds; seed++)
        for (int swap = 0; swap < 2; swap++)
        {
            var battle = new BattleSim(map == "nml" ? ArenaMap.NoMansLand(seed) : ArenaMap.Arena(seed), seed * 31 + (uint)swap);
            var first = swap == 0 ? a : b;
            var second = swap == 0 ? b : a;
            var u0 = battle.AddUnit("A", 0, MechChassis.Medium(first.M), new Vector3(0, 0, ArenaMap.ArenaSpawnZ), 0f, first.P);
            var u1 = battle.AddUnit("B", 1, MechChassis.Medium(second.M), new Vector3(0, 0, -ArenaMap.ArenaSpawnZ), MathF.PI, second.P);
            for (int i = 0; i < 300 * 120 && !battle.Over; i++) battle.Step(1f / 120f);
            var ua = swap == 0 ? u0 : u1;
            var ub = swap == 0 ? u1 : u0;
            ha += ua.Stats.Hits; sa += ua.Stats.Shots; hb += ub.Stats.Hits; sb += ub.Stats.Shots;
            if (battle.Over && battle.Winner == ua.Team) wa++;
            else if (battle.Over && battle.Winner == ub.Team) wb++;
        }
        _out.WriteLine($"{map,-5} {a.P.Name}/{a.M} {wa} - {wb} {b.P.Name}/{b.M} · hit {100 * ha / sa:0}% vs {100 * hb / sb:0}%");
        int n = wa + wb;
        Assert.True(wa < 0.7f * n && wb < 0.7f * n, $"{map}: {a.P.Name}/{a.M} vs {b.P.Name}/{b.M} is lopsided ({wa}-{wb})");
        return (wa, wb);
    }

    [BalanceFact]
    public void Personalities_AreBalanced()
    {
        foreach (string map in new[] { "nml", "hills" })
        {
            Duels(map, (Personality.Brawler, MechModel.Hauberk), (Personality.Balanced, MechModel.Hauberk));
            Duels(map, (Personality.Brawler, MechModel.Hauberk), (Personality.Sniper, MechModel.Hauberk));
            Duels(map, (Personality.Balanced, MechModel.Hauberk), (Personality.Sniper, MechModel.Hauberk));
        }
    }

    [BalanceFact]
    public void Models_AreBalanced()
    {
        foreach (string map in new[] { "nml", "hills" })
            Duels(map, (Personality.Balanced, MechModel.Hauberk), (Personality.Balanced, MechModel.Sicklefoot));
        Duels("nml", (Personality.Brawler, MechModel.Hauberk), (Personality.Brawler, MechModel.Sicklefoot));
        Duels("nml", (Personality.Balanced, MechModel.Hauberk), (Personality.Brawler, MechModel.Sicklefoot)); // watch 1v1 as shipped
    }

    private sealed class RoleStats
    {
        public float Dealt, Taken, Shots, Hits, Fast, Moving, HullDown, Alive, Samples, Games, Survived, Kills, RearDealt, RearTaken, Heat, Shutdowns;
    }

    /// <summary>
    /// Role report: lance battles (one of each class per side, models mirrored) on both maps. Classes are not meant to
    /// be equal: lights should be hard to hit and fast, assaults hit hardest. Asserts only that the battles end.
    /// </summary>
    [BalanceFact]
    public void Lances_ShowTheClassRoles()
    {
        var stats = Enum.GetValues<ChassisClass>().ToDictionary(c => c, _ => new RoleStats());
        var personas = new[] { Personality.Balanced, Personality.Balanced, Personality.Balanced, Personality.Sniper };
        int unfinished = 0;
        foreach (string map in new[] { "nml", "hills" })
        for (uint seed = 1; seed <= 10; seed++)
        {
            var b = new BattleSim(map == "nml" ? ArenaMap.NoMansLand(seed) : ArenaMap.Arena(seed), seed * 13);
            for (int team = 0; team < 2; team++)
            {
                float sign = team == 0 ? 1f : -1f;
                for (int i = 0; i < 4; i++)
                {
                    var model = (i + team) % 2 == 0 ? MechModel.Sicklefoot : MechModel.Hauberk;
                    b.AddUnit($"{team}-{i}", team, MechChassis.Create((ChassisClass)i, model),
                        new Vector3((-45f + 30f * i) * sign, 0, sign * ArenaMap.ArenaSpawnZ), team == 0 ? 0f : MathF.PI, personas[i]);
                }
            }
            b.Damaged += (target, report, _, attacker) =>
            {
                bool rear = report.Facing == Logic.Mech.Damage.Facing.Rear;
                stats[target.Chassis.Class].Taken += report.Amount;
                if (rear) stats[target.Chassis.Class].RearTaken += report.Amount;
                if (attacker == null) return;
                stats[attacker.Chassis.Class].Dealt += report.Amount;
                if (rear) stats[attacker.Chassis.Class].RearDealt += report.Amount;
                if (report.StatusAfter != Logic.Mech.Damage.MechStatus.Operational && report.SectionDestroyed) stats[attacker.Chassis.Class].Kills++;
            };
            for (int i = 0; i < 400 * 120 && !b.Over; i++)
            {
                b.Step(1f / 120f);
                if (i % 24 != 0) continue;
                foreach (var u in b.Units)
                {
                    if (u.IsOut) continue;
                    var st = stats[u.Chassis.Class];
                    st.Samples++;
                    if (MathF.Abs(u.Sim.Locomotion.Body.V) > 0.6f * u.Chassis.VMax) st.Fast++;
                    if (MathF.Abs(u.Sim.Locomotion.Body.V) > 0.25f * u.Chassis.VMax) st.Moving++;
                    if (u.Ai!.Target is { IsOut: false } t)
                    {
                        var legs = u.Position + Vector3.UnitY * (0.45f * u.Chassis.HipHeight);
                        if (!b.Map.LineClear(t.Volume(Logic.Mech.Damage.MechSection.Cockpit).Center, legs)) st.HullDown++;
                    }
                }
            }
            if (!b.Over) unfinished++;
            foreach (var u in b.Units)
            {
                var st = stats[u.Chassis.Class];
                st.Games++;
                st.Shots += u.Stats.Shots;
                st.Hits += u.Stats.Hits;
                st.Heat += u.Stats.HeatInflicted;
                st.Shutdowns += u.Stats.ShutdownsCaused;
                if (!u.IsOut) st.Survived++;
            }
        }
        _out.WriteLine("class   | dealt/game | taken/game | kills/game | hit % | survived | fast % | moving % | hull-down % | rear dealt % | rear taken % | heat in/game | shutdowns/game");
        foreach (var (cls, st) in stats)
            _out.WriteLine($"{cls,-7} | {st.Dealt / st.Games,10:0} | {st.Taken / st.Games,10:0} | {st.Kills / st.Games,10:0.00} | {100 * st.Hits / Math.Max(1, st.Shots),5:0} | {100 * st.Survived / st.Games,7:0}% | {100 * st.Fast / Math.Max(1, st.Samples),5:0}% | {100 * st.Moving / Math.Max(1, st.Samples),7:0}% | {100 * st.HullDown / Math.Max(1, st.Samples),5:0}%    | {100 * st.RearDealt / Math.Max(1, st.Dealt),8:0}%    | {100 * st.RearTaken / Math.Max(1, st.Taken),8:0}%    | {st.Heat / st.Games,8:0.00}    | {st.Shutdowns / st.Games,8:0.00}");
        _out.WriteLine($"unfinished battles: {unfinished}/20");
        Assert.True(unfinished <= 2, $"{unfinished} lance battles didn't finish in 400 s");
    }

    /// <summary>Lone light, or light + medium, vs an assault on the cluttered arena, with a set of jump tactics.</summary>
    private string LightsVsAssault(bool withMedium, JumpTactics tactics, bool escorts = true)
    {
        float dealt = 0, rear = 0, taken = 0;
        int lost = 0, kills = 0;
        for (uint seed = 1; seed <= 12; seed++)
        {
            var b = new BattleSim(ArenaMap.Arena(seed), seed) { TrackOutcome = false, JumpTactics = tactics, Escorts = escorts };
            var light = b.AddUnit("L", 0, MechChassis.Light(), new Vector3(-20, 0, 150), 0f, Personality.Balanced);
            if (withMedium) b.AddUnit("M", 0, MechChassis.Medium(), new Vector3(20, 0, 150), 0f, Personality.Balanced);
            var a = b.AddUnit("A", 1, MechChassis.Assault(), new Vector3(0, 0, -150), MathF.PI, Personality.Balanced);
            b.Damaged += (t, r, _, _) => { if (t == light) taken += r.Amount; };
            for (int i = 0; i < 90 * 120 && !light.IsOut && !a.IsOut; i++) b.Step(1f / 120f);
            dealt += light.Stats.DamageDealt;
            rear += light.Stats.RearDamage;
            if (light.IsOut) lost++;
            if (a.IsOut) kills++;
        }
        return $"light dealt {dealt,5:0} ({100 * rear / Math.Max(1, dealt),2:0} % rear), took {taken,5:0}, lights lost {lost,2}/12, assault killed {kills,2}/12";
    }

    /// <summary>
    /// What each jump tactic is worth, switched on alone and all together (12 seeds each). Measured when added: all
    /// tactics vs none, lone light: lights lost 9 -> 6, assault killed 3 -> 5 (vault alone 7 and 4; holding fire
    /// while lined up made it 10 and 2, so it doesn't); light + medium: 1 -> 0, 11 -> 12 (dodge does most of it).
    /// Pop-ups, medium vs heavy on No Man's Land: medium damage 1714 -> 2154, mediums lost 10 -> 8, heavies killed 2 -> 4.
    /// </summary>
    [BalanceFact]
    public void JumpTactics_WhatEachIsWorth()
    {
        foreach (bool withMedium in new[] { false, true })
            foreach (var t in new[] { JumpTactics.None, JumpTactics.PopUp, JumpTactics.Vault, JumpTactics.Traverse, JumpTactics.Dodge, JumpTactics.All })
                _out.WriteLine($"{(withMedium ? "light + medium" : "light alone   ")} vs assault, {t,-8}: {LightsVsAssault(withMedium, t)}");

        foreach (var t in new[] { JumpTactics.None, JumpTactics.PopUp })
        {
            float dealt = 0;
            int lost = 0, kills = 0;
            for (uint seed = 1; seed <= 12; seed++)
            {
                var b = new BattleSim(ArenaMap.NoMansLand(seed), seed) { TrackOutcome = false, JumpTactics = t };
                var m = b.AddUnit("M", 0, MechChassis.Medium(), new Vector3(0, 0, ArenaMap.ArenaSpawnZ), 0f, Personality.Balanced);
                var h = b.AddUnit("H", 1, MechChassis.Heavy(), new Vector3(0, 0, -ArenaMap.ArenaSpawnZ), MathF.PI, Personality.Balanced);
                for (int i = 0; i < 150 * 120 && !m.IsOut && !h.IsOut; i++) b.Step(1f / 120f);
                dealt += m.Stats.DamageDealt;
                if (m.IsOut) lost++;
                if (h.IsOut) kills++;
            }
            _out.WriteLine($"medium vs heavy, No Man's Land, {t,-6}: medium dealt {dealt:0}, mediums lost {lost}/12, heavies killed {kills}/12");
        }
    }

    /// <summary>
    /// Assault + medium vs two lights with and without escorts. Its value has flipped with every system around it
    /// (leashed, wobbling guns: lights lost 2 -> 8 of 12; stabilised guns: 7 -> 2 of 24, so it went on call;
    /// on call: 7 -> 9; with jump tactics: about even). Revisit with the heavies' role.
    /// </summary>
    [BalanceFact]
    public void Escorts_WhatTheyAreWorth()
    {
        foreach (bool escorts in new[] { false, true })
        {
            float rear = 0f, taken = 0f, lightsTaken = 0f;
            int lost = 0;
            for (uint seed = 1; seed <= 12; seed++)
            {
                var b = new BattleSim(ArenaMap.Arena(seed), seed) { TrackOutcome = false, Escorts = escorts };
                var assault = b.AddUnit("A", 0, MechChassis.Assault(), new Vector3(-15, 0, 150), 0f, Personality.Balanced);
                b.AddUnit("M", 0, MechChassis.Medium(), new Vector3(15, 0, 150), 0f, Personality.Balanced);
                var l1 = b.AddUnit("L1", 1, MechChassis.Light(), new Vector3(-30, 0, -150), MathF.PI, Personality.Balanced);
                var l2 = b.AddUnit("L2", 1, MechChassis.Light(), new Vector3(30, 0, -150), MathF.PI, Personality.Balanced);
                b.Damaged += (target, report, _, _) =>
                {
                    if (target == assault)
                    {
                        taken += report.Amount;
                        if (report.Facing == Facing.Rear) rear += report.Amount;
                    }
                    else if (target == l1 || target == l2) lightsTaken += report.Amount;
                };
                for (int i = 0; i < 90 * 120 && !assault.IsOut && !(l1.IsOut && l2.IsOut); i++) b.Step(1f / 120f);
                lost += (l1.IsOut ? 1 : 0) + (l2.IsOut ? 1 : 0);
            }
            _out.WriteLine($"{(escorts ? "escort   " : "no escort")}: assault took {taken:0} ({rear:0} in the rear), lights took {lightsTaken:0}, lost {lost}/24");
        }
    }

    private static BattleSim Lance(uint seed, string map)
    {
        var personas = new[] { Personality.Balanced, Personality.Balanced, Personality.Balanced, Personality.Sniper };
        var b = new BattleSim(map == "nml" ? ArenaMap.NoMansLand(seed) : ArenaMap.Arena(seed), seed * 13);
        for (int team = 0; team < 2; team++)
        {
            float sign = team == 0 ? 1f : -1f;
            for (int i = 0; i < 4; i++)
            {
                var model = (i + team) % 2 == 0 ? MechModel.Sicklefoot : MechModel.Hauberk;
                b.AddUnit($"{team}-{i}", team, MechChassis.Create((ChassisClass)i, model),
                    new Vector3((-45f + 30f * i) * sign, 0, sign * ArenaMap.ArenaSpawnZ), team == 0 ? 0f : MathF.PI, personas[i]);
            }
        }
        return b;
    }

    /// <summary>
    /// How the AI behaves in lance games (10 seeds each on No Man's Land and the arena): does a mech hit from behind
    /// turn on its attacker, how often do jump-capable mechs use their jets, how much do they stand still, and how
    /// often do pilots call for support. Player feedback behind it: "the AI doesn't react well to lights flanking from
    /// behind", "they don't use the jump jets tactically enough", "popping makes them stay in place". Measured before
    /// the fixes: 52 % turned within 15 s (3.6 s), lights 0.9 jumps/min, mediums still 26 %, pop-ups 2.4/min;
    /// after: 87 % (2.0 s), lights ~6.5/min, mediums 21 %, pop-ups 1.8/min with a relocation after each.
    /// </summary>
    [BalanceFact]
    public void Behaviour_ReactionsJumpsAndMovement()
    {
        var jumps = new Dictionary<(ChassisClass, JumpPlan), int>();
        var alive = new Dictionary<ChassisClass, float>(); var still = new Dictionary<ChassisClass, float>();
        var longStops = new Dictionary<ChassisClass, int>();
        int rearEvents = 0, faced = 0; float faceTime = 0; int retargeted = 0, helpCalls = 0, helpAnswered = 0;
        float retreatT = 0, retreatMovingT = 0, pressT = 0, aiT = 0;
        foreach (string map in new[] { "nml", "hills" })
        for (uint seed = 1; seed <= 10; seed++)
        {
            var b = Lance(seed, map);
            var lastJump = b.Units.ToDictionary(u => u, _ => JumpPlan.None);
            var called = b.Units.ToDictionary(u => u, _ => false);
            var stillFor = b.Units.ToDictionary(u => u, _ => 0f);
            // rear hit bookkeeping: victim -> (attacker, time)
            var pending = new Dictionary<BattleUnit, (BattleUnit Att, float T)>();
            b.Damaged += (t, r, _, att) =>
            {
                if (r.Facing != Facing.Rear || att == null || t.IsOut || pending.ContainsKey(t)) return;
                pending[t] = (att, b.Time); rearEvents++;
            };
            for (int i = 0; i < 300 * 120 && !b.Over; i++)
            {
                b.Step(1f / 120f);
                foreach (var u in b.Units)
                {
                    if (u.IsOut || u.Ai == null) continue;
                    var c = u.Chassis.Class;
                    alive[c] = alive.GetValueOrDefault(c) + 1f / 120f;
                    if (MathF.Abs(u.Sim.Locomotion.Body.V) < 1f && !u.Sim.Locomotion.Body.Airborne)
                    {
                        still[c] = still.GetValueOrDefault(c) + 1f / 120f;
                        stillFor[u] += 1f / 120f;
                        if (MathF.Abs(stillFor[u] - 10f) < 0.5f / 120f) longStops[c] = longStops.GetValueOrDefault(c) + 1;
                    }
                    else stillFor[u] = 0f;
                    var j = u.Ai.Jump;
                    if (j != JumpPlan.None && lastJump[u] == JumpPlan.None) jumps[(c, j)] = jumps.GetValueOrDefault((c, j)) + 1;
                    lastJump[u] = j;
                    aiT += 1f / 120f;
                    if (u.Ai.Pressing) pressT += 1f / 120f;
                    if (u.Ai.Action == AiAction.Retreat)
                    {
                        retreatT += 1f / 120f;
                        if (MathF.Abs(u.Sim.Locomotion.Body.V) > 0.25f * u.Chassis.VMax) retreatMovingT += 1f / 120f;
                    }
                    bool calling = u.Ai.HelpWanted != null;
                    if (calling && !called[u]) { helpCalls++; }
                    called[u] = calling;
                    if (calling && b.Units.Any(x => x.Ai?.Answering == u)) helpAnswered++;
                }
                foreach (var (v, (att, t0)) in pending.ToList())
                {
                    if (v.IsOut || att.IsOut) { pending.Remove(v); continue; }
                    float off = MathF.Abs(MathUtil.WrapAngle(MathUtil.Heading(att.Position - v.Position) - MathUtil.Heading(v.Sim.TorsoFacing)));
                    if (v.Ai?.Target == att && b.Time - t0 < 30) { }
                    if (off < 0.5f) { faced++; faceTime += b.Time - t0; if (v.Ai?.Target == att) retargeted++; pending.Remove(v); }
                    else if (b.Time - t0 > 15f) pending.Remove(v);
                }
            }
        }
                _out.WriteLine($"support calls {helpCalls}, ticks answered {helpAnswered / 120f:0} s");
        _out.WriteLine($"retreating {100 * retreatT / aiT:0}% of the time, moving {100 * retreatMovingT / Math.Max(1e-3f, retreatT):0}% of it; pressing an advantage {100 * pressT / aiT:0}% of the time");
        _out.WriteLine($"rear-hit events {rearEvents}: victim turned to face the attacker within 15 s in {faced} ({100f * faced / Math.Max(1, rearEvents):0}%), mean {faceTime / Math.Max(1, faced):0.0} s; targeting it then: {retargeted}");
        foreach (var c in Enum.GetValues<ChassisClass>())
        {
            float mins = alive.GetValueOrDefault(c) / 60f;
            string js = string.Join(", ", Enum.GetValues<JumpPlan>().Where(p => p != JumpPlan.None).Select(p => $"{p} {jumps.GetValueOrDefault((c, p)) / Math.Max(0.01f, mins):0.00}"));
            _out.WriteLine($"{c,-7}: {mins:0} min alive, standing still {100 * still.GetValueOrDefault(c) / Math.Max(1e-3f, alive.GetValueOrDefault(c)):0}%, stops >10 s {longStops.GetValueOrDefault(c)}; jumps/min: {js}");
        }
        Assert.True(faced >= 0.75f * rearEvents, "mechs hit from behind should turn on the attacker");
        float lightMins = alive.GetValueOrDefault(ChassisClass.Light) / 60f;
        int lightJumps = jumps.Where(kv => kv.Key.Item1 == ChassisClass.Light).Sum(kv => kv.Value);
        Assert.True(lightJumps >= 3f * lightMins, $"lights jump {lightJumps / lightMins:0.0}/min");
        Assert.True(still.GetValueOrDefault(ChassisClass.Medium) < 0.25f * alive.GetValueOrDefault(ChassisClass.Medium), "mediums shouldn't camp");
    }

    private static BattleSim TacticsLance(uint seed, string map)
    {
        var personas = new[] { Personality.Balanced, Personality.Balanced, Personality.Balanced, Personality.Sniper };
        var b = new BattleSim(map == "nml" ? ArenaMap.NoMansLand(seed) : ArenaMap.Arena(seed), seed * 13);
        for (int team = 0; team < 2; team++)
        {
            float sign = team == 0 ? 1f : -1f;
            for (int i = 0; i < 4; i++)
                b.AddUnit($"{team}-{i}", team, MechChassis.Create((ChassisClass)i, i % 2 == 0 ? MechModel.Sicklefoot : MechModel.Hauberk),
                    new Vector3((-45f + 30f * i) * sign, 0, sign * ArenaMap.ArenaSpawnZ), team == 0 ? 0f : MathF.PI, personas[i]);
        }
        return b;
    }
    /// <summary>
    /// A lance using each team tactic against an identical lance without, 24 seeds x 2 sides on both maps. Measured
    /// when added: all 29-19, radar alone 29-19, radar + spotters 27-21, umbrella alone 24-24 (spotters and the
    /// umbrella read as teamwork but don't move win rates yet: lights spotted 9 % of the time).
    /// </summary>
    [BalanceFact]
    public void TeamTactics_HeadToHead()
    {
        foreach (var (t, rival) in new[]
                 {
                     (TeamTactics.Radar, TeamTactics.None), (TeamTactics.Spotters | TeamTactics.Radar, TeamTactics.None),
                     (TeamTactics.Umbrella, TeamTactics.None), (TeamTactics.All, TeamTactics.None),
                     (TeamTactics.All, TeamTactics.All & ~TeamTactics.Marks),
                 })
        {
            int wins = 0, losses = 0, games = 0; float lockT = 0, heavyT = 0, spotT = 0, lightT = 0, arcDmg = 0; int salvos = 0, umbrellas = 0;
            foreach (string map in new[] { "nml", "hills" })
            for (uint seed = 1; seed <= 12; seed++)
            foreach (int smart in new[] { 0, 1 })
            {
                var b = TacticsLance(seed, map);
                b.Tactics[smart] = t;
                b.Tactics[1 - smart] = rival;
                b.Damaged += (tg, r, _, att) => { };
                var umbrellaWas = b.Units.ToDictionary(u => u, _ => false);
                for (int i = 0; i < 300 * 120 && !b.Over; i++)
                {
                    b.Step(1f / 120f);
                    foreach (var e in b.Events)
                    {
                        if (e.Shooter?.Team != smart) continue;
                        if (e.Kind == BattleEventKind.Impact && e.Shot.Spec.Guided && e.Target != null) arcDmg += e.Report.Amount;
                        if (e.Kind == BattleEventKind.MissileFired && e.Shot.Spec.Guided && e.Shooter.Sim.Weapons.Mounts[2].Cooldown > e.Shot.Spec.Cooldown - 0.05f) salvos++;
                    }
                    foreach (var u in b.Units)
                    {
                        if (u.IsOut || u.Team != smart) continue;
                        if (u.Chassis.Class == ChassisClass.Heavy) { heavyT++; if (u.Locked != null) lockT++; }
                        if (u.Chassis.Class == ChassisClass.Light) { lightT++; if (u.Ai!.Spotting) spotT++; }
                        bool um = u.Ai!.Umbrella != null;
                        if (um && !umbrellaWas[u]) umbrellas++;
                        umbrellaWas[u] = um;
                    }
                }
                games++;
                if (b.Over && b.Winner == smart) wins++; else if (b.Over && b.Winner == 1 - smart) losses++;
            }
            _out.WriteLine($"{t,-24} vs {rival}: won {wins}, lost {losses} of {games}; smart heavies locked {100 * lockT / Math.Max(1, heavyT):0}%, {salvos} salvos, arc dmg {arcDmg:0}; lights spotting {100 * spotT / Math.Max(1, lightT):0}%; umbrella moves {umbrellas}");
        }
    }

    /// <summary>
    /// A lance with designator marks against the same lance without, for each light model (24 seeds x 2 sides, both
    /// maps). Measured when added: Hauberk (ranged) light 31-17, light survived 46 %; Sicklefoot (flamer) 24-24;
    /// enemies marked ~13 % of the time, a third of the damage landing on marked targets.
    /// </summary>
    [BalanceFact]
    public void Marks_WhatTheyAreWorth()
    {
        foreach (var lightModel in new[] { MechModel.Hauberk, MechModel.Sicklefoot })
        {
            int wins = 0, losses = 0; float markedT = 0, enemyT = 0, lightDealt = 0, lightSurv = 0, arc = 0, lightOn = 0, games = 0, markedHits = 0, hits = 0;
            foreach (string map in new[] { "nml", "hills" })
            for (uint seed = 1; seed <= 12; seed++)
            foreach (int smart in new[] { 0, 1 })
            {
                var personas = new[] { Personality.Balanced, Personality.Balanced, Personality.Balanced, Personality.Sniper };
                var b = new BattleSim(map == "nml" ? ArenaMap.NoMansLand(seed) : ArenaMap.Arena(seed), seed * 13);
                for (int team = 0; team < 2; team++)
                {
                    float sign = team == 0 ? 1f : -1f;
                    for (int i = 0; i < 4; i++)
                        b.AddUnit($"{team}-{i}", team, MechChassis.Create((ChassisClass)i, i == 0 ? lightModel : i % 2 == 0 ? MechModel.Sicklefoot : MechModel.Hauberk),
                            new Vector3((-45f + 30f * i) * sign, 0, sign * ArenaMap.ArenaSpawnZ), team == 0 ? 0f : MathF.PI, personas[i]);
                }
                b.Tactics[smart] = TeamTactics.All;
                b.Tactics[1 - smart] = TeamTactics.All & ~TeamTactics.Marks;
                for (int i = 0; i < 300 * 120 && !b.Over; i++)
                {
                    b.Step(1f / 120f);
                    foreach (var e in b.Events)
                        if (e.Kind == BattleEventKind.Impact && e.Shooter?.Team == smart && e.Target != null && e.Report.Amount > 0f)
                        { hits += e.Report.Amount; if (b.Marked(smart, e.Target)) markedHits += e.Report.Amount; }
                    if (i % 12 == 0)
                        foreach (var u in b.Units)
                            if (u.Team != smart && !u.IsOut) { enemyT++; if (b.Marked(smart, u)) markedT++; }
                }
                games++;
                if (b.Over && b.Winner == smart) wins++; else if (b.Over && b.Winner == 1 - smart) losses++;
                var l = b.Units.First(u => u.Team == smart && u.Chassis.Class == ChassisClass.Light);
                lightDealt += l.Stats.DamageDealt; if (!l.IsOut) lightSurv++;
            }
            _out.WriteLine($"{lightModel} light, marks vs none: won {wins} lost {losses} of {games}; enemies marked {100 * markedT / enemyT:0}% of the time; {100 * markedHits / Math.Max(1, hits):0}% of damage on marked; light dealt {lightDealt / games:0}, survived {100 * lightSurv / games:0}%");
        }
    }

    /// <summary>
    /// The default lance (Sicklefoot light: flamer brawler with ECM) against the same lance with its light's jammer
    /// switched off, 24 seeds x 2 sides on both maps.
    /// </summary>
    [BalanceFact]
    public void Ecm_WhatItIsWorth()
    {
        int wins = 0, losses = 0, games = 0;
        float jammedT = 0, heavyT = 0, arcFired = 0, arcHit = 0, lightDealt = 0, lightSurv = 0;
        foreach (string map in new[] { "nml", "hills" })
        for (uint seed = 1; seed <= 12; seed++)
        foreach (int smart in new[] { 0, 1 })
        {
            var b = TacticsLance(seed, map);
            foreach (var u in b.Units) if (u.Team != smart) u.Chassis.Ecm = false;
            for (int i = 0; i < 300 * 120 && !b.Over; i++)
            {
                b.Step(1f / 120f);
                foreach (var e in b.Events)
                {
                    if (e.Shooter?.Team == smart || e.Shot.Spec?.Guided != true) continue;
                    if (e.Kind == BattleEventKind.MissileFired) arcFired++;
                    if (e.Kind == BattleEventKind.Impact && e.Target != null && e.Report.Amount > 0f) arcHit++;
                }
                if (i % 12 == 0)
                    foreach (var u in b.Units)
                        if (u.Team != smart && !u.IsOut && u.Chassis.Class == ChassisClass.Heavy) { heavyT++; if (b.Jammed(u.Team, u)) jammedT++; }
            }
            games++;
            if (b.Over && b.Winner == smart) wins++; else if (b.Over && b.Winner == 1 - smart) losses++;
            var l = b.Units.First(u => u.Team == smart && u.Chassis.Class == ChassisClass.Light);
            lightDealt += l.Stats.DamageDealt; if (!l.IsOut) lightSurv++;
        }
        _out.WriteLine($"ECM vs none: won {wins} lost {losses} of {games}; enemy heavies jammed {100 * jammedT / Math.Max(1, heavyT):0}% of the time; "
                       + $"their arc rounds hitting {100 * arcHit / Math.Max(1, arcFired):0}%; ECM light dealt {lightDealt / games:0}, survived {100 * lightSurv / games:0}%");
    }
}
