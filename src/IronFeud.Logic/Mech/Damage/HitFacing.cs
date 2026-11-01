using System;
using System.Numerics;

namespace IronFeud.Logic.Mech.Damage;

public enum Facing { Front, Side, Rear }

/// <summary>
/// Which side of the torso a shot strikes, from the way the shot travels and the way the torso faces (its aim
/// direction on the ground plane, so twisting the torso toward a threat turns the front armour to it).
/// A shot travelling the same way the torso faces came from behind.
/// </summary>
public static class HitFacing
{
    /// <summary>Within ~50° of dead astern a hit is fully a rear hit; past ~80° it is not one at all.</summary>
    private const float RearFull = 0.64f, RearNone = 0.17f;

    /// <summary>How much a hit counts as a rear hit: 0 at the front and sides, 1 from straight behind.</summary>
    public static float RearWeight(Vector3 torsoForward, Vector3 travel)
    {
        float c = Alignment(torsoForward, travel);
        float t = Math.Clamp((c - RearNone) / (RearFull - RearNone), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    public static Facing Classify(Vector3 torsoForward, Vector3 travel)
    {
        float c = Alignment(torsoForward, travel);
        return c > 0.4f ? Facing.Rear : c < -0.4f ? Facing.Front : Facing.Side;
    }

    /// <summary>Cosine between the shot's travel and the torso's forward, both flattened onto the ground.</summary>
    private static float Alignment(Vector3 torsoForward, Vector3 travel)
    {
        var f = new Vector2(torsoForward.X, torsoForward.Z);
        var d = new Vector2(travel.X, travel.Z);
        if (f.LengthSquared() < 1e-8f || d.LengthSquared() < 1e-8f) return -1f;
        return Vector2.Dot(Vector2.Normalize(f), Vector2.Normalize(d));
    }
}
