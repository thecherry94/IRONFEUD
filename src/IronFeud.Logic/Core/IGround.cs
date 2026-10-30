using System.Numerics;

namespace IronFeud.Logic.Core;

/// <summary>Ground height under a point. Locomotion plants feet on it; a null ground means flat at y = 0.</summary>
public interface IGround
{
    float Height(float x, float z);
}

public static class Ground
{
    public static float HeightAt(this IGround? g, float x, float z) => g?.Height(x, z) ?? 0f;

    /// <summary>Surface normal from central differences over <paramref name="d"/> metres.</summary>
    public static Vector3 NormalAt(this IGround? g, float x, float z, float d = 1f)
    {
        if (g == null) return Vector3.UnitY;
        float hx = g.Height(x + d, z) - g.Height(x - d, z), hz = g.Height(x, z + d) - g.Height(x, z - d);
        return Vector3.Normalize(new Vector3(-hx, 2f * d, -hz));
    }

    /// <summary>The point's x/z with y on the ground.</summary>
    public static Vector3 OnGround(this IGround? g, Vector3 p) => new(p.X, g.HeightAt(p.X, p.Z), p.Z);
}
