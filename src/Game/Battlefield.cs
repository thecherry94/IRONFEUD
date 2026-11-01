using System;
using System.Collections.Generic;
using Godot;
using IronFeud.Audio;
using IronFeud.Core;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Mech.Damage;
using IronFeud.Logic.Mech.Weapons;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>What a ray struck: world geometry, or a mech's hit volume.</summary>
public struct BattleHit
{
    public Vector3 Point;
    public float Distance;
    public MechNode? Mech;
    public MechSection Section;
}

/// <summary>
/// Godot side of a <see cref="BattleSim"/>: steps it at the physics tick (after the pilots have written their input),
/// poses the mech views, draws shells and turns battle events into effects. All game rules live in the BattleSim.
/// </summary>
public partial class Battlefield : Node3D
{
    public static readonly Color[] TeamAccent = { new("c46a2c"), new("2c7ac4") };
    public static readonly Color[] TeamColor = { new("ff9a4a"), new("5ab4ff") };
    public static readonly string[] TeamName = { "ALPHA", "BRAVO" };

    public BattleSim Battle { get; private set; } = null!;
    public Effects? Effects;
    public AudioDirector? Audio;

    private readonly List<MechNode> _mechs = new();
    private readonly Dictionary<BattleUnit, MechNode> _nodes = new();
    private readonly Dictionary<int, MeshInstance3D> _shells = new();
    private readonly Stack<MeshInstance3D> _shellPool = new();
    private readonly HashSet<int> _liveShells = new();
    private Mesh _shellMesh = null!;
    private Material _shellMaterial = null!;

    public IReadOnlyList<MechNode> Mechs => _mechs;

    /// <summary>A hit was applied: mech, report, world point, attacker (if known).</summary>
    public event Action<MechNode, DamageReport, Vector3, MechNode?>? Damaged;

