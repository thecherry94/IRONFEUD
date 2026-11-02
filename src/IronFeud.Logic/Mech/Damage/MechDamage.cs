using System;

namespace IronFeud.Logic.Mech.Damage;

/// <summary>
/// Damage sections. <see cref="Torso"/> is the centre torso (the core: destroyed = mech destroyed); the side torsos
/// carry the arms and the shoulder rack; each leg is its own section.
/// </summary>
public enum MechSection { Torso, TorsoLeft, TorsoRight, ArmLeft, ArmRight, LegLeft, LegRight, Cockpit }

public enum MechStatus { Operational, Destroyed, PilotKilled }

/// <summary>Outcome of one hit, after transfer rules.</summary>
public struct DamageReport
{
    /// <summary>Section the hit volume belonged to.</summary>
    public MechSection Struck;

    /// <summary>Section that actually took the damage (destroyed limbs pass it to their side torso, a destroyed side torso to the centre).</summary>
    public MechSection Applied;

    /// <summary>Damage actually done (to armour and structure, after the weapon's factors against each).</summary>
    public float Amount;

    /// <summary>Part of <see cref="Amount"/> that went into the internal structure.</summary>
    public float ToStructure;

    /// <summary>This hit stripped the last of the plate it struck: the structure underneath is exposed.</summary>
    public bool Breached;

    /// <summary>Which side the hit came from (front when unknown): torso hits from behind strike the thin rear plates.</summary>
    public Facing Facing;
    public float HpAfter;
    public bool SectionDestroyed;

    /// <summary>A side torso went and took this arm with it (<see cref="MechSection.ArmLeft"/> / Right), else null.</summary>
    public MechSection? ArmLost;

    /// <summary>Damage the rounds stored in the destroyed section did to the centre torso when they went off.</summary>
    public float CookOff;
    public MechStatus StatusAfter;
    public bool Ignored;
}

/// <summary>
/// Per-section damage as armour over internal structure. Armour plates are sturdy and soak hits first; the structure
/// underneath is fragile and is what actually keeps a section working: a section is destroyed when its structure is
/// gone, and structure damage degrades it on the way (see <see cref="MechSim"/>). Each torso section has a front plate
/// and a thin rear plate over one structure. Weapons can be better against one or the other (a machine gun barely
/// scratches armour but shreds exposed structure).
/// Out: the centre torso, the cockpit (pilot killed) or both legs. A destroyed arm or leg passes hits to the side torso
/// on its side, a destroyed side torso to the centre; a side torso takes its arm with it.
/// Plain data so it can persist between battles.
/// </summary>
public sealed class MechDamage
{
    public static readonly int SectionCount = Enum.GetValues<MechSection>().Length;

    /// <summary>Front plates per section.</summary>
    public readonly float[] Armor = new float[SectionCount], MaxArmor = new float[SectionCount];
    public readonly float[] Structure = new float[SectionCount], MaxStructure = new float[SectionCount];

    /// <summary>Rear plates (torso sections only; 0 elsewhere).</summary>
    public readonly float[] RearArmor = new float[SectionCount], MaxRearArmor = new float[SectionCount];

    public MechStatus Status = MechStatus.Operational;

    // Shares of a section's total that are armour vs structure. Torso sections: 55 % front plate, 15 % rear plate,
    // 30 % structure; limbs and the head: 65 % armour, 35 % structure.
    private const float TorsoFront = 0.55f, TorsoRear = 0.15f, LimbArmor = 0.65f;

    /// <summary>
    /// Shares of the chassis' HP figures per section: the centre torso 80 % of <see cref="MechChassis.TorsoHp"/> (it takes
    /// 80 % of torso hits in lance battles, so its time to kill stays the old single torso's) and each side torso 25 %;
    /// each leg 60 % of <see cref="MechChassis.LegsHp"/>.
    /// </summary>
    public const float CentreShare = 0.8f, SideShare = 0.25f, LegShare = 0.6f;

    /// <summary>Fresh armour for a chassis. The cockpit is extremely fragile: one solid hit kills the pilot.</summary>
    public static MechDamage Create(MechChassis c)
    {
        var d = new MechDamage();
        d.SetTorso(MechSection.Torso, CentreShare * c.TorsoHp);
        d.SetTorso(MechSection.TorsoLeft, SideShare * c.TorsoHp);
        d.SetTorso(MechSection.TorsoRight, SideShare * c.TorsoHp);
        foreach (var s in new[] { MechSection.ArmLeft, MechSection.ArmRight }) d.Set(s, LimbArmor * c.ArmHp, (1f - LimbArmor) * c.ArmHp);
        foreach (var s in new[] { MechSection.LegLeft, MechSection.LegRight })
            d.Set(s, LimbArmor * LegShare * c.LegsHp, (1f - LimbArmor) * LegShare * c.LegsHp);
        d.Set(MechSection.Cockpit, LimbArmor * c.CockpitHp, (1f - LimbArmor) * c.CockpitHp);
        return d;
    }

    private void SetTorso(MechSection s, float total)
    {
        Set(s, TorsoFront * total, (1f - TorsoFront - TorsoRear) * total);
        RearArmor[(int)s] = MaxRearArmor[(int)s] = TorsoRear * total;
    }

