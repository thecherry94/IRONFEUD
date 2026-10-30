using Godot;
using IronFeud.Logic.Core;

namespace IronFeud.Core;

/// <summary>Placeholder textures generated at startup, ported from the prototype's canvas textures.</summary>
public static class ProceduralTextures
{
    public static ImageTexture Ground(uint seed)
    {
        const int s = 512;
        var rng = new DeterministicRng(seed);
        var img = Image.CreateEmpty(s, s, false, Image.Format.Rgba8);
        img.Fill(new Color("5d564c"));
        for (int i = 0; i < 5000; i++)
        {
            int v = 70 + (int)(rng.NextFloat() * 40);
            var c = Color.Color8((byte)v, (byte)(v - 6), (byte)(v - 14));
            int w = 2 + (int)(rng.NextFloat() * 3), h = 2 + (int)(rng.NextFloat() * 3);
            Blend(img, (int)(rng.NextFloat() * s), (int)(rng.NextFloat() * s), w, h, c, 0.35f);
        }
        var seam = Color.Color8(30, 28, 24);
        Outline(img, 0, 0, s, s, 3, seam, 0.55f);
        Blend(img, s / 2 - 1, 0, 2, s, seam, 0.22f);
        Blend(img, 0, s / 2 - 1, s, 2, seam, 0.22f);
        img.GenerateMipmaps();
        return ImageTexture.CreateFromImage(img);
    }

    public static ImageTexture ArmorPanel(uint seed)
    {
        const int s = 256;
        var rng = new DeterministicRng(seed);
        var img = Image.CreateEmpty(s, s, false, Image.Format.Rgba8);
        img.Fill(new Color("b8bcc0"));
        for (int i = 0; i < 1500; i++)
        {
            byte v = (byte)(165 + (int)(rng.NextFloat() * 35));
            Blend(img, (int)(rng.NextFloat() * s), (int)(rng.NextFloat() * s), 2, 2, Color.Color8(v, v, v), 0.3f);
        }
        var line = Color.Color8(40, 40, 40);
        Outline(img, 6, 6, s - 12, s - 12, 2, line, 0.7f);
        Blend(img, 6, (int)(s * 0.55f) - 1, s - 12, 2, line, 0.7f);
        foreach (var (x, y) in new[] { (16, 16), (s - 16, 16), (16, s - 16), (s - 16, s - 16) })
            Disc(img, x, y, 3.5f, Color.Color8(50, 50, 50), 0.8f);
        for (int i = 0; i < 6; i++)
            Blend(img, (int)(rng.NextFloat() * s), (int)(s * 0.55f + rng.NextFloat() * 40), 30 + (int)(rng.NextFloat() * 40), 3, Color.Color8(90, 60, 40), 0.25f);
        img.GenerateMipmaps();
        return ImageTexture.CreateFromImage(img);
    }

    public static GradientTexture2D RadialPuff(Color inner, Color outer)
    {
        var g = new Gradient();
        g.SetColor(0, inner);
        g.SetColor(1, outer);
        return new GradientTexture2D
        {
            Gradient = g,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Width = 64,
            Height = 64,
        };
    }

    /// <summary>Soft-edged footprint mask for decals (white; tint via Decal.Modulate).</summary>
    public static ImageTexture Footprint()
    {
        const int w = 64, h = 128;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float ex = Mathf.Min(x, w - 1 - x) / 8f, ey = Mathf.Min(y, h - 1 - y) / 8f;
            img.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp(Mathf.Min(ex, ey), 0f, 1f)));
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static void Blend(Image img, int x0, int y0, int w, int h, Color c, float a)
    {
        int s = img.GetWidth(), t = img.GetHeight();
        for (int y = y0; y < y0 + h; y++)
        for (int x = x0; x < x0 + w; x++)
        {
            if (x < 0 || y < 0 || x >= s || y >= t) continue;
            img.SetPixel(x, y, img.GetPixel(x, y).Lerp(c, a));
        }
    }

    private static void Outline(Image img, int x, int y, int w, int h, int thick, Color c, float a)
    {
        Blend(img, x, y, w, thick, c, a);
        Blend(img, x, y + h - thick, w, thick, c, a);
        Blend(img, x, y + thick, thick, h - 2 * thick, c, a);
        Blend(img, x + w - thick, y + thick, thick, h - 2 * thick, c, a);
    }

    private static void Disc(Image img, int cx, int cy, float r, Color c, float a)
    {
        int ri = Mathf.CeilToInt(r);
        for (int y = -ri; y <= ri; y++)
        for (int x = -ri; x <= ri; x++)
            if (x * x + y * y <= r * r) Blend(img, cx + x, cy + y, 1, 1, c, a);
    }
}
