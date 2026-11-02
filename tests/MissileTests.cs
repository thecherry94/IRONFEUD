using System;
using System.Numerics;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using Vector2 = System.Numerics.Vector2;
using Xunit.Abstractions;

namespace IronFeud.Tests;

public class MissileTests
{
    private const float H = 1f / 120f;
    private const uint Rack = 1u << 2; // the shoulder rack is the third mount
    private readonly ITestOutputHelper _out;

    public MissileTests(ITestOutputHelper output) => _out = output;

    private static MechDamage Tough(MechChassis c)
    {
        var d = MechDamage.Create(c);
        foreach (var s in Enum.GetValues<MechSection>()) d.Set(s, d.MaxArmor[(int)s], 1e6f);
        return d;
    }

    private static float Taken(BattleUnit u)
    {
        float sum = 0f;
        foreach (var s in Enum.GetValues<MechSection>()) sum += u.Sim.Damage.Max(s) - u.Sim.Damage[s];
        return sum;
    }

    /// <summary>The shooter tracks the target's torso and pulls the rack trigger once at 1 s; returns the target's damage.</summary>
    private float Salvo(BattleSim b, BattleUnit shooter, BattleUnit target, float seconds = 6f)
    {
        float twist = 0f, aim = 0f;
        for (int i = 0; i < (int)(120 * seconds); i++)
        {
            AimSolver.Track(shooter.Sim.Pose, shooter.Sim.Locomotion.Body, shooter.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            shooter.LockCandidate = target;
            shooter.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = shooter.Locked != null || i == 120 ? Rack : 0u,
                Convergence = Vector3.Distance(shooter.Position, target.Position) };
            b.Step(H);
            if (shooter.Stats.Shots > 0 && i > 120) shooter.Input = default;
        }
        return Taken(target);
    }

    [Fact]
    public void Light_HasARocketPod_Medium_AStrikeRack_Heavy_ArcMissiles()
    {
        Assert.Equal(WeaponSpec.RocketPod, new MechSim(MechChassis.Light()).Weapons.Mounts[2].Spec);
        Assert.Equal(WeaponSpec.StrikeRack, new MechSim(MechChassis.Medium()).Weapons.Mounts[2].Spec);
        Assert.Equal(WeaponSpec.ArcMissiles, new MechSim(MechChassis.Heavy()).Weapons.Mounts[2].Spec);
        Assert.Equal(2, new MechSim(MechChassis.Assault()).Weapons.Mounts.Count);
        Assert.Equal(1, MechChassis.Assault().PointDefense);
    }

    [Theory]
    [InlineData(ChassisClass.Light, 80f)]
    [InlineData(ChassisClass.Medium, 250f)]
    public void UnguidedSalvos_HitWhatTheyAreAimedAt(ChassisClass shooterClass, float distance)
    {
        var b = new BattleSim(new ArenaMap { Radius = 600f }) { TrackOutcome = false };
        var shooter = b.AddUnit("S", 0, MechChassis.Create(shooterClass), new Vector3(0, 0, distance), 0f);
        var targetChassis = MechChassis.Heavy();
        var target = b.AddUnit("T", 1, targetChassis, Vector3.Zero, MathF.PI, damage: Tough(targetChassis));
        float taken = Salvo(b, shooter, target, 4f);
        var spec = shooter.Sim.Weapons.Mounts[2].Spec;
        _out.WriteLine($"{spec.Name} at {distance} m: {shooter.Stats.Shots} rounds, {shooter.Stats.Hits} hits, {taken:0.0} of {spec.DamageAt(distance):0.0}");
        Assert.Equal(spec.Pulses, shooter.Stats.Shots);
        Assert.True(taken > 0.6f * spec.DamageAt(distance), $"only {taken:0.0} damage");
    }

