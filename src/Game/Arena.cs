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
        BuildGround(root, seed, map.Terrain);

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
                Position = new Vector3(p.X, map.Height(p.X, p.Y) + s * 0.4f, p.Y),
                Rotation = new Vector3(rng.NextFloat() * 3f, rng.NextFloat() * 3f, 0),
                Scale = new Vector3(s * (1f + rng.NextFloat()), s, s * (1f + rng.NextFloat())),
            });
        }

        if (map.Radius > 0f) BuildBoundary(root, map);
    }

    /// <summary>The arena edge: a 2 m band draped over the ground.</summary>
    private static void BuildBoundary(Node3D root, ArenaMap map)
    {
        const int segments = 192;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.TriangleStrip);
        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Tau * i / segments, c = Mathf.Cos(a), s = Mathf.Sin(a);
            foreach (float r in new[] { map.Radius + 4f, map.Radius + 6f })
                st.AddVertex(new Vector3(c * r, map.Height(c * r, s * r) + 0.15f, s * r));
        }
        root.AddChild(new MeshInstance3D
        {
            Name = "Boundary",
            Mesh = st.Commit(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.54f, 0.16f, 0.35f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            },
        });
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

    /// <summary>
    /// Flat ground to the horizon at y = 0, plus the heightfield as a mesh. With terrain the flat part is a frame
    /// around the grid (the grid fades to 0 at its edge): a full plane would cover every valley below y = 0.
    /// </summary>
    private static void BuildGround(Node3D root, uint seed, HeightField? terrain)
    {
        var groundTex = ProceduralTextures.Ground(seed);
        if (terrain == null)
        {
            FlatGround(root, groundTex, 0f, 0f, GroundSize, GroundSize);
            return;
        }
        float e = terrain.HalfExtent, outer = GroundSize / 2f, w = outer - e;
        FlatGround(root, groundTex, -(e + w / 2f), 0f, w, GroundSize);
        FlatGround(root, groundTex, e + w / 2f, 0f, w, GroundSize);
        FlatGround(root, groundTex, 0f, -(e + w / 2f), 2f * e, w);
        FlatGround(root, groundTex, 0f, e + w / 2f, 2f * e, w);

        // Grid mesh with smooth normals; UVs in world metres / 10 to match the plane's tiling.
        int n = terrain.N;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int iz = 0; iz < n; iz++)
        for (int ix = 0; ix < n; ix++)
        {
            float x = terrain.Origin + ix * terrain.Cell, z = terrain.Origin + iz * terrain.Cell;
            st.SetNormal(terrain.NormalAt(x, z, terrain.Cell).ToGodot());
            st.SetUV(new Vector2(x / 10f, z / 10f));
            st.AddVertex(new Vector3(x, terrain.Height(x, z), z));
        }
        for (int iz = 0; iz < n - 1; iz++)
        for (int ix = 0; ix < n - 1; ix++)
        {
            int a = iz * n + ix, b = a + 1, c = a + n, d = c + 1;
            st.AddIndex(a); st.AddIndex(b); st.AddIndex(c);
            st.AddIndex(b); st.AddIndex(d); st.AddIndex(c);
        }
        root.AddChild(new MeshInstance3D
        {
            Name = "Terrain",
            Mesh = st.Commit(),
            MaterialOverride = GroundMaterial(groundTex, 1f),
        });
    }

    /// <summary>A flat ground rectangle; world-space UVs (triplanar) so pieces tile seamlessly.</summary>
    private static void FlatGround(Node3D root, Texture2D tex, float x, float z, float w, float d)
    {
        var mat = GroundMaterial(tex, 0.1f);
        mat.Uv1Triplanar = true;
        mat.Uv1WorldTriplanar = true;
        root.AddChild(new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(w, d) },
            Position = new Vector3(x, 0f, z),
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private static StandardMaterial3D GroundMaterial(Texture2D tex, float uvScale) => new()
    {
        AlbedoTexture = tex,
        Roughness = 0.95f,
        Uv1Scale = new Vector3(uvScale, uvScale, 1f),
        TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
    };

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
