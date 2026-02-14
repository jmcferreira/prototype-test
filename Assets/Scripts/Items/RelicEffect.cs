/// <summary>
/// Types of passive effects that relics can provide.
/// </summary>
public enum RelicEffect
{
    StartingBlock,      // Gain X Block at the start of each turn
    MaxHPBonus,         // Permanently increase max HP by X
    HealBonus,          // Add X% to between-scenario healing
    GoldBonus,          // Gain X bonus Gold at the start of each scenario
    DamageBonus,        // Deal X extra damage on all attacks
}