    /// <summary>Sets a section's armour and structure to fresh values.</summary>
    public void Set(MechSection s, float armor, float structure)
    {
        Armor[(int)s] = MaxArmor[(int)s] = armor;
        Structure[(int)s] = MaxStructure[(int)s] = structure;
    }

    public static bool IsTorso(MechSection s) => s is MechSection.Torso or MechSection.TorsoLeft or MechSection.TorsoRight;

    /// <summary>The side torso a limb hangs from (arms and legs), else the centre torso.</summary>
    public static MechSection Parent(MechSection s) => s switch
    {
        MechSection.ArmLeft or MechSection.LegLeft => MechSection.TorsoLeft,
        MechSection.ArmRight or MechSection.LegRight => MechSection.TorsoRight,
        _ => MechSection.Torso,
    };

    /// <summary>All that is left of a section: its plates and its structure.</summary>
    public float this[MechSection s] => Armor[(int)s] + Structure[(int)s] + RearArmor[(int)s];
    public float Max(MechSection s) => MaxArmor[(int)s] + MaxStructure[(int)s] + MaxRearArmor[(int)s];

    /// <summary>Both legs are gone.</summary>
    public bool Legless => IsDestroyed(MechSection.LegLeft) && IsDestroyed(MechSection.LegRight);

    public bool IsDestroyed(MechSection s) => Structure[(int)s] <= 0f;
    public float Fraction(MechSection s) => Max(s) > 0f ? this[s] / Max(s) : 0f;
    public float StructureFraction(MechSection s) => MaxStructure[(int)s] > 0f ? Structure[(int)s] / MaxStructure[(int)s] : 0f;

    /// <summary>The section's (front) plate is gone: its structure takes hits directly.</summary>
    public bool Exposed(MechSection s) => Armor[(int)s] <= 0f;
    public bool IsOut => Status != MechStatus.Operational;

    /// <summary>Damage from inside (an ammunition cook-off): straight into a section's structure, plates untouched.</summary>
    public void ApplyInternal(MechSection s, float amount)
    {
        if (IsOut || amount <= 0f) return;
        int i = (int)s;
        bool wasIntact = Structure[i] > 0f;
        Structure[i] = MathF.Max(0f, Structure[i] - amount);
        if (wasIntact && Structure[i] <= 0f && s == MechSection.Torso) Status = MechStatus.Destroyed;
    }

    /// <param name="facing">Torso hits from behind strike the rear plate.</param>
    /// <param name="vsArmor">How effective the weapon is against armour plates (1 = full damage).</param>
    /// <param name="vsStructure">How effective it is against exposed structure.</param>
    public DamageReport Apply(MechSection struck, float amount, Facing facing = Facing.Front, float vsArmor = 1f, float vsStructure = 1f)
    {
        var r = new DamageReport { Struck = struck, Applied = struck, Facing = facing, StatusAfter = Status };
        if (IsOut || amount <= 0f)
        {
            r.Ignored = true;
            r.HpAfter = this[struck];
            return r;
        }

        while (r.Applied != MechSection.Torso && r.Applied != MechSection.Cockpit && IsDestroyed(r.Applied)) r.Applied = Parent(r.Applied);
        int i = (int)r.Applied;
        bool rear = IsTorso(r.Applied) && facing == Facing.Rear;

        // The plate first: it soaks the hit at the weapon's armour effectiveness; whatever it can't hold goes on.
        float plate = rear ? RearArmor[i] : Armor[i], raw = amount, toArmor = 0f;
        if (plate > 0f && vsArmor > 0f)
        {
            toArmor = MathF.Min(plate, raw * vsArmor);
            raw -= toArmor / vsArmor;
            plate -= toArmor;
            r.Breached = plate <= 0f;
            if (rear) RearArmor[i] = plate;
            else Armor[i] = plate;
        }
        else if (plate > 0f) raw = 0f; // can't hurt armour at all, and the plate is still there

        float before = Structure[i];
        float toStructure = MathF.Min(before, raw * vsStructure);
        Structure[i] = before - toStructure;
        r.Amount = toArmor + toStructure;
        r.ToStructure = toStructure;
        r.HpAfter = this[r.Applied];
        r.SectionDestroyed = before > 0f && Structure[i] <= 0f;

        if (r.SectionDestroyed && r.Applied is MechSection.TorsoLeft or MechSection.TorsoRight)
        {
            // The side torso takes its arm with it.
            var arm = r.Applied == MechSection.TorsoLeft ? MechSection.ArmLeft : MechSection.ArmRight;
            if (!IsDestroyed(arm))
            {
                Armor[(int)arm] = Structure[(int)arm] = 0f;
                r.ArmLost = arm;
            }
        }

        if (r.SectionDestroyed && r.Applied == MechSection.Torso) Status = MechStatus.Destroyed;
        else if (r.SectionDestroyed && r.Applied == MechSection.Cockpit) Status = MechStatus.PilotKilled;
        else if (r.SectionDestroyed && Legless) Status = MechStatus.Destroyed;
        r.StatusAfter = Status;
        return r;
    }
}
