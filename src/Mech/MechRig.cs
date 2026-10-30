using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;

namespace IronFeud.Mech;

/// <summary>
/// Builds the placeholder mech node hierarchy in code and applies <see cref="MechPose"/> to it.
/// Geometry is the prototype's, mirrored into Godot's frame (forward -Z, left -X). Smaller chassis build the same
/// meshes at prototype size (leg lengths from the chassis) and scale the whole pelvis node by <see cref="MechChassis.Scale"/>.
///
/// Pelvis (top-level, world space)
///   LegRoot[i] → Hip → Knee → Ankle
///   TorsoBase (twist) → Torso → Head → CamMount
///                             → Shoulder[i] → Elbow[i]
/// </summary>
public sealed class MechRig
{
    /// <summary>Render layer for the cockpit camera's own head, which that camera culls (see <see cref="SetHeadHidden"/>).</summary>
    public const uint HeadLayer = 1u << 1;

    /// <summary>Default accent (orange); teams pass their own.</summary>
    public static readonly Color DefaultAccent = new("c46a2c");

    public sealed class LegNodes
    {
        public Node3D Root = null!, Hip = null!, Knee = null!, Ankle = null!;
        public (MeshInstance3D Outer, MeshInstance3D Rod)[] Pistons = null!;
    }

    public sealed class ArmNodes
    {
        public Node3D Shoulder = null!, Elbow = null!;
    }

    public Node3D Pelvis = null!, TorsoBase = null!, Torso = null!, Head = null!, CamMount = null!;
    public readonly LegNodes[] Legs = new LegNodes[2];
    public readonly ArmNodes[] Arms = new ArmNodes[2];
    public float Scale = 1f;

    private static readonly float[] PistonX = { -0.38f, 0.38f };

    private sealed class Mats
    {
        public readonly StandardMaterial3D Armor, ArmorDark, Frame, Accent, Chrome, Glow, Glass, Lens, Hole;

        public Mats(Color accent)
        {
            var panel = ProceduralTextures.ArmorPanel(11);
            Armor = Lit("9aa1a8", 0.25f, 0.55f, panel);
            ArmorDark = Lit("6a7077", 0.3f, 0.6f, panel);
            Frame = Lit("3e4249", 0.45f, 0.5f);
            Accent = Lit(accent.ToHtml(false), 0.2f, 0.55f);
            Chrome = Lit("c8cdd2", 0.6f, 0.25f);
            Glow = Emissive("221100", "ff7a1a", 2.2f);
            Glass = Lit("0c1820", 0.9f, 0.08f);
            Glass.EmissionEnabled = true;
            Glass.Emission = new Color("0b3a4a");
            Glass.EmissionEnergyMultiplier = 0.7f;
            Lens = Emissive("200000", "ff2a1a", 2.5f);
            Hole = Lit("0a0a0a", 0f, 1f);
        }

        private static StandardMaterial3D Lit(string color, float metal, float rough, Texture2D? tex = null) => new()
        {
            AlbedoColor = new Color(color),
            AlbedoTexture = tex,
            Metallic = metal,
            Roughness = rough,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };

        private static StandardMaterial3D Emissive(string color, string emission, float energy)
        {
            var m = Lit(color, 0f, 0.5f);
            m.EmissionEnabled = true;
            m.Emission = new Color(emission);
            m.EmissionEnergyMultiplier = energy;
            return m;
        }
    }

