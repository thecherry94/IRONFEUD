using System;

namespace IronFeud.Logic.Mech.Damage;

public enum MechSection { Torso, ArmLeft, ArmRight, Legs, Cockpit }

public enum MechStatus { Operational, Destroyed, PilotKilled }

/// <summary>Outcome of one hit, after transfer rules.</summary>
public struct DamageReport
{
    /// <summary>Section the hit volume belonged to.</summary>
    public MechSection Struck;

    /// <summary>Section that actually took the damage (destroyed sections pass it to the torso).</summary>
    public MechSection Applied;
    public float Amount;
    public float HpAfter;
    public bool SectionDestroyed;
    public MechStatus StatusAfter;
    public bool Ignored;
}

/// <summary>
/// Per-section hit points. Plain data so it can persist between battles (campaign → BattleSetup → BattleResult).
/// </summary>
public sealed class MechDamage
{
    public static readonly int SectionCount = Enum.GetValues<MechSection>().Length;

    public readonly float[] Hp = new float[SectionCount];
    public readonly float[] MaxHp = new float[SectionCount];
    public MechStatus Status = MechStatus.Operational;

    /// <summary>Fresh armour for a chassis. The cockpit is extremely fragile: one solid hit kills the pilot.</summary>
    public static MechDamage Create(MechChassis c)
    {
        var d = new MechDamage();
        d.Set(MechSection.Torso, c.TorsoHp);
        d.Set(MechSection.ArmLeft, c.ArmHp);
        d.Set(MechSection.ArmRight, c.ArmHp);
        d.Set(MechSection.Legs, c.LegsHp);
        d.Set(MechSection.Cockpit, c.CockpitHp);
        return d;
    }

    private void Set(MechSection s, float hp) => Hp[(int)s] = MaxHp[(int)s] = hp;

    public float this[MechSection s] => Hp[(int)s];
    public bool IsDestroyed(MechSection s) => Hp[(int)s] <= 0f;
    public float Fraction(MechSection s) => MaxHp[(int)s] > 0f ? Hp[(int)s] / MaxHp[(int)s] : 0f;
    public bool IsOut => Status != MechStatus.Operational;

    public DamageReport Apply(MechSection struck, float amount)
    {
        var r = new DamageReport { Struck = struck, Applied = struck, Amount = amount, StatusAfter = Status };
        if (IsOut || amount <= 0f)
        {
            r.Ignored = true;
            r.HpAfter = Hp[(int)struck];
            return r;
        }

        if (struck != MechSection.Torso && IsDestroyed(struck)) r.Applied = MechSection.Torso;
        int i = (int)r.Applied;
        float before = Hp[i];
        Hp[i] = MathF.Max(0f, before - amount);
        r.HpAfter = Hp[i];
        r.SectionDestroyed = before > 0f && Hp[i] <= 0f;

        if (r.SectionDestroyed && r.Applied == MechSection.Torso) Status = MechStatus.Destroyed;
        else if (r.SectionDestroyed && r.Applied == MechSection.Cockpit) Status = MechStatus.PilotKilled;
        r.StatusAfter = Status;
        return r;
    }
}
