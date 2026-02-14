/// <summary>
/// Provides scenarios based on run progress. In the future this will be
/// procedural generation or a roguelike map. For now, a fixed sequence.
/// </summary>
public static class ScenarioPool
{
    /// <summary>
    /// Get the scenario for a given stage number (1-indexed).
    /// Returns null if the run is complete.
    /// </summary>
    public static ScenarioDef GetScenario(int stageNumber)
    {
        return stageNumber switch
        {
            1 => CreateStage1(),
            2 => CreateStage2(),
            3 => CreateStage3(),
            _ => null, // Run complete
        };
    }

    /// <summary>Stage 1: Easy — 2 Spiders.</summary>
    private static ScenarioDef CreateStage1()
    {
        return new ScenarioDef
        {
            scenarioName = "Spider Nest",
            gridColumns = 7,
            gridRows = 7,
            playerSpawn = new HexCoord(1, 1),
            enemySpawns = new[]
            {
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Spider 1", maxHP = 3, iconLetter = "S",
                        deckId = "spider", passiveType = PassiveType.Horde,
                        xpReward = 2, goldReward = 1,
                    },
                    position = new HexCoord(4, 0),
                },
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Spider 2", maxHP = 3, iconLetter = "S",
                        deckId = "spider", passiveType = PassiveType.Horde,
                        xpReward = 2, goldReward = 1,
                    },
                    position = new HexCoord(5, -1),
                },
            },
            initialLootGold = 3,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 1,
        };
    }

    /// <summary>Stage 2: Medium — 2 Spiders + 1 Cultist.</summary>
    private static ScenarioDef CreateStage2()
    {
        return new ScenarioDef
        {
            scenarioName = "Cultist Ambush",
            gridColumns = 7,
            gridRows = 7,
            playerSpawn = new HexCoord(1, 1),
            enemySpawns = new[]
            {
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Spider", maxHP = 3, iconLetter = "S",
                        deckId = "spider", passiveType = PassiveType.Horde,
                        xpReward = 2, goldReward = 1,
                    },
                    position = new HexCoord(4, 0),
                },
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Spider", maxHP = 4, iconLetter = "S",
                        deckId = "spider", passiveType = PassiveType.Horde,
                        xpReward = 3, goldReward = 1,
                    },
                    position = new HexCoord(5, -1),
                },
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Cultist", maxHP = 8, iconLetter = "C",
                        deckId = "cultist", passiveType = PassiveType.DarkPact,
                        xpReward = 4, goldReward = 2,
                    },
                    position = new HexCoord(3, 3),
                },
            },
            initialLootGold = 5,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 2,
        };
    }

    /// <summary>Stage 3: Hard — 2 Cultists.</summary>
    private static ScenarioDef CreateStage3()
    {
        return new ScenarioDef
        {
            scenarioName = "Dark Ritual",
            gridColumns = 7,
            gridRows = 7,
            playerSpawn = new HexCoord(0, 2),
            enemySpawns = new[]
            {
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "Cultist", maxHP = 8, iconLetter = "C",
                        deckId = "cultist", passiveType = PassiveType.DarkPact,
                        xpReward = 5, goldReward = 2,
                    },
                    position = new HexCoord(4, 0),
                },
                new EnemySpawn
                {
                    unitDef = new UnitDef
                    {
                        displayName = "High Cultist", maxHP = 10, iconLetter = "HC",
                        deckId = "cultist", passiveType = PassiveType.DarkPact,
                        xpReward = 6, goldReward = 3,
                    },
                    position = new HexCoord(4, 2),
                },
            },
            initialLootGold = 4,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 3,
        };
    }
}
