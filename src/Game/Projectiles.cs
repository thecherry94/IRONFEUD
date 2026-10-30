using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Game;

/// <summary>
/// Resolves shots against the physics world: shells fly (ballistics from IronFeud.Logic) and sweep a ray each tick,
/// beams are a single raycast. Damage comes with step 3; for now hits only produce effects.
/// </summary>
public partial class Projectiles : Node3D
{
    public Effects? Effects;

    private sealed class Shell
    {
        public Projectile State;
        public MeshInstance3D Mesh = null!;
        public bool Active;
    }

    private readonly Shell[] _shells = new Shell[24];

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

    public void Fire(in ShotFired shot)
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
                    s.State = Projectile.From(shot);
                    Place(s);
                    s.Mesh.Visible = true;
                    s.Mesh.ResetPhysicsInterpolation();
                    break;
                }
                break;

            case WeaponKind.Beam:
            {
                Vector3 end = origin + dir * shot.Spec.Range;
                bool hit = Raycast(origin, end, out Vector3 at);
                Effects?.BeamShot(origin, hit ? at : end, shot.Spec.BeamVisualTime);
                if (hit) Effects?.Explosion(at, big: false);
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
            Vector3 start = s.State.Step(h).ToGodot();
            bool hit = Raycast(start, s.State.Position.ToGodot(), out Vector3 at);
            if (hit || s.State.Expired)
            {
                s.Active = false;
                s.Mesh.Visible = false;
                if (hit) Effects?.Explosion(at, big: true);
                continue;
            }
            Place(s);
        }
    }

    private bool Raycast(Vector3 from, Vector3 to, out Vector3 at)
    {
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));
        at = hit.Count > 0 ? (Vector3)hit["position"] : to;
        return hit.Count > 0;
    }

    private static void Place(Shell s)
    {
        Vector3 pos = s.State.Position.ToGodot(), dir = s.State.Velocity.ToGodot().Normalized();
        Vector3 up = Mathf.Abs(dir.Y) > 0.99f ? Vector3.Right : Vector3.Up;
        s.Mesh.Transform = new Transform3D(Basis.LookingAt(dir, up), pos);
    }
}
