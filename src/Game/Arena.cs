using Godot;
using IronFeud.Core;
using IronFeud.Logic.Core;

namespace IronFeud.Game;

/// <summary>Flat test ground with scattered blocks and rocks, environment and sun. Placeholder for step 4's arena.</summary>
public static class Arena
{
    private const float GroundSize = 2000f;
    private const float Scatter = 160f;
    private static readonly Color Fog = new("8d98a0");

    public static void Build(Node3D root, uint seed)
    {
        var rng = new DeterministicRng(seed);
        BuildEnvironment(root);
        BuildGround(root, seed);

        var propMats = new[]
        {
            new StandardMaterial3D { AlbedoColor = new Color("6d675e"), Roughness = 0.9f },
            new StandardMaterial3D { AlbedoColor = new Color("5a5f63"), Roughness = 0.8f, Metallic = 0.2f },
            new StandardMaterial3D { AlbedoColor = new Color("7b6f60"), Roughness = 0.95f },
        };

        var blocks = new Node3D { Name = "Blocks" };
        root.AddChild(blocks);
        for (int i = 0; i < 150; i++)
        {
            float h = 3f + rng.NextFloat() * rng.NextFloat() * 26f, w = 3f + rng.NextFloat() * 9f, d = 3f + rng.NextFloat() * 9f;
            Vector2 p = RandomSpot(rng, 16f);
            var body = new StaticBody3D
            {
                Position = new Vector3(p.X, h / 2f, p.Y),
                Rotation = new Vector3(0, rng.NextFloat() * 3f, 0),
            };
            var size = new Vector3(w, h, d);
            body.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = propMats[i % 3] });
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
            blocks.AddChild(body);
        }

        var rocks = new Node3D { Name = "Rocks" };
        root.AddChild(rocks);
        var rockMesh = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 7, Rings = 3 };
        for (int i = 0; i < 110; i++)
        {
            float s = 0.3f + rng.NextFloat() * 1.3f;
            Vector2 p = RandomSpot(rng, 6f);
            rocks.AddChild(new MeshInstance3D
            {
                Mesh = rockMesh,
                MaterialOverride = propMats[2],
                Position = new Vector3(p.X, s * 0.4f, p.Y),
                Rotation = new Vector3(rng.NextFloat() * 3f, rng.NextFloat() * 3f, 0),
                Scale = new Vector3(s * (1f + rng.NextFloat()), s, s * (1f + rng.NextFloat())),
            });
        }
    }

    private static Vector2 RandomSpot(DeterministicRng rng, float minRadius)
    {
        while (true)
        {
            var p = new Vector2(rng.Range(-Scatter, Scatter), rng.Range(-Scatter, Scatter));
            if (p.Length() > minRadius) return p;
        }
    }

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
            FogDepthBegin = 70f,
            FogDepthEnd = 280f,
            FogSkyAffect = 1f,
        };
        root.AddChild(new WorldEnvironment { Environment = env });

        var sun = new DirectionalLight3D
        {
            Name = "Sun",
            LightColor = new Color("fff0dd"),
            LightEnergy = 1.4f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 160f,
        };
        root.AddChild(sun);
        sun.LookAtFromPosition(new Vector3(-30, 55, -22), Vector3.Zero, Vector3.Up);
    }
}
