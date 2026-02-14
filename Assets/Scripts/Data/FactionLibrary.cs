using System.Collections.Generic;

/// <summary>
/// Registry of all PVP factions. Each faction has a champion and a creature pool.
/// </summary>
public static class FactionLibrary
{
    private static readonly Dictionary<string, FactionDef> _factions = new();

    static FactionLibrary()
    {
        Register(CreateDarkside());
        Register(CreateBrotherhood());
    }

    public static FactionDef Get(string factionId)
    {
        return _factions.TryGetValue(factionId, out var f) ? f : null;
    }

    public static IEnumerable<FactionDef> All => _factions.Values;

    private static void Register(FactionDef f) => _factions[f.factionId] = f;

    // ── Darkside ────────────────────────────────────────────────────────

    private static FactionDef CreateDarkside()
    {
        return new FactionDef
        {
            factionId = "darkside",
            factionName = "Darkside",
            championDef = new UnitDef
            {
                displayName = "Dark Champion",
                maxHP = 12,
                iconLetter = "D",
                deckId = "champion_darkside",
                passiveType = PassiveType.DarkPact,
            },
            creatures = new[]
            {
                // Cheap (2g)
                new CreatureEntry(new UnitDef
                {
                    displayName = "Spider",
                    maxHP = 3,
                    iconLetter = "S",
                    deckId = "spider",
                    passiveType = PassiveType.Horde,
                }, 2),

                new CreatureEntry(new UnitDef
                {
                    displayName = "Skeleton",
                    maxHP = 4,
                    iconLetter = "K",
                    deckId = "skeleton",
                    passiveType = PassiveType.None,
                }, 2),

                // Medium (3g)
                new CreatureEntry(new UnitDef
                {
                    displayName = "Cultist",
                    maxHP = 6,
                    iconLetter = "C",
                    deckId = "cultist",
                    passiveType = PassiveType.DarkPact,
                }, 3),

                // Elite (5g)
                new CreatureEntry(new UnitDef
                {
                    displayName = "Witch",
                    maxHP = 5,
                    iconLetter = "W",
                    deckId = "witch",
                    passiveType = PassiveType.None,
                }, 5),
            },
        };
    }

    // ── Brotherhood ─────────────────────────────────────────────────────

    private static FactionDef CreateBrotherhood()
    {
        return new FactionDef
        {
            factionId = "brotherhood",
            factionName = "Brotherhood",
            championDef = new UnitDef
            {
                displayName = "Knight Champion",
                maxHP = 14,
                iconLetter = "K",
                deckId = "champion_brotherhood",
                passiveType = PassiveType.Guardian,
            },
            creatures = new[]
            {
                // Cheap (2g)
                new CreatureEntry(new UnitDef
                {
                    displayName = "Soldier",
                    maxHP = 5,
                    iconLetter = "S",
                    deckId = "soldier",
                    passiveType = PassiveType.None,
                }, 2),

                // Medium (3g)
                new CreatureEntry(new UnitDef
                {
                    displayName = "Sniper",
                    maxHP = 3,
                    iconLetter = "N",
                    deckId = "sniper",
                    passiveType = PassiveType.Setup,
                }, 3),

                new CreatureEntry(new UnitDef
                {
                    displayName = "Guerrilla",
                    maxHP = 4,
                    iconLetter = "G",
                    deckId = "guerrilla",
                    passiveType = PassiveType.None,
                }, 3),
            },
        };
    }
}