    public static MechRig Build(Node3D owner, MechChassis t, Color? accent = null)
    {
        float s = t.Scale;
        var r = new MechRig { Scale = s };
        var m = new Mats(accent ?? DefaultAccent);
        float l1 = t.L1 / s, l2 = t.L2 / s;
        const float halfPi = Mathf.Pi / 2f;

        r.Pelvis = Group(owner, "Pelvis", 0, t.HipHeight, 0);
        r.Pelvis.TopLevel = true;
        var pelvis = r.Pelvis;
        Box(pelvis, 2.6f, 1.1f, 1.9f, m.Frame);
        Box(pelvis, 2.2f, 0.7f, 2.1f, m.ArmorDark, 0, -0.25f, -0.05f);
        Box(pelvis, 1.0f, 1.3f, 0.45f, m.Armor, 0, -0.35f, -1.08f, -0.15f);
        Box(pelvis, 1.2f, 0.9f, 0.4f, m.ArmorDark, 0, -0.2f, 1.08f, 0.15f);
        foreach (int side in new[] { -1, 1 })
        {
            Cyl(pelvis, 0.68f, 0.68f, 0.7f, m.Frame, -side * 1.3f, -0.1f, 0, 0, 0, -halfPi);
            Cyl(pelvis, 0.42f, 0.42f, 0.8f, m.Accent, -side * 1.3f, -0.1f, 0, 0, 0, -halfPi);
        }

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var leg = new LegNodes();
            leg.Root = Group(pelvis, i == 0 ? "LegRootL" : "LegRootR", -side * t.HipWidth / s, t.HipRootY / s, 0);
            leg.Hip = Group(leg.Root, "Hip");
            Cyl(leg.Hip, 0.55f, 0.55f, 0.95f, m.Frame, 0, 0, 0, 0, 0, -halfPi);
            Box(leg.Hip, 0.9f, l1 * 0.9f, 1.0f, m.Frame, 0, -l1 * 0.5f, 0);
            Box(leg.Hip, 1.15f, l1 * 0.75f, 0.35f, m.Armor, 0, -l1 * 0.45f, -0.62f, 0.05f);
            Box(leg.Hip, 0.25f, l1 * 0.7f, 1.15f, m.ArmorDark, -side * 0.6f, -l1 * 0.48f, 0);
            Box(leg.Hip, 0.3f, 0.28f, 1.0f, m.Accent, -side * 0.72f, -0.85f, 0);

            leg.Knee = Group(leg.Hip, "Knee", 0, -l1, 0);
            Cyl(leg.Knee, 0.5f, 0.5f, 1.05f, m.Frame, 0, 0, 0, 0, 0, -halfPi);
            Box(leg.Knee, 1.0f, 0.95f, 0.5f, m.Armor, 0, 0.05f, -0.62f, -0.25f);
            Box(leg.Knee, 0.8f, l2 * 0.9f, 0.9f, m.Frame, 0, -l2 * 0.5f, 0.05f);
            Box(leg.Knee, 1.15f, l2 * 0.78f, 0.4f, m.Armor, 0, -l2 * 0.42f, -0.56f, -0.06f);
            Box(leg.Knee, 1.05f, 0.85f, 1.25f, m.ArmorDark, 0, -l2 * 0.8f, 0.1f);
            Box(leg.Knee, 0.12f, 0.55f, 0.3f, m.Glow, -side * 0.5f, -l2 * 0.8f, 0.35f);

            leg.Ankle = Group(leg.Knee, "Ankle", 0, -l2, 0);
            Cyl(leg.Ankle, 0.42f, 0.42f, 0.9f, m.Frame, 0, 0, 0, 0, 0, -halfPi);
            Box(leg.Ankle, 0.9f, 0.4f, 0.9f, m.Frame, 0, -0.1f, 0);
            Box(leg.Ankle, 1.5f, 0.45f, 1.5f, m.ArmorDark, 0, -0.37f, -0.1f);
            foreach (float tx in new[] { -0.52f, 0f, 0.52f })
            {
                Box(leg.Ankle, 0.42f, 0.38f, 1.05f, m.Armor, tx, -0.42f, -1.2f);
                Box(leg.Ankle, 0.3f, 0.18f, 0.25f, m.Frame, tx, -0.5f, -1.8f);
            }
            Box(leg.Ankle, 0.95f, 0.36f, 1.0f, m.Armor, 0, -0.43f, 0.95f, 0.08f);
            Box(leg.Ankle, 0.1f, 0.3f, 1.4f, m.Accent, -side * 0.76f, -0.38f, -0.1f);

            // Calf pistons are world-space segments so they can stretch.
            leg.Pistons = new (MeshInstance3D, MeshInstance3D)[PistonX.Length];
            for (int p = 0; p < PistonX.Length; p++)
            {
                var outer = Cyl(owner, 0.15f * s, 0.15f * s, 1f, m.Frame, segments: 10);
                var rod = Cyl(owner, 0.085f * s, 0.085f * s, 1f, m.Chrome, segments: 10);
                outer.TopLevel = rod.TopLevel = true;
                leg.Pistons[p] = (outer, rod);
            }
            r.Legs[i] = leg;
        }

