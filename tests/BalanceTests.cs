using System;
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
}
