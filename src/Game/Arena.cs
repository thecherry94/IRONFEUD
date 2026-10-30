using Godot;
using IronFeud.Core;
using IronFeud.Logic.Battle;
using IronFeud.Logic.Core;

namespace IronFeud.Game;

/// <summary>
/// Renders an <see cref="ArenaMap"/>: environment, ground, its blocks, cosmetic rocks and the boundary ring, plus the
/// No Man's Land dressing (mud, wire pickets along the trench parapets, dead tree stumps).
/// Collision and line of sight come from the map itself, so nothing here has a physics body.
/// </summary>
public static class Arena
{
    private const float GroundSize = 2000f;

    public static void Build(Node3D root, ArenaMap map, uint seed)
    {
        var rng = new DeterministicRng(seed);
        bool mud = map.Theme == ArenaTheme.NoMansLand;
        BuildEnvironment(root, mud ? new Color("7f7a70") : new Color("8d98a0"));
        BuildGround(root, seed, map.Terrain, mud);

        var panel = ProceduralTextures.ArmorPanel(seed);
        var propMats = mud
            ? new[] { Prop("8a8984", 0.95f, 0f, panel), Prop("77766f", 0.95f, 0f, panel), Prop("6b655c", 0.95f, 0f, panel) }
            : new[] { Prop("6d675e", 0.9f, 0f, panel), Prop("5a5f63", 0.8f, 0.2f, panel), Prop("7b6f60", 0.95f, 0f, panel) };

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
        if (mud)
        {
            BuildWire(root, map);
            BuildStumps(root, map, rng);
        }
    }