        r.TorsoBase = Group(pelvis, "TorsoBase", 0, t.TorsoBaseY / s, 0);
        Cyl(r.TorsoBase, 0.75f, 0.95f, 0.8f, m.Frame, 0, 0.25f, 0, segments: 12);
        r.Torso = Group(r.TorsoBase, "Torso", 0, t.TorsoY / s, 0);
        var torso = r.Torso;
        Box(torso, 3.6f, 2.4f, 2.8f, m.Armor, 0, 1.3f, 0);
        Box(torso, 3.9f, 0.6f, 2.6f, m.ArmorDark, 0, 0.2f, 0);
        Box(torso, 2.6f, 1.5f, 0.4f, m.ArmorDark, 0, 1.55f, -1.5f, -0.12f);
        Box(torso, 0.35f, 1.5f, 0.42f, m.Accent, -0.95f, 1.55f, -1.56f, -0.12f);
        Box(torso, 0.35f, 1.5f, 0.42f, m.Accent, 0.95f, 1.55f, -1.56f, -0.12f);
        Box(torso, 3.0f, 1.6f, 0.3f, m.Frame, 0, 1.4f, 1.5f);
        for (int i = 0; i < 4; i++) Box(torso, 0.42f, 1.2f, 0.08f, m.Glow, 1.05f - i * 0.7f, 1.4f, 1.68f);
        for (int i = 0; i < 5; i++) Box(torso, 3.0f, 0.08f, 0.25f, m.Frame, 0, 0.85f + i * 0.28f, 1.75f);

        var head0 = t.HeadOffset / s;
        r.Head = Group(torso, "Head", head0.X, head0.Y, head0.Z);
        var head = r.Head;
        Box(head, 1.7f, 0.9f, 1.9f, m.ArmorDark);
        Box(head, 1.4f, 0.42f, 0.3f, m.Glass, 0, 0.05f, -0.98f, 0.2f);
        Box(head, 1.9f, 0.18f, 2.0f, m.Armor, 0, 0.5f, 0);
        Cyl(head, 0.04f, 0.04f, 1.8f, m.Chrome, 0.7f, 1.3f, 0.6f);
        Cyl(head, 0.03f, 0.03f, 1.1f, m.Chrome, 0.5f, 1.0f, 0.7f);
        r.CamMount = Group(head, "CamMount", 0, 0, -1.25f);