    /// <summary>
    /// A heavy behind a 20 m berm fires arc missiles at a target it can't see, 420 m away. With a teammate spotting the
    /// target it locks, and the missiles climb over the berm and dive onto it; with nobody spotting there is no lock
    /// and the salvo flies blind.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ArcMissiles_NeedASpotter_AndFlyOverCover(bool spotter)
    {
        var f = new HeightField(301, 4f).AddRidge(new Vector2(-400f, 180f), new Vector2(400f, 180f), 20f, 20f, 1.2f);
        f.UpdateBounds();
        var b = new BattleSim(new ArenaMap { Terrain = f, Radius = 580f }) { TrackOutcome = false };
        var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 220), 0f);
        if (spotter) b.AddUnit("L", 0, MechChassis.Light(), new Vector3(150, 0, 20), 0f);
        var targetChassis = MechChassis.Medium(MechModel.Sicklefoot); // no point defense
        var target = b.AddUnit("T", 1, targetChassis, new Vector3(0, 0, -200), MathF.PI, damage: Tough(targetChassis));
        b.Step(H);
        Assert.False(b.Map.LineClear(heavy.Volume(MechSection.Cockpit).Center, target.Volume(MechSection.Torso).Center), "the heavy can't see it");
        Salvo(b, heavy, target, 10f);
        float taken = Taken(target);
        _out.WriteLine($"spotter {spotter}: locked {heavy.Locked != null}, {heavy.Stats.Hits}/{heavy.Stats.Shots} hits, {taken:0.0} damage");
        if (spotter) Assert.True(taken > 0.4f * WeaponSpec.ArcMissiles.Damage, $"only {taken:0.0} damage");
        else Assert.Equal(0f, taken);
    }

    /// <summary>
    /// A light marks the target, then ducks out of sight behind the berm. The mark holds 6 s: the heavy behind the
    /// berm still locks (in a third of the usual time), then it lapses. Without a designator, the target is unspotted
    /// the moment the light is gone.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMark_OutlastsTheLightsSight_AndSpeedsUpLocks(bool designator)
    {
        var f = new HeightField(301, 4f).AddRidge(new Vector2(-400f, 180f), new Vector2(400f, 180f), 20f, 20f, 1.2f);
        f.UpdateBounds();
        var b = new BattleSim(new ArenaMap { Terrain = f, Radius = 580f }) { TrackOutcome = false };
        var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 220), 0f);
        var lightChassis = MechChassis.Light();
        lightChassis.Designator = designator;
        var light = b.AddUnit("L", 0, lightChassis, new Vector3(150, 0, 20), 0f);
        var target = b.AddUnit("T", 1, MechChassis.Medium(MechModel.Sicklefoot), new Vector3(0, 0, -150), MathF.PI);
        float twist = 0f, aim = 0f;
        for (int i = 0; i < 120 * 2.5f; i++)
        {
            AimSolver.Track(light.Sim.Pose, light.Sim.Locomotion.Body, light.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            light.Input = new MechInput { TwistTarget = twist, AimTarget = aim };
            light.LockCandidate = target;
            b.Step(H);
        }
        Assert.NotNull(light.Locked);
        Assert.Equal(designator, b.Marked(0, target));

        // The light ducks behind the berm next to the heavy: nobody on its side sees the target now.
        light.LockCandidate = null;
        light.Sim.Locomotion.Reset(new Vector3(40, 0, 230), 0f);
        b.Step(H);
        Assert.False(b.Sees(light, target));
        Assert.Equal(designator, b.Spotted(0, target));

        float lockedAt = -1f, start = b.Time;
        twist = aim = 0f;
        for (int i = 0; i < 120 * 7; i++)
        {
            AimSolver.Track(heavy.Sim.Pose, heavy.Sim.Locomotion.Body, heavy.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            heavy.Input = new MechInput { TwistTarget = twist, AimTarget = aim };
            heavy.LockCandidate = target;
            b.Step(H);
            if (lockedAt < 0f && heavy.Locked != null) lockedAt = b.Time - start;
        }
        _out.WriteLine($"designator {designator}: heavy locked through the berm after {lockedAt:0.00} s; marked 7 s later: {b.Marked(0, target)}");
        if (designator) Assert.InRange(lockedAt, 0.3f, 1.2f); // a third of the arc missiles' 1.5 s, plus slewing on
        else Assert.Equal(-1f, lockedAt);
        Assert.False(b.Marked(0, target), "the mark lapses 6 s after the light last saw the target");
        Assert.False(b.Spotted(0, target));
    }

    /// <summary>
    /// An enemy ECM light next to the target slows the heavy's lock ~2.5x (it used to block it outright: ECM that blocks
    /// became mandatory in the genre and was walked back to lock delays); a designator's mark cuts through it.
    /// </summary>
    [Fact]
    public void Ecm_SlowsLocks_ADesignatorsMarkCutsThrough()
    {
        float clear = LockTime(jammer: false, designator: false), jammed = LockTime(jammer: true, designator: false);
        float marked = LockTime(jammer: true, designator: true);
        _out.WriteLine($"heavy's lock: {clear:0.00} s clear, {jammed:0.00} s jammed, {marked:0.00} s jammed with a designator marking");
        Assert.InRange(jammed / clear, 2.2f, 2.8f);
        // The designator needs its own lock first, then the heavy's runs at a third: about the unjammed time in all.
        Assert.True(marked < 0.6f * jammed, "the mark burns through the jamming");
    }

    private float LockTime(bool jammer, bool designator)
    {
        var b = new BattleSim(new ArenaMap { Radius = 700f }) { TrackOutcome = false };
        var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 450), 0f);
        var target = b.AddUnit("T", 1, MechChassis.Medium(MechModel.Sicklefoot), Vector3.Zero, MathF.PI);
        if (jammer) Assert.True(b.AddUnit("J", 1, MechChassis.Light(MechModel.Sicklefoot), new Vector3(40, 0, 0), MathF.PI).Chassis.Ecm);
        var d = designator ? b.AddUnit("D", 0, MechChassis.Light(MechModel.Bodkin), new Vector3(0, 0, 220), 0f) : null;
        float twist = 0f, aim = 0f, dTwist = 0f, dAim = 0f;
        for (int i = 0; i < 120 * 20; i++)
        {
            AimSolver.Track(heavy.Sim.Pose, heavy.Sim.Locomotion.Body, heavy.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
            heavy.Input = new MechInput { TwistTarget = twist, AimTarget = aim };
            heavy.LockCandidate = target;
            if (d != null)
            {
                AimSolver.Track(d.Sim.Pose, d.Sim.Locomotion.Body, d.Chassis, target.Volume(MechSection.Torso).Center, ref dTwist, ref dAim);
                d.Input = new MechInput { TwistTarget = dTwist, AimTarget = dAim };
                d.LockCandidate = target;
            }
            b.Step(H);
            if (heavy.Locked != null) return b.Time;
        }
        return float.PositiveInfinity;
    }

    /// <summary>
    /// Arc missiles steer on a lock track refreshed once a second: a light weaving flat out shakes most of a salvo
    /// (lance games: hits on lights 74 -> 33 %), while an assault running a straight line is still hit. Over 4
    /// seeds, damage taken relative to the same mech standing still.
    /// </summary>
    [Fact]
    public void ArcMissiles_MissAWeavingLight_ButNotAnAssault()
    {
        float Taken(ChassisClass cls, bool moving)
        {
            float sum = 0f;
            for (uint seed = 1; seed <= 4; seed++)
            {
                var b = new BattleSim(new ArenaMap { Radius = 900f }, seed) { TrackOutcome = false };
                var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 500), 0f);
                b.AddUnit("L", 0, MechChassis.Light(MechModel.Bascinet), new Vector3(250, 0, 0), 0f); // spotter
                var c = MechChassis.Create(cls, MechModel.Bascinet);
                var target = b.AddUnit("T", 1, c, Vector3.Zero, MathF.PI / 2f, damage: Tough(c));
                float twist = 0f, aim = 0f;
                bool launched = false;
                for (int i = 0; i < 120 * 12; i++)
                {
                    AimSolver.Track(heavy.Sim.Pose, heavy.Sim.Locomotion.Body, heavy.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
                    heavy.LockCandidate = target;
                    heavy.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = heavy.Locked != null && !launched ? Rack : 0u };
                    float t = b.Time + seed;
                    target.Input = !moving ? default
                        : cls == ChassisClass.Light ? new MechInput { Throttle = 1f, Turn = MathF.Sign(MathF.Sin(MathF.Tau * t / 2f)) }
                        : new MechInput { Throttle = 1f };
                    b.Step(H);
                    launched |= heavy.Stats.Shots > 0;
                }
                Assert.True(launched);
                sum += MissileTests.Taken(target);
            }
            return sum;
        }
        float lightStill = Taken(ChassisClass.Light, false), lightWeaving = Taken(ChassisClass.Light, true);
        float assaultStill = Taken(ChassisClass.Assault, false), assaultRunning = Taken(ChassisClass.Assault, true);
        _out.WriteLine($"4 arc salvos: light {lightStill:0} standing, {lightWeaving:0} weaving; assault {assaultStill:0} standing, {assaultRunning:0} running straight");
        Assert.True(lightWeaving < 0.5f * lightStill, "a weaving light shakes most of the salvo");
        Assert.True(assaultRunning > 0.6f * assaultStill, "an assault can't outrun it");
    }

    [Fact]
    public void ArcMissiles_AreDudsInsideTheirMinimumRange()
    {
        var b = new BattleSim(new ArenaMap { Radius = 600f }) { TrackOutcome = false };
        var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 100), 0f);
        var target = b.AddUnit("T", 1, MechChassis.Medium(), Vector3.Zero, MathF.PI);
        float taken = Salvo(b, heavy, target, 8f);
        _out.WriteLine($"{heavy.Stats.Shots} rounds at 100 m: {taken:0.0} damage");
        Assert.True(heavy.Stats.Shots > 0);
        Assert.Equal(0f, taken);
    }

    /// <summary>Point defense thins a salvo on an assault, strike rack or arc missiles (it doesn't stop it).</summary>
    [Fact]
    public void PointDefense_ShootsDownPartOfASalvo()
    {
        float Run(bool pd, ChassisClass shooterClass, float distance)
        {
            var b = new BattleSim(new ArenaMap { Radius = 600f }) { TrackOutcome = false };
            var shooter = b.AddUnit("S", 0, MechChassis.Create(shooterClass, MechModel.Sicklefoot), new Vector3(0, 0, distance), 0f);
            var c = MechChassis.Assault();
            c.PointDefense = pd ? 1 : 0;
            var target = b.AddUnit("T", 1, c, Vector3.Zero, MathF.PI, damage: Tough(c));
            int shotDown = 0;
            float taken = 0f;
            taken = Salvo(b, shooter, target, 8f);
            foreach (var e in b.Events) if (e.Kind == BattleEventKind.PointDefense) shotDown++;
            return taken;
        }
        foreach (var (cls, d) in new[] { (ChassisClass.Medium, 250f), (ChassisClass.Heavy, 400f) })
        {
            float without = Run(false, cls, d), with = Run(true, cls, d);
            _out.WriteLine($"{cls} rack on an assault at {d} m: {without:0.0} damage without point defense, {with:0.0} with");
            Assert.InRange(with, 0.25f * without, 0.85f * without);
        }
    }

    /// <summary>
    /// Share of one arc-missile volley shot down by N turrets around the target: one turret only thins it, stopping
    /// most of it takes several (on the mech or across the lance).
    /// </summary>
    [Fact]
    public void PointDefense_Stacks_OneTurretOnlyThinsAVolley()
    {
        float ShotDown(int turrets)
        {
            int down = 0, fired = 0;
            for (uint seed = 1; seed <= 6; seed++)
            {
                var b = new BattleSim(new ArenaMap { Radius = 600f }, seed) { TrackOutcome = false };
                var heavy = b.AddUnit("H", 0, MechChassis.Heavy(), new Vector3(0, 0, 450), 0f);
                b.AddUnit("L", 0, MechChassis.Light(), new Vector3(200, 0, 0), 0f); // spotter
                var c = MechChassis.Assault();
                c.PointDefense = turrets;
                var target = b.AddUnit("T", 1, c, Vector3.Zero, MathF.PI, damage: Tough(c));
                float twist = 0f, aim = 0f;
                bool launched = false;
                for (int i = 0; i < 120 * 12; i++)
                {
                    AimSolver.Track(heavy.Sim.Pose, heavy.Sim.Locomotion.Body, heavy.Chassis, target.Volume(MechSection.Torso).Center, ref twist, ref aim);
                    heavy.LockCandidate = target;
                    bool fire = heavy.Locked != null && !launched;
                    heavy.Input = new MechInput { TwistTarget = twist, AimTarget = aim, FireMask = fire ? Rack : 0u };
                    b.Step(H);
                    launched |= heavy.Stats.Shots > 0;
                    foreach (var e in b.Events) if (e.Kind == BattleEventKind.PointDefense) down++;
                }
                fired += heavy.Stats.Shots;
            }
            return (float)down / Math.Max(1, fired);
        }
        float one = ShotDown(1), two = ShotDown(2), three = ShotDown(3);
        _out.WriteLine($"arc volley shot down: 1 turret {100 * one:0} %, 2 turrets {100 * two:0} %, 3 turrets {100 * three:0} %");
        Assert.InRange(one, 0.15f, 0.5f);
        Assert.True(three > 1.6f * one, "turrets add up");
    }
}