    public override void _Ready()
    {
        _shellMesh = new BoxMesh { Size = new Vector3(0.18f, 0.18f, 6f) };
        _shellMaterial = new StandardMaterial3D { AlbedoColor = new Color("ffd27a"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    }

    /// <summary>Takes over a battle and builds a view for each of its units. Call once, after adding this node.</summary>
    public void Begin(BattleSim battle, bool teamColors = true)
    {
        Battle = battle;
        battle.Damaged += OnDamaged;
        foreach (var u in battle.Units)
        {
            var node = new MechNode { Unit = u, Accent = teamColors ? TeamAccent[u.Team % 2] : MechRig.DefaultAccent };
            AddChild(node);
            _nodes[u] = node;
            _mechs.Add(node);
            Audio?.Attach(node);
        }
    }

    public MechNode? NodeOf(BattleUnit? u) => u != null && _nodes.TryGetValue(u, out var n) ? n : null;

    public override void _PhysicsProcess(double delta)
    {
        if (Battle == null) return;
        Battle.Step((float)delta);

        foreach (var m in _mechs)
        {
            m.SyncFromSimulation();
            if (m.Locomotion.JetsBurning) Effects?.JetFlame(m);
            foreach (var f in m.Locomotion.Footfalls)
            {
                Effects?.Footfall(f, m.Sim.Chassis.Scale);
                Audio?.Footfall(m, f);
            }
        }

        foreach (var e in Battle.Events)
        {
            Vector3 from = e.From.ToGodot(), at = e.Point.ToGodot();
            switch (e.Kind)
            {
                case BattleEventKind.ShellFired:
                    Effects?.MuzzleBlast(from, e.Shot.Direction.ToGodot(), Weight(e.Shot.Spec));
                    Audio?.Shot(e.Shot.Spec, from);
                    break;
                case BattleEventKind.Beam when e.Shot.Phase == ShotPhase.Single:
                    Effects?.BeamShot(from, at, e.Shot.Spec.BeamVisualTime, NodeOf(e.Shooter)?.Accent, Weight(e.Shot.Spec));
                    Audio?.Shot(e.Shot.Spec, from);
                    break;
                case BattleEventKind.Beam:
                {
                    int key = e.Shooter.Id * 8 + e.Shot.Weapon;
                    bool start = e.Shot.Phase == ShotPhase.Start;
                    Effects?.BeamHold(key, from, at, start, e.Target != null, e.Hit && e.Target == null, NodeOf(e.Shooter)?.Accent, Weight(e.Shot.Spec));
                    Audio?.BeamHold(key, e.Shot.Spec, from, start);
                    break;
                }
                case BattleEventKind.Impact:
                {
                    // Size 1 = the cannon's shell, by the damage the round carried (a pulse, one missile of a salvo);
                    // beams leave smaller scorches.
                    float share = e.Shot.DamageFraction;
                    Effects?.Explosion(at, Mathf.Sqrt(e.Shot.Spec.Damage * share / WeaponSpec.Cannon.Damage) * (e.Shot.Spec.Kind == WeaponKind.Beam ? 0.35f : 1f));
                    Audio?.Impact(e.Shot.Spec, at, e.Target != null, share);
                    break;
                }
                case BattleEventKind.MissileFired:
                    Effects?.MissileLaunch(from, e.Shot.Direction.ToGodot());
                    Audio?.MissileLaunch(e.Shot.Spec, from);
                    break;
                case BattleEventKind.PointDefense:
                    Effects?.PointDefenseShot(from, at);
                    Audio?.PointDefense(from);
                    break;
            }
        }
        DrawShells();
        DrawMissiles();
    }

    private readonly Dictionary<int, MeshInstance3D> _missiles = new();
    private readonly Stack<MeshInstance3D> _missilePool = new();
    private readonly HashSet<int> _liveMissiles = new();
    private Mesh? _missileMesh;

    /// <summary>Missiles in flight: a slim body pointed along its velocity, with an exhaust trail.</summary>
    private void DrawMissiles()
    {
        _missileMesh ??= new BoxMesh { Size = new Vector3(0.22f, 0.22f, 1.8f) };
        _liveMissiles.Clear();
        foreach (var m in Battle.Missiles)
        {
            _liveMissiles.Add(m.Id);
            bool fresh = !_missiles.TryGetValue(m.Id, out var mesh);
            if (fresh)
            {
                mesh = _missilePool.Count > 0 ? _missilePool.Pop() : NewMissileMesh();
                _missiles[m.Id] = mesh;
                mesh.Visible = true;
            }
            Vector3 pos = m.Position.ToGodot(), vel = m.Velocity.ToGodot(), dir = vel.Normalized();
            Vector3 up = Mathf.Abs(dir.Y) > 0.99f ? Vector3.Right : Vector3.Up;
            mesh!.Transform = new Transform3D(Basis.LookingAt(dir, up), pos);
            if (fresh) mesh.ResetPhysicsInterpolation();
            Effects?.MissileTrail(pos - dir * 1f, vel);
        }
        if (_missiles.Count == _liveMissiles.Count) return;
        foreach (var id in new List<int>(_missiles.Keys))
        {
            if (_liveMissiles.Contains(id)) continue;
            _missiles[id].Visible = false;
            _missilePool.Push(_missiles[id]);
            _missiles.Remove(id);
        }
    }

    private MeshInstance3D NewMissileMesh()
    {
        var mi = new MeshInstance3D { Mesh = _missileMesh, MaterialOverride = _shellMaterial, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, TopLevel = true };
        AddChild(mi);
        return mi;
    }

    private void OnDamaged(BattleUnit unit, DamageReport report, System.Numerics.Vector3 at, BattleUnit? attacker)
    {
        var node = NodeOf(unit);
        if (node == null) return;
        node.OnDamaged(report);
        Effects?.OnDamaged(node, report, at.ToGodot());
        Audio?.OnDamaged(node, report, at.ToGodot());
        Damaged?.Invoke(node, report, at.ToGodot(), NodeOf(attacker));
    }

    private void DrawShells()
    {
        _liveShells.Clear();
        foreach (var s in Battle.Shells)
        {
            _liveShells.Add(s.Id);
            bool fresh = !_shells.TryGetValue(s.Id, out var mesh);
            if (fresh)
            {
                mesh = _shellPool.Count > 0 ? _shellPool.Pop() : NewShellMesh();
                _shells[s.Id] = mesh;
                mesh.Visible = true;
            }
            Vector3 pos = s.State.Position.ToGodot(), dir = s.State.Velocity.ToGodot().Normalized();
            Vector3 up = Mathf.Abs(dir.Y) > 0.99f ? Vector3.Right : Vector3.Up;
            float k = s.State.Spec.Caliber / WeaponSpec.Cannon.Caliber;
            mesh!.Transform = new Transform3D(Basis.LookingAt(dir, up) * Basis.FromScale(new Vector3(k, k, Mathf.Sqrt(k))), pos);
            if (fresh) mesh.ResetPhysicsInterpolation();
        }
        if (_shells.Count == _liveShells.Count) return;
        foreach (var id in new List<int>(_shells.Keys))
        {
            if (_liveShells.Contains(id)) continue;
            _shells[id].Visible = false;
            _shellPool.Push(_shells[id]);
            _shells.Remove(id);
        }
    }

    /// <summary>Visual weight of a weapon relative to the cannon (1): sizes flashes, beams and impacts.</summary>
    private static float Weight(WeaponSpec spec) => Mathf.Sqrt(spec.Damage / WeaponSpec.Cannon.Damage);

    private MeshInstance3D NewShellMesh()
    {
        var mi = new MeshInstance3D { Mesh = _shellMesh, MaterialOverride = _shellMaterial, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, TopLevel = true };
        AddChild(mi);
        return mi;
    }

    /// <summary>Nearest hit along a ray (unit direction) against the map and every mech except <paramref name="ignore"/>.</summary>
    public bool Raycast(Vector3 from, Vector3 dir, float maxDistance, MechNode? ignore, out BattleHit hit)
    {
        bool found = Battle.Raycast(from.ToLogic(), dir.ToLogic(), maxDistance, ignore?.Unit, out var h);
        hit = new BattleHit { Point = h.Point.ToGodot(), Distance = h.Distance, Mech = NodeOf(h.Unit), Section = h.Section };
        return found;
    }
}
