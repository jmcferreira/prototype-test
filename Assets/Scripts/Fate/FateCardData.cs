/// <summary>
/// Definition of a single Fate card. Fate cards are drawn during combat
/// and modify the attack/defense outcome. Each card has offensive modifiers
/// (used when drawn by the attacker) and defensive modifiers (used when
/// drawn by the defender).
/// </summary>
[System.Serializable]
public class FateCardData
{
    public string cardName;

    // Offensive modifiers (applied when drawn as attacker)
    public int damageBonus;

    // Defensive modifiers (applied when drawn as defender)
    public int blockBonus;
    public int dodgeBonus;

    // Status effect — applied to opponent (attacker → target, defender → attacker)
    public StatusEffectType statusEffect;
    public int statusStacks;
}
