using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Game;

/// <summary>
/// World-space cosmetic effects shared by all mechs: dust and fire puffs, footprints, beams and a flash light.
/// Pooled, animated in <c>_Process</c>, and therefore excluded from physics interpolation.
/// </summary>
public partial class Effects : Node3D
{
    private sealed class PuffPool
    {
        private sealed class Puff
        {
            public MeshInstance3D Mesh = null!;
            public Vector3 Velocity;
            public float Life, Max, Size0, Grow, Opacity, Drag;
        }

        private readonly Puff[] _puffs;
        private int _next;

        public PuffPool(Node3D parent, int count, Material material)
        {
            var quad = new QuadMesh { Size = Vector2.One };
            _puffs = new Puff[count];
            for (int i = 0; i < count; i++)
            {
                var mi = new MeshInstance3D
                {
                    Mesh = quad,
                    MaterialOverride = material,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Visible = false,
                };
                parent.AddChild(mi);
                _puffs[i] = new Puff { Mesh = mi };
            }
        }

        public void Emit(Vector3 pos, Vector3 vel, float max, float size0, float grow, float opacity, float drag)
        {
            var p = _puffs[_next++ % _puffs.Length];
            p.Mesh.Position = pos;
            p.Mesh.Scale = Vector3.One * size0;
            p.Mesh.Transparency = 1f - opacity;
            p.Mesh.Visible = true;
            p.Velocity = vel;
            p.Life = 0f;
            p.Max = max;
            p.Size0 = size0;
            p.Grow = grow;
            p.Opacity = opacity;
            p.Drag = drag;
        }

