using System.Collections.Generic;
using Godot;
using IronFeud.Core;
using IronFeud.Logic.Mech;
using IronFeud.Logic.Mech.Weapons;

namespace IronFeud.Mech;

/// <summary>
/// Builds a mech's node hierarchy in code and applies <see cref="MechPose"/> to it. The joint hierarchy is the same
/// for every model (positions from the chassis); each <see cref="MechModel"/> dresses it with its own meshes.
/// Meshes are built at the model's medium size (leg lengths from the chassis) and the whole pelvis node is scaled by
/// <see cref="MechChassis.Scale"/>. Godot frame: forward -Z, left -X.
///
/// Pelvis (top-level, world space)
///   LegRoot[i] → Hip → Knee → [Hock, digitigrade] → Ankle
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

        /// <summary>Digitigrade legs only.</summary>
        public Node3D? Hock;
    }

    public sealed class ArmNodes
    {
        public Node3D Shoulder = null!, Elbow = null!;
    }

    /// <summary>
    /// A hydraulic ram between two points on different bones: a fixed-length cylinder from A toward B and a rod that
    /// stretches the rest of the way. World-space, so the stretch needs no bone scaling.
    /// </summary>
    private sealed class Piston
    {
        public MeshInstance3D Outer = null!, Rod = null!;
        public Node3D A = null!, B = null!;
        public Vector3 LocalA, LocalB;
        public float OuterLength;
    }

    public Node3D Pelvis = null!, TorsoBase = null!, Torso = null!, Head = null!, CamMount = null!;
    public readonly LegNodes[] Legs = new LegNodes[2];
    public readonly ArmNodes[] Arms = new ArmNodes[2];
    public float Scale = 1f;

    private readonly List<Piston> _pistons = new();
    private Node3D _owner = null!;
    private Mats _m = null!;

    private sealed class Mats
    {
        public readonly Material Armor, ArmorDark, Frame, Accent;
        public readonly StandardMaterial3D Chrome, Glow, Glass, Lens, Hole;

        private static Shader? _armorShader;
        private static Texture2D? _noise;

        public Mats(Color accent)
        {
            Armor = Painted("6f757a", 0.55f, wear: 0.45f, grime: 0.45f);
            ArmorDark = Painted("4c5156", 0.6f, wear: 0.5f, grime: 0.5f);
            Frame = Painted("2e3135", 0.5f, wear: 0.7f, grime: 0.6f, metallic: 0.45f, seams: 0f);
            Accent = Painted(accent.ToHtml(false), 0.5f, wear: 0.6f, grime: 0.35f);
            Chrome = Lit("c8cdd2", 0.6f, 0.25f);
            Glow = Emissive("221100", "ff7a1a", 2.2f);
            Glass = Lit("0c1820", 0.9f, 0.08f);
            Glass.EmissionEnabled = true;
            Glass.Emission = new Color("0b3a4a");
            Glass.EmissionEnergyMultiplier = 0.7f;
            Lens = Emissive("200000", "ff2a1a", 2.5f);
            Hole = Lit("0a0a0a", 0f, 1f);
        }

        /// <summary>Painted armour (mech_armor.gdshader): bevel highlights, chipped edges, panel seams, grime.</summary>
        private static ShaderMaterial Painted(string color, float rough, float wear, float grime, float metallic = 0.1f, float seams = 1f)
        {
            _armorShader ??= GD.Load<Shader>("res://src/Mech/mech_armor.gdshader");
            _noise ??= ProceduralTextures.Noise(7);
            var m = new ShaderMaterial { Shader = _armorShader };
            m.SetShaderParameter("paint_color", new Color(color));
            m.SetShaderParameter("paint_roughness", rough);
            m.SetShaderParameter("paint_metallic", metallic);
            m.SetShaderParameter("wear", wear);
            m.SetShaderParameter("grime", grime);
            m.SetShaderParameter("panel_seams", seams);
            m.SetShaderParameter("noise", _noise);
            return m;
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

    private const float HalfPi = Mathf.Pi / 2f;

    public static MechRig Build(Node3D owner, MechChassis t, Color? accent = null)
    {
        float s = t.Scale;
        var r = new MechRig { Scale = s, _owner = owner, _m = new Mats(accent ?? DefaultAccent) };

        // Joint hierarchy, positions in the model's medium-size units.
        r.Pelvis = Group(owner, "Pelvis", 0, t.HipHeight, 0);
        r.Pelvis.TopLevel = true;
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var leg = new LegNodes();
            leg.Root = Group(r.Pelvis, i == 0 ? "LegRootL" : "LegRootR", -side * t.HipWidth / s, t.HipRootY / s, 0);
            leg.Hip = Group(leg.Root, "Hip");
            leg.Knee = Group(leg.Hip, "Knee", 0, -t.L1 / s, 0);
            if (t.Legs == LegLayout.Digitigrade)
            {
                leg.Hock = Group(leg.Knee, "Hock", 0, -t.L2 / s, 0);
                leg.Ankle = Group(leg.Hock, "Ankle", 0, -t.L3 / s, 0);
            }
            else leg.Ankle = Group(leg.Knee, "Ankle", 0, -t.L2 / s, 0);
            r.Legs[i] = leg;
        }
        r.TorsoBase = Group(r.Pelvis, "TorsoBase", 0, t.TorsoBaseY / s, 0);
        r.Torso = Group(r.TorsoBase, "Torso", 0, t.TorsoY / s, 0);
        var head = t.HeadOffset / s;
        r.Head = Group(r.Torso, "Head", head.X, head.Y, head.Z);
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            Vector3 sh = t.ShoulderOffset.ToGodot() / s, el = t.ElbowOffset.ToGodot() / s;
            var arm = new ArmNodes();
            arm.Shoulder = Group(r.Torso, i == 0 ? "ShoulderL" : "ShoulderR", -side * sh.X, sh.Y, sh.Z);
            arm.Elbow = Group(arm.Shoulder, "Elbow", -side * el.X, el.Y, el.Z);
            r.Arms[i] = arm;
        }

        switch (t.Model)
        {
            case MechModel.Sicklefoot: r.DressSicklefoot(t); break;
            case MechModel.Bodkin: r.DressBodkin(t); break;
            case MechModel.Brigandine: r.DressBrigandine(t); break;
            case MechModel.Harrow: r.DressHarrow(t); break;
            case MechModel.Barbican: r.DressBarbican(t); break;
            default: r.DressBascinet(t); break;
        }
        if (t.TorsoWeapon is { } rack) r.BuildRack(r.Torso, rack, t.TorsoMount.ToGodot() / s);
        if (t.PointDefense > 0) r.BuildPointDefense(r.Torso, t);
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
            if (leg.Hock != null) leg.Hock.Quaternion = lp.Hock.ToGodot();
            leg.Ankle.Quaternion = lp.Ankle.ToGodot();
        }

        foreach (var p in _pistons)
        {
            Vector3 a = p.A.GlobalTransform * p.LocalA, b = p.B.GlobalTransform * p.LocalB;
            Vector3 dir = (b - a).Normalized();
            float outer = p.OuterLength * Scale;
            p.Outer.Transform = GodotConv.Segment(a, a + dir * outer);
            p.Rod.Transform = GodotConv.Segment(a + dir * (outer - 0.2f * Scale), b);
        }
    }

    // ---------- Bascinet: the prototype's upright biped ----------

    private void DressBascinet(MechChassis t)
    {
        var m = _m;
        var pelvis = Pelvis;
        Box(pelvis, 2.6f, 1.1f, 1.9f, m.Frame);
        Box(pelvis, 2.2f, 0.7f, 2.1f, m.ArmorDark, 0, -0.25f, -0.05f);
        Box(pelvis, 1.0f, 1.3f, 0.45f, m.Armor, 0, -0.35f, -1.08f, -0.15f);
        Box(pelvis, 1.2f, 0.9f, 0.4f, m.ArmorDark, 0, -0.2f, 1.08f, 0.15f);
        foreach (int side in new[] { -1, 1 })
        {
            Cyl(pelvis, 0.68f, 0.68f, 0.7f, m.Frame, -side * 1.3f, -0.1f, 0, 0, 0, -HalfPi);
            Cyl(pelvis, 0.42f, 0.42f, 0.8f, m.Accent, -side * 1.3f, -0.1f, 0, 0, 0, -HalfPi);
        }

        DressPlantigradeLegs(t, 1f, 1f);

        TorsoBase.AddChild(NewCyl(0.75f, 0.95f, 0.8f, m.Frame, 0, 0.25f, 0, segments: 12));
        var torso = Torso;
        Box(torso, 3.6f, 2.4f, 2.8f, m.Armor, 0, 1.3f, 0);
        Box(torso, 3.9f, 0.6f, 2.6f, m.ArmorDark, 0, 0.2f, 0);
        Box(torso, 2.6f, 1.5f, 0.4f, m.ArmorDark, 0, 1.55f, -1.5f, -0.12f);
        Box(torso, 0.35f, 1.5f, 0.42f, m.Accent, -0.95f, 1.55f, -1.56f, -0.12f);
        Box(torso, 0.35f, 1.5f, 0.42f, m.Accent, 0.95f, 1.55f, -1.56f, -0.12f);
        Box(torso, 3.0f, 1.6f, 0.3f, m.Frame, 0, 1.4f, 1.5f);
        for (int i = 0; i < 4; i++) Box(torso, 0.42f, 1.2f, 0.08f, m.Glow, 1.05f - i * 0.7f, 1.4f, 1.68f);
        for (int i = 0; i < 5; i++) Box(torso, 3.0f, 0.08f, 0.25f, m.Frame, 0, 0.85f + i * 0.28f, 1.75f);

        var head = Head;
        Box(head, 1.7f, 0.9f, 1.9f, m.ArmorDark);
        Box(head, 1.4f, 0.42f, 0.3f, m.Glass, 0, 0.05f, -0.98f, 0.2f);
        Box(head, 1.9f, 0.18f, 2.0f, m.Armor, 0, 0.5f, 0);
        Cyl(head, 0.04f, 0.04f, 1.8f, m.Chrome, 0.7f, 1.3f, 0.6f);
        Cyl(head, 0.03f, 0.03f, 1.1f, m.Chrome, 0.5f, 1.0f, 0.7f);
        CamMount = Group(head, "CamMount", 0, 0, -1.25f);

        var launcher = Group(torso, "Launcher", 1.3f, 2.95f, -0.1f);
        Box(launcher, 1.5f, 1.0f, 2.0f, m.Armor);
        Box(launcher, 1.55f, 0.2f, 2.05f, m.Accent, 0, -0.3f, 0);
        for (int row = 0; row < 2; row++)
        for (int c = 0; c < 3; c++)
            Cyl(launcher, 0.16f, 0.16f, 0.1f, m.Hole, 0.45f - c * 0.45f, 0.2f - row * 0.38f, -1.01f, -HalfPi);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            // Pauldron and flange: the chassis' cover plates, drawn exactly where the hit test has them.
            DrawCover(t, i);
            Box(arm.Shoulder, 0.2f, 0.85f, 1.6f, m.Accent, -side * 1.0f, 0.25f, 0);
            Cyl(arm.Shoulder, 0.5f, 0.5f, 0.9f, m.Frame, -side * 0.2f, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 0.75f, 1.9f, 0.8f, m.Frame, -side * 0.45f, -1.1f, 0);
            Cyl(arm.Elbow, 0.42f, 0.42f, 0.95f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Elbow, 1.0f, 1.0f, 2.4f, m.ArmorDark, 0, 0, -1.0f);
            BuildWeapon(arm.Elbow, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    /// <summary>
    /// Plantigrade legs (thigh, shin, flat armoured foot, calf pistons), as the Bascinet's at <paramref name="w"/> = 1:
    /// <paramref name="w"/> thickens thigh, shin and joints, <paramref name="f"/> enlarges the feet.
    /// </summary>
    private void DressPlantigradeLegs(MechChassis t, float w, float f)
    {
        var m = _m;
        float l1 = t.L1 / Scale, l2 = t.L2 / Scale;
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var leg = Legs[i];
            Cyl(leg.Hip, 0.55f * w, 0.55f * w, 0.95f * w, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Hip, 0.9f * w, l1 * 0.9f, 1.0f * w, m.Frame, 0, -l1 * 0.5f, 0);
            Box(leg.Hip, 1.15f * w, l1 * 0.75f, 0.35f, m.Armor, 0, -l1 * 0.45f, -0.62f * w, 0.05f);
            Box(leg.Hip, 0.25f, l1 * 0.7f, 1.15f * w, m.ArmorDark, -side * 0.6f * w, -l1 * 0.48f, 0);
            Box(leg.Hip, 0.3f, 0.28f, 1.0f * w, m.Accent, -side * 0.72f * w, -0.85f, 0);

            Cyl(leg.Knee, 0.5f * w, 0.5f * w, 1.05f * w, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Knee, 1.0f * w, 0.95f, 0.5f, m.Armor, 0, 0.05f, -0.62f * w, -0.25f);
            Box(leg.Knee, 0.8f * w, l2 * 0.9f, 0.9f * w, m.Frame, 0, -l2 * 0.5f, 0.05f);
            Box(leg.Knee, 1.15f * w, l2 * 0.78f, 0.4f, m.Armor, 0, -l2 * 0.42f, -0.56f * w, -0.06f);
            Box(leg.Knee, 1.05f * w, 0.85f, 1.25f * w, m.ArmorDark, 0, -l2 * 0.8f, 0.1f);
            Box(leg.Knee, 0.12f, 0.55f, 0.3f, m.Glow, -side * 0.5f * w, -l2 * 0.8f, 0.35f * w);

            Cyl(leg.Ankle, 0.42f * w, 0.42f * w, 0.9f * w, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Ankle, 0.9f * w, 0.4f, 0.9f * w, m.Frame, 0, -0.1f, 0);
            Box(leg.Ankle, 1.5f * f, 0.45f, 1.5f * f, m.ArmorDark, 0, -0.37f, -0.1f * f);
            foreach (float tx in new[] { -0.52f * f, 0f, 0.52f * f })
            {
                Box(leg.Ankle, 0.42f * f, 0.38f, 1.05f * f, m.Armor, tx, -0.42f, -1.2f * f);
                Box(leg.Ankle, 0.3f * f, 0.18f, 0.25f, m.Frame, tx, -0.5f, -1.8f * f);
            }
            Box(leg.Ankle, 0.95f * f, 0.36f, 1.0f * f, m.Armor, 0, -0.43f, 0.95f * f, 0.08f);
            Box(leg.Ankle, 0.1f, 0.3f, 1.4f * f, m.Accent, -side * 0.76f * f, -0.38f, -0.1f * f);

            // Calf pistons from the back of the thigh to the back of the shin.
            foreach (float px in new[] { -0.38f * w, 0.38f * w })
                AddPiston(leg.Hip, new Vector3(px, -0.6f, 0.68f * w), leg.Knee, new Vector3(px, -1.35f, 0.62f * w), 0.15f * w, 0.085f * w, 1.7f);
        }
    }

    /// <summary>The chassis' cover plates on arm <paramref name="i"/>'s shoulder (0 = left), where the hit test has them.</summary>
    private void DrawCover(MechChassis t, int i)
    {
        for (int k = 0; k < t.ShoulderCover.Length; k++)
        {
            var c = t.ShoulderCover[k];
            Vector3 size = c.Size.ToGodot() / Scale, at = c.Center.ToGodot() / Scale;
            Box(Arms[i].Shoulder, size.X, size.Y, size.Z, k == 0 ? _m.Armor : _m.ArmorDark, i == 0 ? at.X : -at.X, at.Y, at.Z);
        }
    }

    // ---------- Bodkin: light jumper ----------

    /// <summary>
    /// An upright armoured drum on tall "Z" legs, the cockpit a visor block on its front face, jump-jet nozzles on the
    /// back, thin arms whose long forearm housings carry the barrels forward past the body.
    /// </summary>
    private void DressBodkin(MechChassis t)
    {
        var m = _m;
        float s = Scale, l1 = t.L1 / s, l2 = t.L2 / s, l3 = t.L3 / s;
        var pelvis = Pelvis;
        Box(pelvis, 1.7f, 0.8f, 1.3f, m.Frame);
        Box(pelvis, 1.4f, 0.3f, 1.5f, m.ArmorDark, 0, -0.45f, 0);
        foreach (int side in new[] { -1, 1 })
            Cyl(pelvis, 0.45f, 0.45f, 0.5f, m.Accent, -side * 0.95f, -0.05f, 0, 0, 0, -HalfPi);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var leg = Legs[i];
            var hock = leg.Hock!;
            Cyl(leg.Hip, 0.4f, 0.4f, 0.6f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Hip, 0.6f, l1 * 0.95f, 0.75f, m.Armor, 0, -l1 * 0.5f, 0);
            Box(leg.Hip, 0.15f, l1 * 0.6f, 0.7f, m.Accent, -side * 0.35f, -l1 * 0.5f, 0);
            Cyl(leg.Knee, 0.36f, 0.36f, 0.7f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Knee, 0.38f, l2 * 0.95f, 0.42f, m.Frame, 0, -l2 * 0.5f, 0);
            Box(leg.Knee, 0.55f, l2 * 0.7f, 0.18f, m.Armor, 0, -l2 * 0.45f, -0.28f);
            Cyl(hock, 0.3f, 0.3f, 0.6f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(hock, 0.32f, l3 * 0.95f, 0.36f, m.Frame, 0, -l3 * 0.5f, 0);
            Box(hock, 0.4f, l3 * 0.5f, 0.16f, m.ArmorDark, 0, -l3 * 0.45f, 0.24f);
            var foot = leg.Ankle;
            Cyl(foot, 0.25f, 0.25f, 0.5f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            foreach (float yaw in new[] { -0.3f, 0.3f })
            {
                var toe = Group(foot, "Toe", 0, -0.18f, -0.15f);
                toe.Rotation = new Vector3(0, yaw, 0);
                Box(toe, 0.24f, 0.2f, 1.1f, m.Armor, 0, 0, -0.5f);
            }
            Box(foot, 0.22f, 0.18f, 0.6f, m.Armor, 0, -0.2f, 0.35f, -0.15f);
            AddPiston(leg.Knee, new Vector3(0, -l2 * 0.7f, -0.25f), hock, new Vector3(0, -l3 * 0.6f, -0.2f), 0.09f, 0.055f, 0.8f);
        }

        TorsoBase.AddChild(NewCyl(0.65f, 0.75f, 0.45f, m.Frame, 0, 0.2f, 0, segments: 12));
        var torso = Torso;
        Cyl(torso, 1.45f, 1.6f, 2.2f, m.Armor, 0, 1.0f, 0, segments: 16);
        Cyl(torso, 1.25f, 1.45f, 0.35f, m.ArmorDark, 0, 2.25f, 0, segments: 16);
        Box(torso, 2.0f, 0.5f, 0.3f, m.Accent, 0, 0.35f, -1.5f);
        foreach (int side in new[] { -1, 1 })
        {
            Cyl(torso, 0.32f, 0.42f, 0.9f, m.Frame, side * 0.6f, 0.9f, 1.55f, 0.35f);
            Cyl(torso, 0.22f, 0.22f, 0.06f, m.Glow, side * 0.6f, 0.5f, 1.7f, 0.35f);
        }
        Cyl(torso, 0.03f, 0.03f, 1.4f, m.Chrome, 0.7f, 2.9f, 0.5f);

        var head = Head;
        Box(head, 1.25f, 0.75f, 0.7f, m.ArmorDark);
        Box(head, 1.05f, 0.28f, 0.2f, m.Glass, 0, 0.05f, -0.36f, 0.15f);
        Box(head, 1.35f, 0.15f, 0.8f, m.Armor, 0, 0.42f, 0);
        CamMount = Group(head, "CamMount", 0, 0.05f, -0.45f);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            Cyl(arm.Shoulder, 0.42f, 0.42f, 0.7f, m.Frame, -side * 0.1f, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 0.85f, 0.6f, 1.0f, m.Armor, -side * 0.15f, 0.3f, 0);
            Box(arm.Shoulder, 0.38f, 1.4f, 0.42f, m.Frame, -side * 0.3f, -0.75f, 0);
            Cyl(arm.Elbow, 0.3f, 0.3f, 0.6f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Elbow, 0.6f, 0.6f, 1.9f, m.ArmorDark, 0, 0, -0.75f);
            Box(arm.Elbow, 0.64f, 0.12f, 1.4f, m.Accent, 0, 0.33f, -0.8f);
            BuildWeapon(arm.Elbow, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    // ---------- Brigandine: medium hull-down shooter ----------

    /// <summary>
    /// A compact chest under a high, level shoulder bar that spans the whole width; the cockpit is a dark visor tucked
    /// under the bar's centre, the arms hang from its ends and their forearm guns ride high.
    /// </summary>
    private void DressBrigandine(MechChassis t)
    {
        var m = _m;
        var pelvis = Pelvis;
        Box(pelvis, 2.4f, 1.0f, 1.8f, m.Frame);
        Box(pelvis, 2.0f, 0.6f, 2.0f, m.ArmorDark, 0, -0.25f, 0);
        foreach (int side in new[] { -1, 1 })
            Cyl(pelvis, 0.6f, 0.6f, 0.6f, m.Accent, -side * 1.2f, -0.1f, 0, 0, 0, -HalfPi);
        DressPlantigradeLegs(t, 0.95f, 0.95f);

        TorsoBase.AddChild(NewCyl(0.7f, 0.9f, 0.7f, m.Frame, 0, 0.25f, 0, segments: 12));
        var torso = Torso;
        Box(torso, 2.9f, 2.0f, 2.5f, m.Armor, 0, 1.0f, 0);
        Box(torso, 3.1f, 0.5f, 2.3f, m.ArmorDark, 0, 0.1f, 0);
        Box(torso, 2.2f, 1.1f, 0.35f, m.ArmorDark, 0, 0.9f, -1.35f, -0.1f);
        // The shoulder bar: one level beam across the top, a pillar up to it from each side of the chest.
        Box(torso, 6.4f, 0.75f, 1.6f, m.Armor, 0, 2.75f, 0.2f);
        Box(torso, 6.5f, 0.15f, 1.65f, m.Accent, 0, 3.17f, 0.2f);
        foreach (int side in new[] { -1, 1 })
            Box(torso, 0.6f, 1.2f, 1.2f, m.Frame, side * 1.25f, 2.0f, 0.3f);
        for (int i = 0; i < 4; i++) Box(torso, 0.4f, 0.9f, 0.08f, m.Glow, 0.9f - i * 0.6f, 1.1f, 1.3f);

        var head = Head;
        Box(head, 1.6f, 0.55f, 1.0f, m.ArmorDark);
        Box(head, 1.35f, 0.22f, 0.2f, m.Glass, 0, 0, -0.52f);
        CamMount = Group(head, "CamMount", 0, 0, -0.55f);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            Cyl(arm.Shoulder, 0.55f, 0.55f, 0.9f, m.Frame, -side * 0.1f, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 0.7f, 1.5f, 0.75f, m.Frame, -side * 0.35f, -0.8f, 0);
            Box(arm.Shoulder, 0.85f, 0.9f, 0.9f, m.Armor, -side * 0.35f, -0.6f, -0.1f);
            Cyl(arm.Elbow, 0.42f, 0.42f, 0.9f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Elbow, 0.95f, 0.95f, 2.3f, m.ArmorDark, 0, 0, -0.95f);
            BuildWeapon(arm.Elbow, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    // ---------- Harrow: squat heavy breacher ----------

    /// <summary>
    /// Low and broad: thick short legs on big feet, a wide torso with two raised shoulder humps (the cover plates) and
    /// the cockpit sunk between them, heavy forearms with the guns above blocky fists.
    /// </summary>
    private void DressHarrow(MechChassis t)
    {
        var m = _m;
        var pelvis = Pelvis;
        Box(pelvis, 3.4f, 1.2f, 2.2f, m.Frame);
        Box(pelvis, 3.0f, 0.7f, 2.4f, m.ArmorDark, 0, -0.3f, 0);
        Box(pelvis, 1.4f, 1.0f, 0.5f, m.Armor, 0, -0.3f, -1.2f, -0.15f);
        foreach (int side in new[] { -1, 1 })
            Cyl(pelvis, 0.8f, 0.8f, 0.8f, m.Accent, -side * 1.65f, -0.1f, 0, 0, 0, -HalfPi);
        DressPlantigradeLegs(t, 1.35f, 1.25f);

        TorsoBase.AddChild(NewCyl(0.95f, 1.15f, 0.7f, m.Frame, 0, 0.25f, 0, segments: 14));
        var torso = Torso;
        Box(torso, 4.6f, 2.1f, 3.0f, m.Armor, 0, 1.0f, 0);
        Box(torso, 4.9f, 0.55f, 2.8f, m.ArmorDark, 0, 0.05f, 0);
        Box(torso, 3.4f, 1.2f, 0.45f, m.ArmorDark, 0, 0.9f, -1.6f, -0.15f);
        Box(torso, 1.6f, 0.9f, 1.4f, m.Frame, 0, 2.2f, 0.6f);
        foreach (int side in new[] { -1, 1 }) Box(torso, 0.25f, 1.6f, 2.6f, m.Accent, side * 2.3f, 1.0f, 0);
        for (int i = 0; i < 3; i++) Box(torso, 3.6f, 0.1f, 0.25f, m.Frame, 0, 0.6f + i * 0.35f, 1.55f);

        var head = Head;
        Box(head, 1.3f, 0.7f, 1.1f, m.ArmorDark);
        Box(head, 1.05f, 0.2f, 0.2f, m.Glass, 0, 0.05f, -0.56f);
        CamMount = Group(head, "CamMount", 0, 0.05f, -0.6f);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            DrawCover(t, i);
            Cyl(arm.Shoulder, 0.7f, 0.7f, 1.0f, m.Frame, -side * 0.1f, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 1.0f, 1.6f, 1.0f, m.Frame, -side * 0.4f, -0.9f, 0);
            Cyl(arm.Elbow, 0.55f, 0.55f, 1.1f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Elbow, 1.5f, 1.4f, 2.4f, m.Armor, 0, 0, -1.0f);
            Box(arm.Elbow, 1.2f, 1.0f, 0.9f, m.ArmorDark, 0, -0.45f, -2.45f);
            for (int k = 0; k < 3; k++) Box(arm.Elbow, 0.3f, 0.35f, 0.35f, m.Frame, (k - 1) * 0.38f, -0.75f, -2.85f);
            BuildWeapon(arm.Elbow, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    // ---------- Barbican: assault fortress ----------

    /// <summary>
    /// A wide upper block on a short base: stubby legs on very large feet, a deep chest whose shoulder line sits well
    /// above a narrow visor slit under a flat brow plate, heavy pauldrons, arms hanging wide.
    /// </summary>
    private void DressBarbican(MechChassis t)
    {
        var m = _m;
        var pelvis = Pelvis;
        Box(pelvis, 3.6f, 1.3f, 2.4f, m.Frame);
        Box(pelvis, 3.2f, 0.8f, 2.6f, m.ArmorDark, 0, -0.35f, 0);
        foreach (int side in new[] { -1, 1 })
            Cyl(pelvis, 0.85f, 0.85f, 0.8f, m.Accent, -side * 1.75f, -0.1f, 0, 0, 0, -HalfPi);
        DressPlantigradeLegs(t, 1.5f, 1.55f);

        TorsoBase.AddChild(NewCyl(1.1f, 1.3f, 0.8f, m.Frame, 0, 0.3f, 0, segments: 14));
        var torso = Torso;
        Box(torso, 5.4f, 3.0f, 3.4f, m.Armor, 0, 1.5f, 0);
        Box(torso, 5.7f, 0.6f, 3.2f, m.ArmorDark, 0, 0.1f, 0);
        Box(torso, 6.0f, 0.7f, 3.0f, m.ArmorDark, 0, 3.0f, 0.1f);
        // Brow plate over the visor slit, chest plating below it.
        Box(torso, 2.8f, 0.4f, 1.0f, m.Armor, 0, 2.2f, -1.85f, 0.1f);
        Box(torso, 4.0f, 1.3f, 0.4f, m.ArmorDark, 0, 0.9f, -1.8f, -0.1f);
        for (int i = 0; i < 5; i++) Box(torso, 0.5f, 1.4f, 0.08f, m.Glow, 1.6f - i * 0.8f, 1.5f, 1.72f);
        Cyl(torso, 0.05f, 0.05f, 2.0f, m.Chrome, 2.2f, 4.2f, 0.8f);

        var head = Head;
        Box(head, 2.0f, 0.55f, 0.9f, m.ArmorDark);
        Box(head, 1.7f, 0.16f, 0.2f, m.Glass, 0, 0, -0.47f);
        CamMount = Group(head, "CamMount", 0, 0, -0.5f);

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            DrawCover(t, i);
            Cyl(arm.Shoulder, 0.7f, 0.7f, 1.1f, m.Frame, -side * 0.15f, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 1.0f, 2.0f, 1.0f, m.Frame, -side * 0.45f, -1.1f, 0);
            Cyl(arm.Elbow, 0.55f, 0.55f, 1.1f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Elbow, 1.35f, 1.35f, 2.6f, m.ArmorDark, 0, 0, -1.05f);
            BuildWeapon(arm.Elbow, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    // ---------- Sicklefoot: chicken walker ----------

    /// <summary>
    /// A long, low hull carried forward over a turret ring, cockpit pod under the nose, dorsal radiator spine and a
    /// rear counterweight; weapon pods on short pylons; "Z" legs with forward knees, backward hocks and bird feet.
    /// </summary>
    private void DressSicklefoot(MechChassis t)
    {
        var m = _m;
        float s = Scale, l1 = t.L1 / s, l2 = t.L2 / s, l3 = t.L3 / s;

        // Hip block with the leg actuators on its flanks.
        var pelvis = Pelvis;
        Box(pelvis, 2.0f, 1.0f, 1.6f, m.Frame);
        Box(pelvis, 1.6f, 0.3f, 1.8f, m.ArmorDark, 0, -0.55f, 0);
        Box(pelvis, 1.2f, 0.6f, 0.35f, m.Armor, 0, -0.1f, -0.9f, -0.2f);
        foreach (int side in new[] { -1, 1 })
        {
            Cyl(pelvis, 0.62f, 0.62f, 0.6f, m.Frame, -side * 1.15f, -0.1f, 0, 0, 0, -HalfPi);
            Cyl(pelvis, 0.38f, 0.38f, 0.7f, m.Accent, -side * 1.15f, -0.1f, 0, 0, 0, -HalfPi);
        }

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var leg = Legs[i];
            var hock = leg.Hock!;

            // Thigh: heavy, armoured, angled forward by the IK.
            Cyl(leg.Hip, 0.5f, 0.5f, 0.8f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Hip, 0.85f, l1 * 0.95f, 1.0f, m.Armor, 0, -l1 * 0.5f, 0);
            Box(leg.Hip, 1.0f, l1 * 0.7f, 0.3f, m.ArmorDark, 0, -l1 * 0.5f, -0.6f, 0.08f);
            Box(leg.Hip, 0.22f, l1 * 0.6f, 0.9f, m.Accent, -side * 0.5f, -l1 * 0.5f, 0);

            // Knee drum with a forward cap; slender shin raked back.
            Cyl(leg.Knee, 0.45f, 0.45f, 0.95f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(leg.Knee, 0.9f, 0.75f, 0.5f, m.Armor, 0, 0.05f, -0.45f, -0.2f);
            Box(leg.Knee, 0.5f, l2 * 0.95f, 0.55f, m.Frame, 0, -l2 * 0.5f, 0);
            Box(leg.Knee, 0.7f, l2 * 0.55f, 0.25f, m.ArmorDark, 0, -l2 * 0.4f, 0.38f);
            Box(leg.Knee, 0.06f, l2 * 0.4f, 0.16f, m.Glow, -side * 0.37f, -l2 * 0.45f, 0.38f); // proud of the plate's face (was coplanar: z-fighting)

            // Hock: the backward joint, with a heel spur.
            Cyl(hock, 0.38f, 0.38f, 0.75f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(hock, 0.55f, 0.45f, 0.9f, m.ArmorDark, 0, 0.05f, 0.45f, 0.3f);
            Box(hock, 0.4f, l3 * 0.95f, 0.45f, m.Frame, 0, -l3 * 0.5f, 0);
            Box(hock, 0.5f, l3 * 0.6f, 0.2f, m.Armor, 0, -l3 * 0.5f, -0.3f);

            // Bird foot: three splayed toes forward, one spur back, on a toe-joint drum.
            var foot = leg.Ankle;
            Cyl(foot, 0.3f, 0.3f, 0.6f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(foot, 0.7f, 0.25f, 0.7f, m.ArmorDark, 0, -0.15f, -0.05f);
            foreach (float yaw in new[] { -0.4f, 0f, 0.4f })
            {
                var toe = Group(foot, "Toe", 0, -0.2f, -0.2f);
                toe.Rotation = new Vector3(0, yaw, 0);
                Box(toe, 0.28f, 0.22f, 1.25f, m.Armor, 0, 0, -0.6f);
                Box(toe, 0.2f, 0.16f, 0.3f, m.Frame, 0, -0.04f, -1.32f, 0.25f);
            }
            Box(foot, 0.24f, 0.2f, 0.75f, m.Armor, 0, -0.22f, 0.45f, -0.15f);

            // Hydraulics: across the knee on the back of the leg, across the hock on the front.
            AddPiston(leg.Hip, new Vector3(0, -l1 * 0.45f, 0.5f), leg.Knee, new Vector3(0, -l2 * 0.4f, 0.32f), 0.14f, 0.08f, 1.2f);
            AddPiston(leg.Knee, new Vector3(0, -l2 * 0.7f, -0.3f), hock, new Vector3(0, -l3 * 0.6f, -0.25f), 0.11f, 0.065f, 0.9f);
        }

        // Turret ring and the hull, slung forward.
        TorsoBase.AddChild(NewCyl(0.8f, 0.95f, 0.5f, m.Frame, 0, 0.2f, 0, segments: 14));
        var hull = Torso;
        Box(hull, 2.7f, 1.5f, 4.4f, m.Armor, 0, 0.9f, -0.5f);
        Box(hull, 2.2f, 0.5f, 3.6f, m.ArmorDark, 0, 1.85f, -0.3f);
        Box(hull, 2.4f, 0.7f, 0.5f, m.ArmorDark, 0, 1.3f, -2.85f, 0.35f);
        Box(hull, 2.5f, 0.6f, 0.6f, m.Frame, 0, 0.25f, -2.6f, -0.3f);
        foreach (int side in new[] { -1, 1 })
            Box(hull, 0.25f, 0.9f, 3.0f, m.Accent, side * 1.43f, 0.75f, -0.6f);
        for (int i = 0; i < 5; i++)
        {
            Box(hull, 0.14f, 0.75f, 0.85f, m.Frame, 0, 2.45f, -1.7f + i * 0.72f, 0.2f);
            Box(hull, 0.16f, 0.5f, 0.6f, m.Glow, 0, 2.4f, -1.7f + i * 0.72f, 0.2f);
        }
        Box(hull, 2.2f, 1.2f, 1.0f, m.ArmorDark, 0, 0.9f, 2.1f);
        foreach (float x in new[] { -0.6f, 0f, 0.6f })
            Box(hull, 0.5f, 0.15f, 0.08f, m.Glow, x, 1.0f, 2.62f);
        Cyl(hull, 0.04f, 0.04f, 1.6f, m.Chrome, -0.8f, 2.6f, 1.6f);

        // Cockpit pod under the nose with a wide canopy.
        var head = Head;
        Box(head, 1.6f, 0.9f, 1.4f, m.ArmorDark, 0, 0, -0.35f);
        Box(head, 1.4f, 0.45f, 0.55f, m.Glass, 0, 0.15f, -1.0f, 0.5f);
        Box(head, 1.7f, 0.15f, 1.2f, m.Armor, 0, 0.5f, -0.4f);
        Box(head, 0.3f, 0.12f, 0.3f, m.Lens, 0.55f, -0.25f, -1.08f);
        CamMount = Group(head, "CamMount", 0, 0.2f, -0.75f);

        // Weapon pods on pylons: cannon right, laser left.
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? +1 : -1;
            var arm = Arms[i];
            Cyl(arm.Shoulder, 0.35f, 0.35f, 0.6f, m.Frame, 0, 0, 0, 0, 0, -HalfPi);
            Box(arm.Shoulder, 0.9f, 0.35f, 1.0f, m.Frame, side * 0.5f, 0, 0);
            var pod = arm.Elbow;
            if (i == 1)
            {
                Box(pod, 1.0f, 0.9f, 3.0f, m.Armor, 0, 0, -0.9f);
                Box(pod, 1.05f, 0.15f, 2.4f, m.Accent, 0, 0.48f, -0.9f);
            }
            else
            {
                Box(pod, 1.1f, 0.8f, 2.4f, m.ArmorDark, 0, 0, -0.7f);
                Box(pod, 1.15f, 0.12f, 1.8f, m.Accent, 0, 0.45f, -0.8f);
            }
            BuildWeapon(pod, i == 1 ? t.RightArmWeapon : t.LeftArmWeapon, (i == 1 ? t.RightArmMount : t.LeftArmMount).ToGodot() / Scale);
        }
    }

    // ---------- weapons ----------

    /// <summary>
    /// The business end of a weapon, from its spec, bolted on at <paramref name="mount"/> (elbow frame, model units)
    /// and ending at the muzzle <see cref="WeaponSpec.Length"/> ahead. Guns: a breech housing, one or more barrels of
    /// the spec's calibre, muzzle brakes (the siege cannon gets cooling rings). Beams: an emitter block sized by
    /// calibre with a row of lenses, cooling fins on the big ones.
    /// </summary>
    /// <summary>
    /// A shoulder missile rack: an armoured box on a short pylon, its face a grid of launch tubes (two rows), the
    /// guided arc-missile box taller and angled up a little.
    /// </summary>
    private void BuildRack(Node3D parent, WeaponSpec spec, Vector3 mount)
    {
        var m = _m;
        int tubes = spec.Barrels, cols = (tubes + 1) / 2;
        float tube = 0.32f * spec.Caliber + 0.12f, w = cols * tube + 0.25f, h = 2f * tube + 0.25f, len = spec.Length;
        var box = Group(parent, "Rack", mount.X, mount.Y, mount.Z + len / 2f);
        if (spec.Guided) box.Rotation = new Vector3(0.18f, 0f, 0f);
        Box(parent, 0.35f, 0.5f, 0.6f, m.Frame, mount.X, mount.Y - h / 2f - 0.2f, mount.Z + len / 2f);
        Box(box, w, h, len, m.Armor, 0f, 0f, 0f);
        Box(box, w * 1.02f, 0.1f, len * 0.8f, m.Accent, 0f, h / 2f + 0.04f, 0.05f);
        for (int i = 0; i < tubes; i++)
        {
            float x = (i % cols - (cols - 1) / 2f) * tube, y = (i / cols - 0.5f) * tube;
            Cyl(box, tube * 0.36f, tube * 0.36f, 0.06f, m.Hole, x, y, -len / 2f - 0.01f, -HalfPi);
        }
    }

    /// <summary>A point-defense turret per fitted mount: a small dome with a twin barrel on the left shoulder (stacked back).</summary>
    private void BuildPointDefense(Node3D parent, MechChassis t)
    {
        var m = _m;
        for (int i = 0; i < t.PointDefense; i++)
        {
            Vector3 at = t.PointDefenseMount.ToGodot() / Scale;
            float x = at.X, y = at.Y, z = at.Z + 0.9f * i;
            Cyl(parent, 0.42f, 0.5f, 0.35f, m.Armor, x, y, z, 0f);
            Box(parent, 0.5f, 0.3f, 0.5f, m.Frame, x, y + 0.3f, z);
            for (int b = 0; b < 2; b++)
                Cyl(parent, 0.06f, 0.06f, 0.9f, m.Frame, x + (b - 0.5f) * 0.16f, y + 0.32f, z - 0.6f, -HalfPi);
        }
    }

    private void BuildWeapon(Node3D parent, WeaponSpec spec, Vector3 mount)
    {
        var m = _m;
        float len = spec.Length, c = spec.Caliber;
        if (spec.Kind == WeaponKind.Projectile)
        {
            Cyl(parent, c * 1.5f, c * 1.5f, 0.7f, m.Armor, mount.X, mount.Y, mount.Z - 0.35f, -HalfPi);
            for (int b = 0; b < spec.Barrels; b++)
            {
                float x = mount.X + (spec.Barrels == 1 ? 0f : (b - 0.5f) * c * 2.6f);
                Cyl(parent, c, c, len - 0.5f, m.Frame, x, mount.Y, mount.Z - 0.7f - (len - 0.9f) / 2f, -HalfPi);
                Cyl(parent, c * 1.3f, c * 1.15f, 0.45f, m.Frame, x, mount.Y, mount.Z - len + 0.25f, -HalfPi);
                Cyl(parent, c * 0.6f, c * 0.6f, 0.05f, m.Hole, x, mount.Y, mount.Z - len + 0.01f, -HalfPi);
            }
            if (spec.Caliber >= 0.5f)
                for (int r = 0; r < 4; r++)
                    Cyl(parent, c * 1.25f, c * 1.25f, 0.18f, m.Accent, mount.X, mount.Y, mount.Z - 1.3f - r * 0.45f, -HalfPi);
            return;
        }

        float w = 0.85f * c, h = 0.6f * c;
        Box(parent, w, h, len, m.Armor, mount.X, mount.Y, mount.Z - len / 2f);
        Box(parent, w * 1.05f, 0.1f, len * 0.75f, m.Accent, mount.X, mount.Y + h / 2f + 0.04f, mount.Z - len * 0.45f);
        for (int b = 0; b < spec.Barrels; b++)
        {
            float x = mount.X + (spec.Barrels == 1 ? 0f : (b - (spec.Barrels - 1) / 2f) * w * 0.32f);
            Cyl(parent, 0.11f * c, 0.11f * c, 0.12f, m.Lens, x, mount.Y, mount.Z - len - 0.02f, -HalfPi);
        }
        if (spec.Caliber >= 1.3f)
            for (int f = 0; f < 4; f++)
                Box(parent, w * 0.9f, 0.3f, 0.06f, m.Frame, mount.X, mount.Y + h / 2f + 0.15f, mount.Z - 0.3f - f * 0.35f);
    }

    // ---------- helpers ----------

    private void AddPiston(Node3D a, Vector3 localA, Node3D b, Vector3 localB, float rOuter, float rRod, float outerLength)
    {
        float s = Scale;
        var outer = NewCyl(rOuter * s, rOuter * s, 1f, _m.Frame, segments: 10);
        var rod = NewCyl(rRod * s, rRod * s, 1f, _m.Chrome, segments: 10);
        outer.TopLevel = rod.TopLevel = true;
        _owner.AddChild(outer);
        _owner.AddChild(rod);
        _pistons.Add(new Piston { Outer = outer, Rod = rod, A = a, B = b, LocalA = localA, LocalB = localB, OuterLength = outerLength });
    }

    private static Node3D Group(Node3D parent, string name, float x = 0, float y = 0, float z = 0)
    {
        var g = new Node3D { Name = name, Position = new Vector3(x, y, z) };
        parent.AddChild(g);
        return g;
    }

    private static MeshInstance3D Box(Node3D parent, float w, float h, float d, Material mat,
        float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0)
    {
        var mi = New(new BoxMesh { Size = new Vector3(w, h, d) }, mat, x, y, z, rx, ry, rz);
        mi.SetInstanceShaderParameter("shape", new Vector4(w / 2f, h / 2f, d / 2f, 1f)); // for the armour shader's edges
        parent.AddChild(mi);
        return mi;
    }

    private static MeshInstance3D Cyl(Node3D parent, float rTop, float rBottom, float h, Material mat,
        float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, int segments = 18)
    {
        var mi = NewCyl(rTop, rBottom, h, mat, x, y, z, rx, ry, rz, segments);
        parent.AddChild(mi);
        return mi;
    }

    private static MeshInstance3D NewCyl(float rTop, float rBottom, float h, Material mat,
        float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, int segments = 18)
    {
        var mi = New(new CylinderMesh { TopRadius = rTop, BottomRadius = rBottom, Height = h, RadialSegments = segments, Rings = 1 },
            mat, x, y, z, rx, ry, rz);
        mi.SetInstanceShaderParameter("shape", new Vector4(rTop, h / 2f, rBottom, 2f));
        return mi;
    }

    private static MeshInstance3D New(Mesh mesh, Material mat, float x, float y, float z, float rx, float ry, float rz) => new()
    {
        Mesh = mesh,
        MaterialOverride = mat,
        Position = new Vector3(x, y, z),
        RotationOrder = EulerOrder.Xyz,
        Rotation = new Vector3(rx, ry, rz),
    };
}
