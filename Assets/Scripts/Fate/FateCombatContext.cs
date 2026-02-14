/// <summary>
/// Static context set before attack resolution so that CombatResolver and
/// attack resolvers can read fate modifiers without changing their signatures.
/// Set by FateManager, cleared after each attack action resolves.
/// </summary>
public static class FateCombatContext
{
    /// <summary>Bonus damage from the attacker's chosen Fate card.</summary>
    public static int DamageBonus;

    /// <summary>Status effect applied to target from attacker's Fate card.</summary>
    public static StatusEffectType AttackerStatusEffect;
    public static int AttackerStatusStacks;

    /// <summary>Status effect applied to attacker from defender's Fate card.</summary>
    public static StatusEffectType DefenderStatusEffect;
    public static int DefenderStatusStacks;

    public static void Clear()
    {
        DamageBonus = 0;
        AttackerStatusEffect = default;
        AttackerStatusStacks = 0;
        DefenderStatusEffect = default;
        DefenderStatusStacks = 0;
    }
}
