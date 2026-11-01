using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Damage;

namespace IronFeud.Mech;

/// <summary>
/// Draws gait state through geometry: plants (green), swing origin (blue), predicted landing (orange),
/// ankle targets (white), leg bones (yellow) and heading / torso aim ray (cyan / red) for <see cref="Mech"/>,
/// plus hit spheres for every mech in <see cref="AllMechs"/> (cockpit magenta).
/// </summary>
public partial class GaitDebugDraw : MeshInstance3D
{
    public MechNode? Mech;
    public IReadOnlyList<MechNode>? AllMechs;

    private readonly ImmediateMesh _mesh = new();

    private static readonly Color Plant = new(0.3f, 1f, 0.4f);
    private static readonly Color From = new(0.3f, 0.6f, 1f);
    private static readonly Color Landing = new(1f, 0.6f, 0.1f);
    private static readonly Color Target = new(1f, 1f, 1f);
    private static readonly Color Bone = new(1f, 0.9f, 0.2f);
    private static readonly Color Heading = new(0.2f, 1f, 1f);
    private static readonly Color Aim = new(1f, 0.25f, 0.2f);
    private static readonly Color HitSphere = new(0.9f, 0.9f, 0.9f, 0.6f);
    private static readonly Color CockpitSphere = new(1f, 0.2f, 1f);

    public override void _Ready()
    {
        TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        CastShadow = ShadowCastingSetting.Off;
        Mesh = _mesh;
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            NoDepthTest = true,
            DisableFog = true,
        };
    }

    public override void _Process(double delta)
    {
        _mesh.ClearSurfaces();
        if (Mech?.Locomotion is not { } loco) return;

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        var body = loco.Body;
        Vector3 root = body.Position.ToGodot() + Vector3.Up * 0.05f;
        Line(root, root + MathUtil.Forward(body.Psi).ToGodot() * 4f, Heading);

        Vector3 aimFrom = loco.Pose.AimOrigin.ToGodot();
        Line(aimFrom, aimFrom + loco.Pose.AimDirection.ToGodot() * 40f, Aim);

        for (int i = 0; i < loco.Gait.Legs.Length; i++)
        {
            var leg = loco.Gait.Legs[i];
            var lp = loco.Pose.Legs[i];
            Cross(leg.Plant.Position.ToGodot(), 0.5f, Plant);
            if (leg.Swing)
            {
                Vector3 from = leg.From.Position.ToGodot(), land = lp.Target.Landing.Position.ToGodot();
                Cross(from, 0.35f, From);
                Cross(land, 0.6f, Landing);
                Line(from, land, Landing);
            }
            Cross(lp.Target.Ankle.ToGodot(), 0.25f, Target);
            Line(lp.HipWorld.ToGodot(), lp.KneeWorld.ToGodot(), Bone);
            Line(lp.KneeWorld.ToGodot(), lp.HockWorld.ToGodot(), Bone);
            Line(lp.HockWorld.ToGodot(), lp.AnkleWorld.ToGodot(), Bone);
        }
        if (AllMechs != null)
            foreach (var m in AllMechs)
            {
                if (!IsInstanceValid(m)) continue;
                foreach (var v in m.Sim.Pose.HitVolumes)
                    Sphere(v.Center.ToGodot(), v.Radius, v.Section == MechSection.Cockpit ? CockpitSphere : HitSphere);
                foreach (var b in m.Sim.Pose.HitBoxes)
                    if (!m.Sim.Damage.IsDestroyed(b.Section)) WireBox(b, HitSphere);
            }
        _mesh.SurfaceEnd();
    }

    /// <summary>A cover plate's twelve edges.</summary>
    private void WireBox(Logic.Mech.Damage.HitBox b, Color col)
    {
        Vector3 Corner(int i) => (b.Center + System.Numerics.Vector3.Transform(new System.Numerics.Vector3(
            (i & 1) != 0 ? b.HalfExtents.X : -b.HalfExtents.X, (i & 2) != 0 ? b.HalfExtents.Y : -b.HalfExtents.Y,
            (i & 4) != 0 ? b.HalfExtents.Z : -b.HalfExtents.Z), b.Rotation)).ToGodot();
        for (int i = 0; i < 8; i++)
            for (int bit = 1; bit < 8; bit <<= 1)
                if ((i & bit) == 0) Line(Corner(i), Corner(i | bit), col);
    }

    private void Sphere(Vector3 c, float r, Color col)
    {
        const int n = 20;
        for (int i = 0; i < n; i++)
        {
            float a0 = Mathf.Tau * i / n, a1 = Mathf.Tau * (i + 1) / n;
            Vector2 p0 = new(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r), p1 = new(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r);
            Line(c + new Vector3(p0.X, p0.Y, 0), c + new Vector3(p1.X, p1.Y, 0), col);
            Line(c + new Vector3(p0.X, 0, p0.Y), c + new Vector3(p1.X, 0, p1.Y), col);
            Line(c + new Vector3(0, p0.X, p0.Y), c + new Vector3(0, p1.X, p1.Y), col);
        }
    }

    private void Line(Vector3 a, Vector3 b, Color c)
    {
        _mesh.SurfaceSetColor(c);
        _mesh.SurfaceAddVertex(a);
        _mesh.SurfaceSetColor(c);
        _mesh.SurfaceAddVertex(b);
    }

    private void Cross(Vector3 p, float r, Color c)
    {
        Line(p + new Vector3(-r, 0, 0), p + new Vector3(r, 0, 0), c);
        Line(p + new Vector3(0, 0, -r), p + new Vector3(0, 0, r), c);
        Line(p + new Vector3(0, -r * 0.5f, 0), p + new Vector3(0, r * 0.5f, 0), c);
    }
}
