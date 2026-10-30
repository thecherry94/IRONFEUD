using System;
using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech.Damage;
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

/// <summary>The mechs in this battle plus combined world/mech raycasts. Mechs have no colliders; they are spheres.</summary>
public sealed class Battlefield
{
    private readonly List<MechNode> _mechs = new();

    public IReadOnlyList<MechNode> Mechs => _mechs;

    public event Action<MechNode, DamageReport, Vector3, MechNode?>? Damaged;

    public void Add(MechNode mech)
    {
        _mechs.Add(mech);
        mech.Damaged += (report, at, attacker) => Damaged?.Invoke(mech, report, at, attacker);
    }

    public void Remove(MechNode mech) => _mechs.Remove(mech);

    /// <summary>Nearest hit along a ray (unit direction) against physics geometry and every mech except <paramref name="ignore"/>.</summary>
    public bool Raycast(World3D world, Vector3 from, Vector3 dir, float maxDistance, MechNode? ignore, out BattleHit hit)
    {
        hit = new BattleHit { Point = from + dir * maxDistance, Distance = maxDistance };
        bool found = false;

        var query = PhysicsRayQueryParameters3D.Create(from, from + dir * maxDistance);
        var w = world.DirectSpaceState.IntersectRay(query);
        if (w.Count > 0)
        {
            var p = (Vector3)w["position"];
            hit = new BattleHit { Point = p, Distance = from.DistanceTo(p) };
            found = true;
        }

        foreach (var m in _mechs)
        {
            if (m == ignore || !GodotObject.IsInstanceValid(m)) continue;
            if (m.Sim.Raycast(from.ToLogic(), dir.ToLogic(), hit.Distance, out var mh))
            {
                hit = new BattleHit { Point = mh.Point.ToGodot(), Distance = mh.Distance, Mech = m, Section = mh.Section };
                found = true;
            }
        }
        return found;
    }
}
