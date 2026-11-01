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

    /// <summary>Damage actually done (to armour and structure, after the weapon's factors against each).</summary>
    public float Amount;

    /// <summary>Part of <see cref="Amount"/> that went into the internal structure.</summary>
    public float ToStructure;

    /// <summary>This hit stripped the last of the plate it struck: the structure underneath is exposed.</summary>
    public bool Breached;

    /// <summary>Which side the hit came from (front when unknown): torso hits from behind strike the thin rear plate.</summary>
    public Facing Facing;
    public float HpAfter;
    public bool SectionDestroyed;
    public MechStatus StatusAfter;
    public bool Ignored;
}

/// <summary>
/// Per-section damage as armour over internal structure. Armour plates are sturdy and soak hits first; the structure
/// underneath is fragile and is what actually keeps a section working: a section is destroyed when its structure is
/// gone, and structure damage degrades it on the way (see <see cref="MechSim"/>). The torso has a front plate and a
/// thin rear plate over one structure. Weapons can be better against one or the other (a machine gun barely scratches
/// armour but shreds exposed structure). Plain data so it can persist between battles.
/// </summary>
public sealed class MechDamage
{
    public static readonly int SectionCount = Enum.GetValues<MechSection>().Length;

    /// <summary>Front plates per section (the torso's front plate).</summary>
    public readonly float[] Armor = new float[SectionCount], MaxArmor = new float[SectionCount];
    public readonly float[] Structure = new float[SectionCount], MaxStructure = new float[SectionCount];

    /// <summary>The torso's rear plate.</summary>
    public float RearArmor, MaxRearArmor;

    public MechStatus Status = MechStatus.Operational;

    // Shares of a section's total (the chassis' HP figure) that are armour vs structure. The torso: 55 % front plate,
    // 15 % rear plate, 30 % structure; limbs and the head: 65 % armour, 35 % structure.
    private const float TorsoFront = 0.55f, TorsoRear = 0.15f, LimbArmor = 0.65f;

    /// <summary>Fresh armour for a chassis. The cockpit is extremely fragile: one solid hit kills the pilot.</summary>
    public static MechDamage Create(MechChassis c)
    {
        var d = new MechDamage();
        d.Set(MechSection.Torso, TorsoFront * c.TorsoHp, (1f - TorsoFront - TorsoRear) * c.TorsoHp);
        d.RearArmor = d.MaxRearArmor = TorsoRear * c.TorsoHp;
        d.Set(MechSection.ArmLeft, LimbArmor * c.ArmHp, (1f - LimbArmor) * c.ArmHp);
        d.Set(MechSection.ArmRight, LimbArmor * c.ArmHp, (1f - LimbArmor) * c.ArmHp);
        d.Set(MechSection.Legs, LimbArmor * c.LegsHp, (1f - LimbArmor) * c.LegsHp);
        d.Set(MechSection.Cockpit, LimbArmor * c.CockpitHp, (1f - LimbArmor) * c.CockpitHp);
        return d;
    }

    /// <summary>Sets a section's armour and structure to fresh values.</summary>
    public void Set(MechSection s, float armor, float structure)
    {
        Armor[(int)s] = MaxArmor[(int)s] = armor;
        Structure[(int)s] = MaxStructure[(int)s] = structure;
    }

    /// <summary>All that is left of a section: its plates and its structure.</summary>
    public float this[MechSection s] => Armor[(int)s] + Structure[(int)s] + (s == MechSection.Torso ? RearArmor : 0f);
    public float Max(MechSection s) => MaxArmor[(int)s] + MaxStructure[(int)s] + (s == MechSection.Torso ? MaxRearArmor : 0f);

    public bool IsDestroyed(MechSection s) => Structure[(int)s] <= 0f;
    public float Fraction(MechSection s) => Max(s) > 0f ? this[s] / Max(s) : 0f;
    public float StructureFraction(MechSection s) => MaxStructure[(int)s] > 0f ? Structure[(int)s] / MaxStructure[(int)s] : 0f;

    /// <summary>The section's (front) plate is gone: its structure takes hits directly.</summary>
    public bool Exposed(MechSection s) => Armor[(int)s] <= 0f;
    public bool IsOut => Status != MechStatus.Operational;

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

        if (struck != MechSection.Torso && IsDestroyed(struck)) r.Applied = MechSection.Torso;
        int i = (int)r.Applied;
        bool rear = r.Applied == MechSection.Torso && facing == Facing.Rear;

        // The plate first: it soaks the hit at the weapon's armour effectiveness; whatever it can't hold goes on.
        float plate = rear ? RearArmor : Armor[i], raw = amount, toArmor = 0f;
        if (plate > 0f && vsArmor > 0f)
        {
            toArmor = MathF.Min(plate, raw * vsArmor);
            raw -= toArmor / vsArmor;
            plate -= toArmor;
            r.Breached = plate <= 0f;
            if (rear) RearArmor = plate;
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

        if (r.SectionDestroyed && r.Applied == MechSection.Torso) Status = MechStatus.Destroyed;
        else if (r.SectionDestroyed && r.Applied == MechSection.Cockpit) Status = MechStatus.PilotKilled;
        r.StatusAfter = Status;
        return r;
    }
}