    /// <summary>Wire pickets along the crest of each trench's parapet: posts every 6 m, two strands between them.</summary>
    private static void BuildWire(Node3D root, ArenaMap map)
    {
        var posts = new System.Collections.Generic.List<Transform3D>();
        var strands = new System.Collections.Generic.List<Transform3D>();
        foreach (var line in map.Trenches)
        {
            var front = new Vector2(0f, -Mathf.Sign(line[0].Y));
            Vector3? prev = null;
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 a = new(line[i].X, line[i].Y), b = new(line[i + 1].X, line[i + 1].Y);
                Vector2 n = new Vector2(b.Y - a.Y, a.X - b.X).Normalized();
                if (n.Dot(front) < 0f) n = -n;
                float len = a.DistanceTo(b);
                for (float t = 0f; t < len; t += 6f)
                {
                    Vector2 p = a + (b - a) * (t / len) + n * 10.8f; // parapet crest, 1.8 half-widths out
                    var foot = new Vector3(p.X, map.Height(p.X, p.Y), p.Y);
                    posts.Add(new Transform3D(Basis.Identity.Rotated(Vector3.Right, (posts.Count % 3 - 1) * 0.15f), foot + Vector3.Up * 1.1f));
                    if (prev is { } q)
                        foreach (float h in new[] { 0.8f, 1.8f })
                            strands.Add(GodotConv.Segment(q + Vector3.Up * h, foot + Vector3.Up * h * 1.05f));
                    prev = foot;
                }
            }
        }
        var wireMat = new StandardMaterial3D { AlbedoColor = new Color("3a332c"), Roughness = 0.9f, Metallic = 0.4f };
        AddMulti(root, "WirePosts", new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.1f, Height = 2.2f, RadialSegments = 5, Rings = 1 }, wireMat, posts);
        AddMulti(root, "Wire", new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.025f, Height = 1f, RadialSegments = 4, Rings = 1 }, wireMat, strands);
    }

    /// <summary>Shattered tree stumps: tilted, splintered trunks, some with a broken limb.</summary>
    private static void BuildStumps(Node3D root, ArenaMap map, DeterministicRng rng)
    {
        var stumps = new System.Collections.Generic.List<Transform3D>();
        for (int i = 0; i < 140; i++)
        {
            float a = rng.NextFloat() * Mathf.Tau, r = Mathf.Sqrt(rng.NextFloat()) * (map.Radius + 30f);
            float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
            if (map.InsideAnyBlock(x, z, 1f)) continue;
            float h = 1.5f + rng.NextFloat() * rng.NextFloat() * 7f, w = 0.6f + rng.NextFloat() * 0.8f;
            var basis = new Basis(Vector3.Up, rng.NextFloat() * Mathf.Tau) * new Basis(Vector3.Right, (rng.NextFloat() - 0.5f) * 0.5f);
            stumps.Add(new Transform3D(basis.Scaled(new Vector3(w, h, w)), new Vector3(x, map.Height(x, z) + h * 0.45f, z)));
            if (h > 4f && rng.NextFloat() < 0.5f)
            {
                var limb = basis * new Basis(Vector3.Forward, 0.9f + rng.NextFloat() * 0.5f);
                stumps.Add(new Transform3D(limb.Scaled(new Vector3(w * 0.4f, h * 0.4f, w * 0.4f)),
                    new Vector3(x, map.Height(x, z) + h * 0.7f, z) + limb.Y * h * 0.2f));
            }
        }
        var bark = new StandardMaterial3D { AlbedoColor = new Color("3b342d"), Roughness = 1f };
        AddMulti(root, "Stumps", new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.32f, Height = 1f, RadialSegments = 6, Rings = 1 }, bark, stumps);
    }

    private static void AddMulti(Node3D root, string name, Mesh mesh, Material mat, System.Collections.Generic.List<Transform3D> transforms)
    {
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = transforms.Count };
        for (int i = 0; i < transforms.Count; i++) mm.SetInstanceTransform(i, transforms[i]);
        root.AddChild(new MultiMeshInstance3D { Name = name, Multimesh = mm, MaterialOverride = mat });
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
    private static void BuildGround(Node3D root, uint seed, HeightField? terrain, bool mud)
    {
        var groundTex = ProceduralTextures.Ground(seed);
        var tint = mud ? new Color(0.78f, 0.72f, 0.64f) : Colors.White;
        if (terrain == null)
        {
            FlatGround(root, groundTex, 0f, 0f, GroundSize, GroundSize, tint);
            return;
        }
        float e = terrain.HalfExtent, outer = GroundSize / 2f, w = outer - e;
        FlatGround(root, groundTex, -(e + w / 2f), 0f, w, GroundSize, tint);
        FlatGround(root, groundTex, e + w / 2f, 0f, w, GroundSize, tint);
        FlatGround(root, groundTex, 0f, -(e + w / 2f), 2f * e, w, tint);
        FlatGround(root, groundTex, 0f, e + w / 2f, 2f * e, w, tint);

        // Grid mesh with smooth normals; UVs in world metres / 10 to match the plane's tiling.
        int n = terrain.N;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int iz = 0; iz < n; iz++)
        for (int ix = 0; ix < n; ix++)
        {
            float x = terrain.Origin + ix * terrain.Cell, z = terrain.Origin + iz * terrain.Cell;
            var normal = terrain.NormalAt(x, z, terrain.Cell);
            float h = terrain.Height(x, z);
            // Darker in hollows (wet mud in craters and trenches) and on steep faces, so the shapes read from afar.
            float shade = Mathf.Lerp(mud ? 0.5f : 0.8f, 1f, Mathf.SmoothStep(-6f, 3f, h)) * (1f - 0.25f * (1f - normal.Y));
            st.SetColor(new Color(tint.R * shade, tint.G * shade, tint.B * shade));
            st.SetNormal(normal.ToGodot());
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
        var terrainMat = GroundMaterial(groundTex, 1f);
        terrainMat.VertexColorUseAsAlbedo = true;
        root.AddChild(new MeshInstance3D
        {
            Name = "Terrain",
            Mesh = st.Commit(),
            MaterialOverride = terrainMat,
        });
    }

    /// <summary>A flat ground rectangle; world-space UVs (triplanar) so pieces tile seamlessly.</summary>
    private static void FlatGround(Node3D root, Texture2D tex, float x, float z, float w, float d, Color tint)
    {
        var mat = GroundMaterial(tex, 0.1f);
        mat.AlbedoColor = tint;
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

    private static void BuildEnvironment(Node3D root, Color fog)
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky
            {
                SkyMaterial = new ProceduralSkyMaterial
                {
                    SkyTopColor = new Color("aab4bc"),
                    SkyHorizonColor = fog,
                    GroundHorizonColor = fog,
                    GroundBottomColor = new Color("5a4e40"),
                },
            },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 1.0f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.05f,
            FogEnabled = true,
            FogMode = Godot.Environment.FogModeEnum.Depth,
            FogLightColor = fog,
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
