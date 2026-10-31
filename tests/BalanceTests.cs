using System;
using System.Linq;
using System.Numerics;
using IronFeud.Logic.AI;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
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
        public float Dealt, Taken, Shots, Hits, Fast, Moving, HullDown, Alive, Samples, Games, Survived, Kills;
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
                stats[target.Chassis.Class].Taken += report.Amount;
                if (attacker == null) return;
                stats[attacker.Chassis.Class].Dealt += report.Amount;
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
                if (!u.IsOut) st.Survived++;
            }
        }
        _out.WriteLine("class   | dealt/game | taken/game | kills/game | hit % | survived | fast % | moving % | hull-down %");
        foreach (var (cls, st) in stats)
            _out.WriteLine($"{cls,-7} | {st.Dealt / st.Games,10:0} | {st.Taken / st.Games,10:0} | {st.Kills / st.Games,10:0.00} | {100 * st.Hits / Math.Max(1, st.Shots),5:0} | {100 * st.Survived / st.Games,7:0}% | {100 * st.Fast / Math.Max(1, st.Samples),5:0}% | {100 * st.Moving / Math.Max(1, st.Samples),7:0}% | {100 * st.HullDown / Math.Max(1, st.Samples),5:0}%");
        _out.WriteLine($"unfinished battles: {unfinished}/20");
        Assert.True(unfinished <= 2, $"{unfinished} lance battles didn't finish in 400 s");
    }
}