        var launcher = Group(torso, "Launcher", 1.3f, 2.95f, -0.1f);
        Box(launcher, 1.5f, 1.0f, 2.0f, m.Armor);
        Box(launcher, 1.55f, 0.2f, 2.05f, m.Accent, 0, -0.3f, 0);
        for (int row = 0; row < 2; row++)
        for (int c = 0; c < 3; c++)
            Cyl(launcher, 0.16f, 0.16f, 0.1f, m.Hole, 0.45f - c * 0.45f, 0.2f - row * 0.38f, -1.01f, -halfPi);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = new ArmNodes();
            var sh = t.ShoulderOffset / s;
            arm.Shoulder = Group(torso, i == 0 ? "ShoulderL" : "ShoulderR", -side * sh.X, sh.Y, sh.Z);
            Box(arm.Shoulder, 1.25f, 1.0f, 1.8f, m.Armor, -side * 0.35f, 0.3f, 0);
            Box(arm.Shoulder, 0.2f, 0.85f, 1.6f, m.Accent, -side * 1.0f, 0.25f, 0);
            Cyl(arm.Shoulder, 0.5f, 0.5f, 0.9f, m.Frame, -side * 0.2f, 0, 0, 0, 0, -halfPi);
            Box(arm.Shoulder, 0.75f, 1.9f, 0.8f, m.Frame, -side * 0.45f, -1.1f, 0);
            var el = t.ElbowOffset / s;
            arm.Elbow = Group(arm.Shoulder, "Elbow", -side * el.X, el.Y, el.Z);
            Cyl(arm.Elbow, 0.42f, 0.42f, 0.95f, m.Frame, 0, 0, 0, 0, 0, -halfPi);
            Box(arm.Elbow, 1.0f, 1.0f, 2.4f, m.ArmorDark, 0, 0, -1.0f);
            if (i == 1)
            {
                // Right arm: cannon.
                Cyl(arm.Elbow, 0.36f, 0.36f, 0.7f, m.Armor, 0, 0.05f, -2.5f, -halfPi);
                Cyl(arm.Elbow, 0.26f, 0.26f, 3.2f, m.Frame, 0, 0.05f, -3.6f, -halfPi);
                Cyl(arm.Elbow, 0.34f, 0.3f, 0.45f, m.Frame, 0, 0.05f, -5.1f, -halfPi);
                Cyl(arm.Elbow, 0.15f, 0.15f, 0.05f, m.Hole, 0, 0.05f, -5.33f, -halfPi);
            }
            else
            {
                // Left arm: beam emitter.
                Box(arm.Elbow, 1.25f, 0.85f, 1.5f, m.Armor, 0, 0.1f, -2.6f);
                Box(arm.Elbow, 1.3f, 0.12f, 1.1f, m.Accent, 0, 0.58f, -2.5f);
                foreach (float lx in new[] { -0.38f, 0f, 0.38f })
                    Cyl(arm.Elbow, 0.14f, 0.14f, 0.12f, m.Lens, lx, 0.1f, -3.38f, -halfPi);
            }
            r.Arms[i] = arm;
        }

        return r;
    }

    /// <summary>Moves the head meshes onto <see cref="HeadLayer"/> so a camera sitting inside this head can cull them.</summary>
    public void SetHeadHidden(bool hidden)
    {
        foreach (var child in Head.GetChildren())
            if (child is VisualInstance3D vi) vi.Layers = hidden ? HeadLayer : 1u;
    }

    public void Apply(MechPose pose)
    {
        Pelvis.Transform = new Transform3D(new Basis(pose.PelvisRotation.ToGodot()).Scaled(Vector3.One * Scale), pose.PelvisPosition.ToGodot());
        TorsoBase.Quaternion = pose.TorsoRotation.ToGodot();
        Head.Quaternion = pose.HeadRotation.ToGodot();
        for (int i = 0; i < 2; i++)
        {
            Arms[i].Shoulder.Quaternion = pose.Shoulders[i].ToGodot();
            Arms[i].Elbow.Quaternion = pose.Elbows[i].ToGodot();

            var leg = Legs[i];
            var lp = pose.Legs[i];
            leg.Hip.Quaternion = lp.Hip.ToGodot();
            leg.Knee.Quaternion = lp.Knee.ToGodot();
            leg.Ankle.Quaternion = lp.Ankle.ToGodot();

            Transform3D hip = leg.Hip.GlobalTransform, knee = leg.Knee.GlobalTransform;
            for (int p = 0; p < leg.Pistons.Length; p++)
            {
                float px = PistonX[p];
                Vector3 a = hip * new Vector3(px, -0.6f, 0.68f);
                Vector3 b = knee * new Vector3(px, -1.35f, 0.62f);
                Vector3 dir = (b - a).Normalized();
                leg.Pistons[p].Outer.Transform = GodotConv.Segment(a, a + dir * (1.7f * Scale));
                leg.Pistons[p].Rod.Transform = GodotConv.Segment(a + dir * (1.5f * Scale), b);
            }
        }
    }

    private static Node3D Group(Node3D parent, string name, float x = 0, float y = 0, float z = 0)
    {
        var g = new Node3D { Name = name, Position = new Vector3(x, y, z) };
        parent.AddChild(g);
        return g;
    }

    private static MeshInstance3D Box(Node3D parent, float w, float h, float d, Material mat,
        float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0) =>
        Add(parent, new BoxMesh { Size = new Vector3(w, h, d) }, mat, x, y, z, rx, ry, rz);

    private static MeshInstance3D Cyl(Node3D parent, float rTop, float rBottom, float h, Material mat,
        float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, int segments = 18) =>
        Add(parent, new CylinderMesh { TopRadius = rTop, BottomRadius = rBottom, Height = h, RadialSegments = segments, Rings = 1 },
            mat, x, y, z, rx, ry, rz);

    private static MeshInstance3D Add(Node3D parent, Mesh mesh, Material mat, float x, float y, float z, float rx, float ry, float rz)
    {
        var mi = new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = mat,
            Position = new Vector3(x, y, z),
            RotationOrder = EulerOrder.Xyz,
            Rotation = new Vector3(rx, ry, rz),
        };
        parent.AddChild(mi);
        return mi;
    }
}
