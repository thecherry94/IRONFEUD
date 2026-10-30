using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Mech;

/// <summary>
/// Draws gait state through geometry: plants (green), swing origin (blue), predicted landing (orange),
/// ankle targets (white), leg bones (yellow) and heading / torso aim (cyan / red).
/// </summary>
public partial class GaitDebugDraw : MeshInstance3D
{
    public MechNode? Mech;

    private readonly ImmediateMesh _mesh = new();

    private static readonly Color Plant = new(0.3f, 1f, 0.4f);
    private static readonly Color From = new(0.3f, 0.6f, 1f);
    private static readonly Color Landing = new(1f, 0.6f, 0.1f);
    private static readonly Color Target = new(1f, 1f, 1f);
    private static readonly Color Bone = new(1f, 0.9f, 0.2f);
    private static readonly Color Heading = new(0.2f, 1f, 1f);
    private static readonly Color Aim = new(1f, 0.25f, 0.2f);

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

        Vector3 pelvis = loco.Pose.PelvisPosition.ToGodot();
        float aimYaw = body.Psi + body.Twist + loco.Secondary.TorsoYaw.X;
        Vector3 aimDir = MathUtil.Forward(aimYaw).ToGodot() * Mathf.Cos(body.Aim) + Vector3.Up * Mathf.Sin(body.Aim);
        Line(pelvis + Vector3.Up * 3f, pelvis + Vector3.Up * 3f + aimDir * 25f, Aim);

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
            Line(lp.KneeWorld.ToGodot(), lp.AnkleWorld.ToGodot(), Bone);
        }
        _mesh.SurfaceEnd();
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
