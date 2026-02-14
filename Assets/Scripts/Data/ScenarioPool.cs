using System.Collections.Generic;

/// <summary>
/// Provides scenario choices based on run progress. Each stage offers
/// 1-3 scenario options with different enemy compositions and threat levels.
/// All scenarios provide spawn positions for a 2-player party.
/// </summary>
public static class ScenarioPool
{
    /// <summary>
    /// Get scenario choices for a given stage (1-indexed).
    /// Returns null if no more stages exist (run complete).
    /// </summary>
    public static ScenarioDef[] GetChoices(int stageNumber)
    {
        return stageNumber switch
        {
            1 => new[] { CreateSpiderNest() },
            2 => new[] { CreateCultistAmbush(), CreateSpiderLair(), CreateDarkShrine() },
            3 => new[] { CreateDarkRitual(), CreateSpiderQueensDen() },
            _ => null,
        };
    }

    /// <summary>
    /// Whether a stage exists. Used for hasNext checks.
    /// </summary>
    public static bool HasStage(int stageNumber)
    {
        return GetChoices(stageNumber) != null;
    }

    /// <summary>
    /// Get the default (first) scenario for a stage. Backward-compatible helper.
    /// </summary>
    public static ScenarioDef GetScenario(int stageNumber)
    {
        var choices = GetChoices(stageNumber);
        return choices != null && choices.Length > 0 ? choices[0] : null;
    }

    /// <summary>
    /// Build a human-readable enemy preview from a scenario's spawns.
    /// E.g. "2x Spider, 1x Cultist"
    /// </summary>
    public static string GetEnemyPreview(ScenarioDef scenario)
    {
        var counts = new Dictionary<string, int>();
        foreach (var spawn in scenario.enemySpawns)
        {
            // Strip trailing numbers/spaces for grouping (e.g. "Spider 1" → "Spider")
            string name = spawn.unitDef.displayName;
            int lastSpace = name.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                string suffix = name.Substring(lastSpace + 1);
                if (int.TryParse(suffix, out _))
                    name = name.Substring(0, lastSpace);
            }
            counts[name] = counts.GetValueOrDefault(name, 0) + 1;
        }

        var parts = new List<string>();
        foreach (var kvp in counts)
            parts.Add($"{kvp.Value}x {kvp.Key}");
        return string.Join(", ", parts);
    }

    // ── Stage 1: Easy ─────────────────────────────────────────────────────

    private static ScenarioDef CreateSpiderNest()
    {
        return new ScenarioDef
        {
            scenarioName = "Spider Nest",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(1, 1), new HexCoord(1, 2) },
            enemySpawns = new[]
            {
                Spider("Spider 1", 3, new HexCoord(4, 0), xp: 2, gold: 1),
                Spider("Spider 2", 3, new HexCoord(5, -1), xp: 2, gold: 1),
            },
            initialLootGold = 3,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 1,
        };
    }

    // ── Stage 2: Medium (3 choices) ────────────────────────────────────────

    private static ScenarioDef CreateCultistAmbush()
    {
        return new ScenarioDef
        {
            scenarioName = "Cultist Ambush",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(1, 1), new HexCoord(1, 2) },
            enemySpawns = new[]
            {
                Spider("Spider", 3, new HexCoord(4, 0), xp: 2, gold: 1),
                Spider("Spider", 4, new HexCoord(5, -1), xp: 3, gold: 1),
                Cultist("Cultist", 8, new HexCoord(3, 3), xp: 4, gold: 2),
            },
            initialLootGold = 5,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 2,
        };
    }

    private static ScenarioDef CreateSpiderLair()
    {
        return new ScenarioDef
        {
            scenarioName = "Spider Lair",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(0, 2), new HexCoord(0, 3) },
            enemySpawns = new[]
            {
                Spider("Spider 1", 3, new HexCoord(3, 0), xp: 2, gold: 1),
                Spider("Spider 2", 3, new HexCoord(5, 0), xp: 2, gold: 1),
                Spider("Spider 3", 4, new HexCoord(4, 2), xp: 3, gold: 1),
                Spider("Spider 4", 4, new HexCoord(5, 3), xp: 3, gold: 2),
            },
            initialLootGold = 6,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 2,
        };
    }

    private static ScenarioDef CreateDarkShrine()
    {
        return new ScenarioDef
        {
            scenarioName = "Dark Shrine",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(1, 1), new HexCoord(1, 2) },
            enemySpawns = new[]
            {
                Cultist("Cultist", 7, new HexCoord(4, 0), xp: 5, gold: 2),
                Cultist("High Cultist", 9, new HexCoord(4, 3), xp: 6, gold: 3),
            },
            initialLootGold = 3,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 3,
        };
    }

    // ── Stage 3: Hard (2 choices) ──────────────────────────────────────────

    private static ScenarioDef CreateDarkRitual()
    {
        return new ScenarioDef
        {
            scenarioName = "Dark Ritual",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(0, 2), new HexCoord(0, 3) },
            enemySpawns = new[]
            {
                Cultist("Cultist", 8, new HexCoord(4, 0), xp: 5, gold: 2),
                Cultist("High Cultist", 10, new HexCoord(4, 2), xp: 6, gold: 3),
            },
            initialLootGold = 4,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 3,
        };
    }

    private static ScenarioDef CreateSpiderQueensDen()
    {
        return new ScenarioDef
        {
            scenarioName = "Spider Queen's Den",
            gridColumns = 7, gridRows = 7,
            playerSpawns = new[] { new HexCoord(0, 2), new HexCoord(0, 3) },
            enemySpawns = new[]
            {
                Spider("Spider 1", 4, new HexCoord(3, 0), xp: 2, gold: 1),
                Spider("Spider 2", 4, new HexCoord(5, 0), xp: 2, gold: 1),
                Spider("Spider 3", 5, new HexCoord(4, 3), xp: 3, gold: 1),
                Cultist("High Cultist", 10, new HexCoord(5, 3), xp: 6, gold: 3),
            },
            initialLootGold = 4,
            winCondition = WinConditionType.DefeatAll,
            threatLevel = 3,
        };
    }

    // ── Helper factories for cleaner scenario definitions ──────────────────

    private static EnemySpawn Spider(string name, int hp, HexCoord pos, int xp, int gold)
    {
        return new EnemySpawn
        {
            unitDef = new UnitDef
            {
                displayName = name, maxHP = hp, iconLetter = "S",
                deckId = "spider", passiveType = PassiveType.Horde,
                xpReward = xp, goldReward = gold,
            },
            position = pos,
        };
    }

    private static EnemySpawn Cultist(string name, int hp, HexCoord pos, int xp, int gold)
    {
        return new EnemySpawn
        {
            unitDef = new UnitDef
            {
                displayName = name, maxHP = hp,
                iconLetter = name.Contains("High") ? "HC" : "C",
                deckId = "cultist", passiveType = PassiveType.DarkPact,
                xpReward = xp, goldReward = gold,
            },
            position = pos,
        };
    }
}
