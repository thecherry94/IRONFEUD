using System;

namespace IronFeud.Logic.Mech.Weapons;

/// <summary>
/// The pilot's weapon groups: six sets of mounts, each fired by one trigger, in alpha (every ready weapon in the group
/// at once) or chain mode (the next ready one, then a <see cref="ChainBeat"/> pause, round the group). Turns held
/// triggers into a <see cref="MechInput.FireMask"/>; the AI picks weapons itself and doesn't use this.
/// </summary>
public sealed class WeaponGroups
{
    public const int Count = 6;

    /// <summary>Seconds between weapons in a chain-firing group.</summary>
    public const float ChainBeat = 0.25f;

    /// <summary>Mount bits per group (bit i = mount i).</summary>
    public readonly uint[] Members = new uint[Count];

    public readonly bool[] Chain = new bool[Count];

    private readonly int[] _next = new int[Count];
    private readonly float[] _wait = new float[Count];

    /// <summary>
    /// Default groups: 1 long-range direct fire, 2 short-range direct fire (under 300 m effective), 3 missiles. Guided
    /// racks fire in chain by default, so a held trigger ripples volleys instead of dumping them.
    /// </summary>
    public static WeaponGroups Default(MechWeapons weapons)
    {
        var g = new WeaponGroups();
        for (int i = 0; i < weapons.Mounts.Count; i++)
        {
            var spec = weapons.Mounts[i].Spec;
            int group = spec.Kind == WeaponKind.Missile ? 2 : spec.EffectiveRange >= 300f ? 0 : 1;
            g.Members[group] |= 1u << i;
        }
        // An empty first group would leave the left mouse button dead: give it the short-range weapons.
        if (g.Members[0] == 0u)
        {
            g.Members[0] = g.Members[1];
            g.Members[1] = 0u;
        }
        return g;
    }

    public bool Contains(int group, int weapon) => (Members[group] & (1u << weapon)) != 0u;

    public void Toggle(int group, int weapon) => Members[group] ^= 1u << weapon;

    public void ToggleChain(int group)
    {
        Chain[group] = !Chain[group];
        _wait[group] = 0f;
    }

    /// <summary>
    /// Fire mask for this tick. <paramref name="held"/>: bit g = group g's trigger is held. Alpha groups request every
    /// member (weapons that aren't ready simply don't fire); chain groups fire their next ready member when the beat
    /// allows.
    /// </summary>
    public uint FireMask(uint held, MechWeapons weapons, float h)
    {
        uint mask = 0u;
        for (int g = 0; g < Count; g++)
        {
            _wait[g] = MathF.Max(0f, _wait[g] - h);
            if ((held & (1u << g)) == 0u || Members[g] == 0u) continue;
            if (!Chain[g])
            {
                mask |= Members[g];
                continue;
            }
            if (_wait[g] > 0f) continue;
            int n = weapons.Mounts.Count;
            for (int k = 0; k < n; k++)
            {
                int i = (_next[g] + k) % n;
                if (!Contains(g, i) || !weapons.CanFire(i)) continue;
                mask |= 1u << i;
                _next[g] = (i + 1) % n;
                _wait[g] = ChainBeat;
                break;
            }
        }
        return mask;
    }
}
