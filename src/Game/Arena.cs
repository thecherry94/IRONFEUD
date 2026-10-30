using Godot;
using IronFeud.Core;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;

namespace IronFeud.Game;

/// <summary>
/// Renders an <see cref="ArenaMap"/>: environment, ground, its blocks, cosmetic rocks and the boundary ring.
/// Collision and line of sight come from the map itself, so nothing here has a physics body.
/// </summary>
public static class Arena
{
    private const float GroundSize = 2000f;
    private static readonly Color Fog = new("8d98a0");

    public static void Build(Node3D root, ArenaMap map, uint seed)
    {
        var rng = new DeterministicRng(seed);
        BuildEnvironment(root);
        BuildGround(root, seed);

        var panel = ProceduralTextures.ArmorPanel(seed);
        var propMats = new[]
        {
            Prop("6d675e", 0.9f, 0f, panel),
            Prop("5a5f63", 0.8f, 0.2f, panel),
            Prop("7b6f60", 0.95f, 0f, panel),
        };

        var blocks = new Node3D { Name = "Blocks" };
        root.AddChild(blocks);
        for (int i = 0; i < map.Blocks.Count; i++)
        {
            var b = map.Blocks[i];
            blocks.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = b.Size.ToGodot() },
                MaterialOverride = propMats[i % 3],
                Position = b.Center.ToGodot(),
            });
        }

        var rocks = new Node3D { Name = "Rocks" };
        root.AddChild(rocks);
        var rockMesh = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 7, Rings = 3 };
        float spread = map.Radius > 0f ? map.Radius + 40f : 160f;
        for (int i = 0; i < 110; i++)
        {
            float s = 0.3f + rng.NextFloat() * 1.3f;
            float a = rng.NextFloat() * Mathf.Tau, r = 6f + rng.NextFloat() * spread;
            Vector2 p = new(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            if (map.InsideAnyBlock(p.X, p.Y, 1f)) continue;
            rocks.AddChild(new MeshInstance3D
            {
                Mesh = rockMesh,
                MaterialOverride = propMats[2],
                Position = new Vector3(p.X, s * 0.4f, p.Y),
                Rotation = new Vector3(rng.NextFloat() * 3f, rng.NextFloat() * 3f, 0),
                Scale = new Vector3(s * (1f + rng.NextFloat()), s, s * (1f + rng.NextFloat())),
            });
        }

        if (map.Radius > 0f)
        {
            root.AddChild(new MeshInstance3D
            {
                Name = "Boundary",
                Mesh = new TorusMesh { InnerRadius = map.Radius + 4f, OuterRadius = map.Radius + 6f, Rings = 96, RingSegments = 4 },
                Scale = new Vector3(1f, 0.05f, 1f),
                Position = new Vector3(0f, 0.05f, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(1f, 0.54f, 0.16f, 0.35f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            });
        }
    }

    private static StandardMaterial3D Prop(string color, float rough, float metal, Texture2D panel) => new()
    {
        AlbedoColor = new Color(color),
        AlbedoTexture = panel,
        Roughness = rough,
        Metallic = metal,
        Uv1Triplanar = true,
        Uv1Scale = new Vector3(0.25f, 0.25f, 0.25f),
        TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
    };

    private static void BuildGround(Node3D root, uint seed)
    {
        var ground = new StaticBody3D { Name = "Ground" };
        ground.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(GroundSize, GroundSize) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = ProceduralTextures.Ground(seed),
                Roughness = 0.95f,
                Uv1Scale = new Vector3(GroundSize / 10f, GroundSize / 10f, 1f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        ground.AddChild(new CollisionShape3D { Shape = new WorldBoundaryShape3D() });
        root.AddChild(ground);
    }

    private static void BuildEnvironment(Node3D root)
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky
            {
                SkyMaterial = new ProceduralSkyMaterial
                {
                    SkyTopColor = new Color("aab4bc"),
                    SkyHorizonColor = Fog,
                    GroundHorizonColor = Fog,
                    GroundBottomColor = new Color("5a4e40"),
                },
            },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 1.0f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.05f,
            FogEnabled = true,
            FogMode = Godot.Environment.FogModeEnum.Depth,
            FogLightColor = Fog,
            FogDensity = 1f,
            FogDepthBegin = 200f,
            FogDepthEnd = 800f,
            FogSkyAffect = 1f,
        };
        root.AddChild(new WorldEnvironment { Environment = env });

        var sun = new DirectionalLight3D
        {
            Name = "Sun",
            LightColor = new Color("fff0dd"),
            LightEnergy = 1.4f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 220f,
        };
        root.AddChild(sun);
        sun.LookAtFromPosition(new Vector3(-30, 55, -22), Vector3.Zero, Vector3.Up);
    }
}