        public void Update(float dt)
        {
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

    private sealed class Beam
    {
        public MeshInstance3D Core = null!, Glow = null!;
        public float T, Duration;
    }

    private PuffPool _dust = null!, _fire = null!;
    private readonly Decal[] _prints = new Decal[40];
    private readonly Beam[] _beams = new Beam[6];
    private OmniLight3D _flash = null!;
    private float _flashT, _flashMax = 0.1f, _flashEnergy;
    private int _nextPrint, _nextBeam;
    private readonly DeterministicRng _rng = new(99);

    public override void _Ready()
    {
        TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;

        _dust = new PuffPool(this, 120, Billboard(
            ProceduralTextures.RadialPuff(Color.Color8(150, 135, 115, 230), Color.Color8(150, 135, 115, 0)), additive: false));
        var fireGradient = new Gradient
        {
            Offsets = new[] { 0f, 0.35f, 1f },
            Colors = new[] { Color.Color8(255, 240, 200), Color.Color8(255, 150, 40, 217), Color.Color8(255, 60, 0, 0) },
        };
        _fire = new PuffPool(this, 60, Billboard(new GradientTexture2D
        {
            Gradient = fireGradient,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
        }, additive: true));

        var printTex = ProceduralTextures.Footprint();
        for (int i = 0; i < _prints.Length; i++)
        {
            _prints[i] = new Decal
            {
                Size = new Vector3(1.6f, 1.0f, 3.0f),
                TextureAlbedo = printTex,
                Modulate = new Color(0.165f, 0.149f, 0.125f, 0.35f),
                Visible = false,
            };
            AddChild(_prints[i]);
        }

        var coreMat = new StandardMaterial3D
        {
            AlbedoColor = new Color("ffd0c0"),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var glowMat = new StandardMaterial3D
        {
            AlbedoColor = new Color("ff2a1a"),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
        };
        var coreMesh = new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.07f, Height = 1f, RadialSegments = 8, Rings = 1 };
        var glowMesh = new CylinderMesh { TopRadius = 0.28f, BottomRadius = 0.28f, Height = 1f, RadialSegments = 10, Rings = 1 };
        for (int i = 0; i < _beams.Length; i++)
        {
            var b = new Beam
            {
                Core = new MeshInstance3D { Mesh = coreMesh, MaterialOverride = coreMat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false },
                Glow = new MeshInstance3D { Mesh = glowMesh, MaterialOverride = glowMat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false },
            };
            AddChild(b.Core);
            AddChild(b.Glow);
            _beams[i] = b;
        }

        _flash = new OmniLight3D { LightColor = new Color("ffa040"), OmniRange = 40f, LightEnergy = 0f, ShadowEnabled = false };
        AddChild(_flash);
    }

    private static StandardMaterial3D Billboard(Texture2D tex, bool additive) => new()
    {
        AlbedoTexture = tex,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BlendMode = additive ? BaseMaterial3D.BlendModeEnum.Add : BaseMaterial3D.BlendModeEnum.Mix,
        BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        DisableReceiveShadows = true,
    };

    public void Footfall(in Footfall f)
    {
        float amt = f.Strength * (0.5f + f.SpeedFraction);
        Vector3 pos = f.Position.ToGodot();
        if (amt > 0.01f)
        {
            for (int k = 0; k < 8; k++)
            {
                float a = _rng.NextFloat() * Mathf.Tau, sp = (1.5f + _rng.NextFloat() * 2.5f) * amt;
                _dust.Emit(pos + new Vector3(Mathf.Cos(a) * 0.8f, 0.3f, Mathf.Sin(a) * 1.3f),
                    new Vector3(Mathf.Cos(a) * sp, 0.4f + _rng.NextFloat() * 0.9f, Mathf.Sin(a) * sp),
                    max: 1.2f + _rng.NextFloat() * 0.9f, size0: (1f + _rng.NextFloat()) * amt, grow: 3.2f,
                    opacity: 0.55f * Mathf.Min(1f, amt), drag: 2.2f);
            }
        }
        var print = _prints[_nextPrint++ % _prints.Length];
        print.Transform = new Transform3D(new Basis(Vector3.Up, f.Yaw), pos + new Vector3(0, 0.05f, 0));
        print.Visible = true;
    }

    /// <summary>Cannon muzzle blast: fire puffs along the barrel, a dust puff and a flash.</summary>
    public void MuzzleBlast(Vector3 at, Vector3 dir)
    {
        for (int k = 0; k < 3; k++)
            _fire.Emit(at + dir * (k * 0.5f), Vector3.Zero, max: 0.09f, size0: 1.6f - k * 0.3f, grow: 1.2f, opacity: 1f, drag: 0f);
        _dust.Emit(at, new Vector3(dir.X * 3f, 0.6f, dir.Z * 3f), max: 1.2f, size0: 0.8f, grow: 2.5f, opacity: 0.35f, drag: 2.2f);
        Flash(at, 30f, 0.07f);
    }

    public void Explosion(Vector3 at, bool big)
    {
        int n = big ? 7 : 3;
        for (int k = 0; k < n; k++)
        {
            var jitter = new Vector3(_rng.NextFloat() - 0.5f, _rng.NextFloat() - 0.5f, _rng.NextFloat() - 0.5f);
            _fire.Emit(at + jitter, new Vector3((_rng.NextFloat() - 0.5f) * 4f, _rng.NextFloat() * 3f, (_rng.NextFloat() - 0.5f) * 4f),
                max: 0.35f + _rng.NextFloat() * 0.3f, size0: big ? 2.2f : 1.0f, grow: 1.6f, opacity: 1f, drag: 3f);
        }
        for (int k = 0; k < (big ? 6 : 2); k++)
        {
            float a = _rng.NextFloat() * Mathf.Tau;
            _dust.Emit(new Vector3(at.X, Mathf.Max(0.3f, at.Y), at.Z), new Vector3(Mathf.Cos(a) * 3f, 1f + _rng.NextFloat() * 2f, Mathf.Sin(a) * 3f),
                max: 1.6f + _rng.NextFloat(), size0: big ? 2f : 1f, grow: 3f, opacity: 0.6f, drag: 2.2f);
        }
        Flash(at, big ? 60f : 25f, big ? 0.12f : 0.08f);
    }

    public void BeamShot(Vector3 from, Vector3 to, float duration)
    {
        var b = _beams[_nextBeam++ % _beams.Length];
        b.Core.Transform = GodotConv.Segment(from, to);
        b.Glow.Transform = b.Core.Transform;
        b.Core.Visible = b.Glow.Visible = true;
        b.T = b.Duration = duration;
        Flash(from, 20f, 0.12f);
    }

    /// <summary>Intensity uses the prototype's numbers; scaled to Godot light energy here.</summary>
    private void Flash(Vector3 at, float intensity, float duration)
    {
        _flash.Position = at;
        _flashEnergy = intensity / 6f;
        _flashT = _flashMax = duration;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _dust.Update(dt);
        _fire.Update(dt);

        foreach (var b in _beams)
        {
            if (b.T <= 0f) continue;
            b.T -= dt;
            float o = Mathf.Max(0f, b.T / b.Duration);
            b.Core.Transparency = 1f - o;
            b.Glow.Transparency = 1f - 0.8f * o;
            if (b.T <= 0f) b.Core.Visible = b.Glow.Visible = false;
        }

        if (_flashT > 0f)
        {
            _flashT -= dt;
            _flash.LightEnergy = _flashEnergy * Mathf.Max(0f, _flashT / _flashMax);
        }
        else _flash.LightEnergy = 0f;
    }
}
