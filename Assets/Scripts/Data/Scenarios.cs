/// <summary>
/// Predefined scenario definitions. In the future these could be loaded
/// from JSON, generated procedurally, or selected from a map screen.
/// </summary>
public static class Scenarios
{
    /// <summary>
    /// The original test battle: Player vs 2 Spiders + 1 Cultist on a 7×7 grid.
    /// </summary>
    public static ScenarioDef CreateTestScenario()
    {
        return new ScenarioDef
        {
            scenarioName = "Test Battle",
            gridColumns = 7,
            gridRows = 7,

            playerSpawns = new[] { new HexCoord(1, 1), new HexCoord(1, 2) },

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
            threatLevel = 1,
        };
    }
}
