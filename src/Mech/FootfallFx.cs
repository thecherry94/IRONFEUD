using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Mech;

/// <summary>
/// Cosmetic footfall effects: pooled dust puffs and footprint decals. Lives in world space and is
/// animated in <c>_Process</c>, so it opts out of physics interpolation.
/// </summary>
public partial class FootfallFx : Node3D
{
    private sealed class Puff
    {
        public MeshInstance3D Mesh = null!;
        public Vector3 Velocity;
        public float Life, Max, Size0, Grow, Opacity, Drag;
    }

    private const int PuffCount = 80;
    private const int PrintCount = 40;

    private readonly Puff[] _puffs = new Puff[PuffCount];
    private readonly Decal[] _prints = new Decal[PrintCount];
    private readonly DeterministicRng _rng = new(99);
    private int _nextPuff, _nextPrint;

    public override void _Ready()
    {
        TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

        var dustMat = new StandardMaterial3D
        {
            AlbedoTexture = ProceduralTextures.RadialPuff(Color.Color8(150, 135, 115, 230), Color.Color8(150, 135, 115, 0)),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            DisableReceiveShadows = true,
        };
        var quad = new QuadMesh { Size = Vector2.One };
        for (int i = 0; i < PuffCount; i++)
        {
            var mi = new MeshInstance3D
            {
                Mesh = quad,
                MaterialOverride = dustMat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false,
            };
            AddChild(mi);
            _puffs[i] = new Puff { Mesh = mi };
        }

        var printTex = ProceduralTextures.Footprint();
        for (int i = 0; i < PrintCount; i++)
        {
            var d = new Decal
            {
                Size = new Vector3(1.6f, 1.0f, 3.0f),
                TextureAlbedo = printTex,
                Modulate = new Color(0.165f, 0.149f, 0.125f, 0.35f),
                Visible = false,
            };
            AddChild(d);
            _prints[i] = d;
        }
    }

    public void Spawn(in Footfall f)
    {
        float amt = f.Strength * (0.5f + f.SpeedFraction);
        Vector3 pos = f.Position.ToGodot();
        if (amt > 0.01f)
        {
            for (int k = 0; k < 8; k++)
            {
                float a = _rng.NextFloat() * Mathf.Tau, sp = (1.5f + _rng.NextFloat() * 2.5f) * amt;
                Emit(pos + new Vector3(Mathf.Cos(a) * 0.8f, 0.3f, Mathf.Sin(a) * 1.3f),
                    new Vector3(Mathf.Cos(a) * sp, 0.4f + _rng.NextFloat() * 0.9f, Mathf.Sin(a) * sp),
                    max: 1.2f + _rng.NextFloat() * 0.9f, size0: (1f + _rng.NextFloat()) * amt, opacity: 0.55f * Mathf.Min(1f, amt));
            }
        }

        var print = _prints[_nextPrint++ % PrintCount];
        print.Transform = new Transform3D(new Basis(Vector3.Up, f.Yaw), pos + new Vector3(0, 0.05f, 0));
        print.Visible = true;
    }

    private void Emit(Vector3 pos, Vector3 vel, float max, float size0, float opacity, float grow = 3.2f, float drag = 2.2f)
    {
        var p = _puffs[_nextPuff++ % PuffCount];
        p.Mesh.Position = pos;
        p.Mesh.Visible = true;
        p.Velocity = vel;
        p.Life = 0f;
        p.Max = max;
        p.Size0 = size0;
        p.Grow = grow;
        p.Opacity = opacity;
        p.Drag = drag;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        foreach (var p in _puffs)
        {
            if (!p.Mesh.Visible) continue;
            p.Life += dt;
            float t = p.Life / p.Max;
            if (t >= 1f) { p.Mesh.Visible = false; continue; }
            p.Mesh.Position += p.Velocity * dt;
            p.Velocity *= Mathf.Max(0f, 1f - p.Drag * dt);
            p.Mesh.Scale = Vector3.One * (p.Size0 * (1f + p.Grow * t));
            p.Mesh.Transparency = 1f - p.Opacity * (1f - t);
        }
    }
}
