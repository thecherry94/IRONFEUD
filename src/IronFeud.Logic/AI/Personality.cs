namespace IronFeud.Logic.AI;

/// <summary>
/// Pilot temperament (docs/combat-ai.md): preferred range (m), heat budget for firing, aggression multiplier on
/// ENGAGE, reaction time before firing at a fresh contact (s), and whether it twists its weaker arm away.
/// Ranges are the prototype's ×2 (55 / 100 / 165 m), matching the doubled weapon ranges.
/// </summary>
public sealed record Personality(string Name, float Range, float HeatLimit, float Aggression, float Reaction, bool TwistAway)
{
    public static readonly Personality Brawler = new("Brawler", 110f, 0.92f, 1.25f, 0.25f, true);
    public static readonly Personality Balanced = new("Balanced", 200f, 0.82f, 1.0f, 0.30f, true);
    public static readonly Personality Sniper = new("Sniper", 330f, 0.75f, 0.85f, 0.35f, false);

    public static readonly Personality[] All = { Brawler, Balanced, Sniper };
}
