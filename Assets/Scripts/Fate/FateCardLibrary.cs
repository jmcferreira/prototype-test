/// <summary>
/// Predefined Fate card decks. Returns fresh arrays each call.
/// </summary>
public static class FateCardLibrary
{
    public static FateCardData[] GetStarterDeck()
    {
        return new[]
        {
            // Offensive cards
            new FateCardData { cardName = "Sharp Edge",       damageBonus = 2 },
            new FateCardData { cardName = "Focused Strike",   damageBonus = 1 },
            new FateCardData { cardName = "Heavy Blow",       damageBonus = 3 },

            // Defensive cards
            new FateCardData { cardName = "Brace",            blockBonus = 3 },
            new FateCardData { cardName = "Iron Guard",       blockBonus = 2 },
            new FateCardData { cardName = "Quick Reflexes",   dodgeBonus = 1 },

            // Hybrid cards
            new FateCardData { cardName = "Fortune's Favor",  damageBonus = 1, blockBonus = 1 },
            new FateCardData { cardName = "Calculated Risk",  damageBonus = 2, blockBonus = 1 },

            // Status cards
            new FateCardData { cardName = "Venomous Fate",    statusEffect = StatusEffectType.Poison, statusStacks = 1 },
            new FateCardData { cardName = "Burning Fate",     statusEffect = StatusEffectType.Burn,   statusStacks = 1 },
        };
    }

    /// <summary>
    /// Basic enemy fate deck — weaker modifiers than the player's.
    /// </summary>
    public static FateCardData[] GetBasicEnemyDeck()
    {
        return new[]
        {
            new FateCardData { cardName = "Crude Swing",     damageBonus = 1 },
            new FateCardData { cardName = "Glancing Blow",   damageBonus = 1 },
            new FateCardData { cardName = "Tough Hide",      blockBonus = 1 },
            new FateCardData { cardName = "Tough Hide",      blockBonus = 1 },
            new FateCardData { cardName = "Lucky Dodge",     dodgeBonus = 1 },
            new FateCardData { cardName = "Wild Swing",      damageBonus = 2 },
        };
    }
}
