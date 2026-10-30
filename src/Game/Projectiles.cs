using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech.Weapons;
using IronFeud.Mech;

namespace IronFeud.Game;

/// <summary>
/// Resolves shots against the battlefield: shells fly (ballistics from IronFeud.Logic) and sweep a ray each tick,
/// beams are a single raycast. Hits on mech volumes apply damage; the shooter's own volumes are ignored.
/// </summary>
public partial class Projectiles : Node3D
{
    public Battlefield Battlefield = null!;
    public Effects? Effects;

    private sealed class Shell
    {
        public Projectile State;
        public MechNode? Owner;
        public MeshInstance3D Mesh = null!;
        public bool Active;
    }

    private readonly Shell[] _shells = new Shell[32];

    public override void _Ready()
    {
        TopLevel = true;
        var mesh = new BoxMesh { Size = new Vector3(0.16f, 0.16f, 2.6f) };
        var mat = new StandardMaterial3D { AlbedoColor = new Color("ffd27a"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        for (int i = 0; i < _shells.Length; i++)
        {
            var mi = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false };
            AddChild(mi);
            _shells[i] = new Shell { Mesh = mi };
        }
    }

    public void Fire(MechNode owner, in ShotFired shot)
    {
        Vector3 origin = shot.Origin.ToGodot(), dir = shot.Direction.ToGodot();
        switch (shot.Spec.Kind)
        {
            case WeaponKind.Projectile:
                Effects?.MuzzleBlast(origin, dir);
                foreach (var s in _shells)
                {
                    if (s.Active) continue;
                    s.Active = true;
                    s.Owner = owner;
                    s.State = Projectile.From(shot);
                    Place(s);
                    s.Mesh.Visible = true;
                    s.Mesh.ResetPhysicsInterpolation();
                    break;
                }
                break;

            case WeaponKind.Beam:
            {
                bool hit = Battlefield.Raycast(GetWorld3D(), origin, dir, shot.Spec.Range, owner, out var bh);
                Effects?.BeamShot(origin, bh.Point, shot.Spec.BeamVisualTime);
                if (!hit) break;
                Effects?.Explosion(bh.Point, 0f);
                bh.Mech?.ApplyHit(bh.Section, shot.Spec.DamageAt(bh.Distance), bh.Point, owner);
                break;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float h = (float)delta;
        foreach (var s in _shells)
        {
            if (!s.Active) continue;
            Vector3 start = s.State.Step(h).ToGodot(), end = s.State.Position.ToGodot();
            float len = start.DistanceTo(end);
            bool hit = Battlefield.Raycast(GetWorld3D(), start, (end - start) / len, len, s.Owner, out var bh);
            if (hit || s.State.Expired)
            {
                s.Active = false;
                s.Mesh.Visible = false;
                if (!hit) continue;
                Effects?.Explosion(bh.Point, 1f);
                bh.Mech?.ApplyHit(bh.Section, s.State.Spec.Damage, bh.Point, IsInstanceValid(s.Owner) ? s.Owner : null);
                continue;
            }
            Place(s);
        }
    }

    private static void Place(Shell s)
    {
        Vector3 pos = s.State.Position.ToGodot(), dir = s.State.Velocity.ToGodot().Normalized();
        Vector3 up = Mathf.Abs(dir.Y) > 0.99f ? Vector3.Right : Vector3.Up;
        s.Mesh.Transform = new Transform3D(Basis.LookingAt(dir, up), pos);
    }
}
